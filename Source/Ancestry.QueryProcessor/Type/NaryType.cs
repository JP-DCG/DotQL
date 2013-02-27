using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public abstract class NaryType : BaseType, IComponentType
	{
		public BaseType Of { get; set; }

		public override ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, BaseType typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Dereference: 
					return CompileDereference(method, compiler, frame, left, expression, typeHint);

				default: return base.CompileBinaryExpression(method, compiler, frame, left, expression, typeHint);
			}
		}
		
		protected override ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
					return base.DefaultBinaryOperator(method, compiler, left, right, expression);

				//// TODO: nary intersection and union
				//case Parse.Operator.BitwiseOr:
				//case Parse.Operator.BitwiseAnd:

				default: throw NotSupported(expression);
			}
		}

		public override ExpressionContext CompileRestrictExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.RestrictExpression expression, BaseType typeHint)
		{
			left = compiler.MaterializeRepository(method, left);

			var memberType = ((NaryType)left.Type).Of;
			var memberNative = left.NativeType ?? memberType.GetNative(compiler.Emitter);

			bool indexReferenced;
			var innerMethod = EmitWhereLambda(method, compiler, frame, expression, memberType, memberNative, out indexReferenced);

			// TODO: Force ordering to left if set and index is referenced

			// Instantiate a delegate pointing to the new method
			method.IL.Emit(OpCodes.Ldnull);				// instance
			method.IL.Emit(OpCodes.Ldftn, innerMethod.Builder);	// method
			method.IL.Emit
			(
				OpCodes.Newobj,
				System.Linq.Expressions.Expression.GetFuncType(memberNative, typeof(int), typeof(bool))
					.GetConstructor(new[] { typeof(object), typeof(IntPtr) })
			);

			var where = typeof(System.Linq.Enumerable).GetMethodExt("Where", new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), typeof(Func<ReflectionUtility.T, int, bool>) });
			where = where.MakeGenericMethod(memberNative);
			
			method.IL.EmitCall(OpCodes.Call, where, null);

			return new ExpressionContext((NaryType)left.Type, where.ReturnType);
		}

		private static MethodContext EmitWhereLambda(MethodContext outerMethod, Compiler compiler, Frame frame, Parse.RestrictExpression expression, BaseType memberType, System.Type memberNative, out bool indexReferenced)
		{
			var local = compiler.AddFrame(frame, expression);

			// Create a new private method for the condition
			var typeBuilder = (TypeBuilder)outerMethod.Builder.DeclaringType;
			var innerMethod =
				new MethodContext
				(
					typeBuilder.DefineMethod
					(
						"Where" + expression.GetHashCode(),
						MethodAttributes.Private | MethodAttributes.Static,
						typeof(bool),	// return type
						new System.Type[] { memberNative, typeof(int) }	// param types
					)
				);

			// Register value argument
			var valueSymbol = new Object();
			local.Add(expression.Condition, Name.FromComponents(Parse.ReservedWords.Value), valueSymbol);
			compiler.WritersBySymbol.Add(valueSymbol, m => { m.IL.Emit(OpCodes.Ldarg_0); return new ExpressionContext(memberType); });

			// Register index argument
			var indexSymbol = new Object();
			local.Add(expression.Condition, Name.FromComponents(Parse.ReservedWords.Index), indexSymbol);
			compiler.WritersBySymbol.Add(indexSymbol, m => { m.IL.Emit(OpCodes.Ldarg_1); return new ExpressionContext(SystemTypes.Integer); });

			// If the members are tuples, declare locals for each field
			if (memberType is TupleType)
			{
				var tupleType = (TupleType)memberType;
				foreach (var attribute in tupleType.Attributes)
				{
					local.Add(attribute.Key.ToQualifiedIdentifier(), attribute);
					compiler.WritersBySymbol.Add
					(
						attribute,
						m =>
						{
							m.IL.Emit(OpCodes.Ldarg_0);	// value
							m.IL.Emit(OpCodes.Ldfld, memberNative.GetField(attribute.Key.ToString()));
							return new ExpressionContext(attribute.Value);
						}
					);
				}

				// TODO: Add references
			}

			// Compile condition
			var condition = compiler.MaterializeRepository
				(
					innerMethod,
					compiler.CompileExpression(innerMethod, local, expression.Condition, SystemTypes.Boolean)
				);

			indexReferenced = compiler.References.ContainsKey(indexSymbol);

			innerMethod.IL.Emit(OpCodes.Ret);
			return innerMethod;
		}

		protected virtual ExpressionContext CompileDereference(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			left = compiler.MaterializeRepository(method, left);

			var memberType = ((NaryType)left.Type).Of;
			var memberNative = left.NativeType ?? memberType.GetNative(compiler.Emitter);

			var lambda = EmitSelectLambda(method, compiler, frame, expression, memberType, memberNative);

			// TODO: Force ordering of Left if a set and index is referenced

			// Instantiate a delegate pointing to the new method
			method.IL.Emit(OpCodes.Ldnull);				// instance
			method.IL.Emit(OpCodes.Ldftn, lambda.Method.Builder);	// method
			method.IL.Emit
			(
				OpCodes.Newobj,
				System.Linq.Expressions.Expression.GetFuncType(memberNative, typeof(int), lambda.Method.Builder.ReturnType)
					.GetConstructor(new[] { typeof(object), typeof(IntPtr) })
			);

			var select =
				typeof(Enumerable).GetMethodExt
				(
					"Select",
					new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), typeof(Func<ReflectionUtility.T, int, ReflectionUtility.T>) }
				);
			select = select.MakeGenericMethod(memberNative, lambda.Method.Builder.ReturnType);
			
			method.IL.EmitCall(OpCodes.Call, select, null);

			return new ExpressionContext(new ListType { Of = lambda.ReturnType }, select.ReturnType);
		}

		private struct SelectLambdaInfo
		{
			public bool IndexReferenced;
			public BaseType ReturnType;
			public MethodContext Method;
		}

		private static SelectLambdaInfo EmitSelectLambda(MethodContext outerMethod, Compiler compiler, Frame frame, Parse.BinaryExpression expression, BaseType memberType, System.Type memberNative)
		{
			// TODO: this largely duplicates EmitWhereLambda; refactor.

			SelectLambdaInfo info = new SelectLambdaInfo();

			var local = compiler.AddFrame(frame, expression);

			// Create a new private method for the condition
			var typeBuilder = (TypeBuilder)outerMethod.Builder.DeclaringType;
			info.Method =
				new MethodContext
				(
					typeBuilder.DefineMethod
					(
						"Select" + expression.GetHashCode(),
						MethodAttributes.Private | MethodAttributes.Static,
						typeof(void),	// temporary return type
						new System.Type[] { memberNative, typeof(int) }	// param types
					)
				);

			// Register value argument
			var valueSymbol = new Object();
			local.Add(expression.Right, Name.FromComponents(Parse.ReservedWords.Value), valueSymbol);
			compiler.WritersBySymbol.Add(valueSymbol, m => { m.IL.Emit(OpCodes.Ldarg_0); return new ExpressionContext(memberType); });

			// Register index argument
			var indexSymbol = new Object();
			local.Add(expression.Right, Name.FromComponents(Parse.ReservedWords.Index), indexSymbol);
			compiler.WritersBySymbol.Add(indexSymbol, m => { m.IL.Emit(OpCodes.Ldarg_1); return new ExpressionContext(SystemTypes.Integer); });

			// If the members are tuples, declare locals for each field
			if (memberType is TupleType)
			{
				var tupleType = (TupleType)memberType;
				foreach (var attribute in tupleType.Attributes)
				{
					local.Add(attribute.Key.ToQualifiedIdentifier(), attribute);
					compiler.WritersBySymbol.Add
					(
						attribute,
						m =>
						{
							m.IL.Emit(OpCodes.Ldarg_0);	// value
							m.IL.Emit(OpCodes.Ldfld, memberNative.GetField(attribute.Key.ToString()));
							return new ExpressionContext(attribute.Value);
						}
					);
				}

				// TODO: Add references
			}

			// Compile selection
			var selection = compiler.MaterializeRepository
				(
					info.Method,
					compiler.CompileExpression(info.Method, local, expression.Right)
				);
			info.ReturnType = selection.Type;
			var returnNative = selection.NativeType ?? selection.Type.GetNative(compiler.Emitter);
			info.Method.Builder.SetReturnType(returnNative);

			info.IndexReferenced = compiler.References.ContainsKey(indexSymbol);

			info.Method.IL.Emit(OpCodes.Ret);
			return info;
		}
		
		//left = compiler.MaterializeReference(method, left);

		//	var local = compiler.AddFrame(frame, expression);
		//	var memberType = left.Type.GenericTypeArguments[0];
		//	var parameters = new List<ParameterExpression>();

		//	var valueParam = compiler.CreateValueParam(expression, local, left, memberType);
		//	parameters.Add(valueParam);

		//	var indexParam = compiler.CreateIndexParam(expression, local);
		//	parameters.Add(indexParam);

		//	var right =
		//		compiler.MaterializeReference
		//		(
		//			compiler.CompileExpression(method, local, expression.Right, typeHint)
		//		);

		//	var selection = Expression.Lambda(right, parameters);
		//	var select =
		//		typeof(Enumerable).GetMethodExt
		//		(
		//			"Select",
		//			new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), typeof(Func<ReflectionUtility.T, int, ReflectionUtility.T>) }
		//		);
		//	select = select.MakeGenericMethod(memberType, selection.ReturnType);
		//	return Expression.Call(select, left, selection);
		//}

		protected override ExpressionContext DefaultUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
					method.IL.EmitCall(OpCodes.Callvirt, typeof(ICollection<>).MakeGenericType(inner.Type.GetNative(compiler.Emitter)).GetProperty("Count").GetGetMethod(), null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.IsNull:
					method.IL.Emit(OpCodes.Pop);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					return new ExpressionContext(SystemTypes.Boolean);
				default: throw NotSupported(expression);
			}
		}

		public override int GetHashCode()
		{
			return GetType().GetHashCode() * 83 + Of.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is NaryType)
				return (NaryType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(NaryType left, NaryType right)
		{
			return Object.ReferenceEquals(left, right) 
				|| (left.GetType() == right.GetType() && left.Of == right.Of);
		}

		public static bool operator !=(NaryType left, NaryType right)
		{
			return !(left == right);
		}
	}
}
