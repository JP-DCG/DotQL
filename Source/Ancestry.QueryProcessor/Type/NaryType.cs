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

		public override ExpressionContext CompileBinaryExpression(Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, BaseType typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Dereference: 
					return CompileDereference(compiler, frame, left, expression, typeHint);

				default: return base.CompileBinaryExpression(compiler, frame, left, expression, typeHint);
			}
		}
		
		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
					base.EmitBinaryOperator(method, compiler, left, right, expression);
					break;

				//// TODO: nary intersection and union
				//case Parse.Operator.BitwiseOr:
				//case Parse.Operator.BitwiseAnd:

				default: throw NotSupported(expression);
			}
		}

		// Restriction
		public override ExpressionContext CompileExtractExpression(Compiler compiler, Frame frame, ExpressionContext left, Parse.ExtractExpression expression, BaseType typeHint)
		{
			var memberType = ((NaryType)left.Type).Of;
			var memberNative = left.NativeType ?? memberType.GetNative(compiler.Emitter);

			var local = compiler.AddFrame(frame, expression);

			// Prepare index and value symbols
			var indexSymbol = PrepareValueIndexContext(compiler, left, expression.Condition, memberType, memberNative, local);

			// Compile condition
			var condition = compiler.CompileExpression(local, expression.Condition, SystemTypes.Boolean);
			if (!(condition.Type is BooleanType))
				throw new CompilerException(expression.Condition, CompilerException.Codes.IncorrectType, condition.Type, "Boolean");

			var indexReferenced = compiler.References.ContainsKey(indexSymbol);

			return
				new ExpressionContext
				(
					expression,
					left.Type,
					Compiler.MergeCharacteristics(left.Characteristics, condition.Characteristics),
					m =>
					{
						// Create a new private method for the condition
						var typeBuilder = (TypeBuilder)m.Builder.DeclaringType;
						var innerMethod =
							new MethodContext
							(
								typeBuilder.DefineMethod
								(
									"Where" + expression.GetHashCode(),
									MethodAttributes.Private | MethodAttributes.Static,
									typeof(bool),	// return type
									indexReferenced ? new System.Type[] { memberNative, typeof(int) } : new System.Type[] { memberNative }	// param types
								)
							);
						condition.EmitGet(innerMethod);
						innerMethod.IL.Emit(OpCodes.Ret);

						left.EmitGet(m);

						// TODO: Force ordering to left if Set and index is referenced

						// Instantiate a delegate pointing to the new method
						m.IL.Emit(OpCodes.Ldnull);				// instance
						m.IL.Emit(OpCodes.Ldftn, innerMethod.Builder);	// method
						var funcType =
							indexReferenced
								? System.Linq.Expressions.Expression.GetFuncType(memberNative, typeof(int), typeof(bool))
								: System.Linq.Expressions.Expression.GetFuncType(memberNative, typeof(bool));
						m.IL.Emit
						(
							OpCodes.Newobj,
							funcType.GetConstructor(new[] { typeof(object), typeof(IntPtr) })
						);

						funcType = indexReferenced ? typeof(Func<ReflectionUtility.T, int, bool>) : typeof(Func<ReflectionUtility.T, bool>);
						var where = typeof(System.Linq.Enumerable).GetMethodExt("Where", new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), funcType });
						where = where.MakeGenericMethod(memberNative);
			
						m.IL.EmitCall(OpCodes.Call, where, null);
					}
				);
		}

		private static object PrepareValueIndexContext(Compiler compiler, ExpressionContext left, Parse.Statement statement, BaseType memberType, System.Type memberNative, Frame local)
		{
			// Register value argument
			var valueSymbol = new Object();
			local.Add(statement, Name.FromComponents(Parse.ReservedWords.Value), valueSymbol);
			compiler.ContextsBySymbol.Add
			(
				valueSymbol,
				new ExpressionContext
				(
					null,
					memberType,
					left.Characteristics,
					m => { m.IL.Emit(OpCodes.Ldarg_0); }
				)
			);

			// Register index argument
			var indexSymbol = new Object();
			local.Add(statement, Name.FromComponents(Parse.ReservedWords.Index), indexSymbol);
			compiler.ContextsBySymbol.Add
			(
				indexSymbol,
				new ExpressionContext
				(
					null,
					SystemTypes.Int32,
					Characteristic.Default,
					m => { m.IL.Emit(OpCodes.Ldarg_1); }
				)
			);

			// If the members are tuples, declare locals for each field
			if (memberType is TupleType)
			{
				var tupleType = (TupleType)memberType;
				foreach (var attribute in tupleType.Attributes)
				{
					local.Add(attribute.Key.ToID(), attribute);
					compiler.ContextsBySymbol.Add
					(
						attribute,
						new ExpressionContext
						(
							null,
							attribute.Value,
							Characteristic.Default,
							m =>
							{
								m.IL.Emit(OpCodes.Ldarg_0);	// value
								m.IL.Emit(OpCodes.Ldfld, memberNative.GetField(attribute.Key.ToString()));
							}
						)
					);
				}

				// TODO: Add references
			}
			return indexSymbol;
		}

		protected virtual ExpressionContext CompileDereference(Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			var memberType = ((NaryType)left.Type).Of;
			var memberNative = left.NativeType ?? memberType.GetNative(compiler.Emitter);

			var local = compiler.AddFrame(frame, expression);

			// Prepare index and value symbols
			var indexSymbol = PrepareValueIndexContext(compiler, left, expression.Right, memberType, memberNative, local);

			// Compile selection
			var selection = compiler.CompileExpression(local, expression.Right);

			var indexReferenced = compiler.References.ContainsKey(indexSymbol);

			return
				new ExpressionContext
				(
					expression,
					new ListType(selection.Type),
					Compiler.MergeCharacteristics(left.Characteristics, selection.Characteristics),
					m =>
					{
						// Create a new private method for the condition
						var typeBuilder = (TypeBuilder)m.Builder.DeclaringType;
						var innerMethod =
							new MethodContext
							(
								typeBuilder.DefineMethod
								(
									"Select" + expression.GetHashCode(),
									MethodAttributes.Private | MethodAttributes.Static,
									selection.ActualNative(compiler.Emitter),	// temporary return type
									indexReferenced ? new System.Type[] { memberNative, typeof(int) } : new System.Type[] { memberNative }	// param types
								)
							);
						selection.EmitGet(innerMethod);
						innerMethod.IL.Emit(OpCodes.Ret);

						left.EmitGet(m);

						// TODO: Force ordering of Left if a set and index is referenced

						// Instantiate a delegate pointing to the new method
						m.IL.Emit(OpCodes.Ldnull);				// instance
						m.IL.Emit(OpCodes.Ldftn, innerMethod.Builder);	// method
						var funcType =
							indexReferenced
								? System.Linq.Expressions.Expression.GetFuncType(memberNative, typeof(int), innerMethod.Builder.ReturnType)
								: System.Linq.Expressions.Expression.GetFuncType(memberNative, innerMethod.Builder.ReturnType);
						m.IL.Emit
						(
							OpCodes.Newobj,
							funcType.GetConstructor(new[] { typeof(object), typeof(IntPtr) })
						);

						funcType = indexReferenced ? typeof(Func<ReflectionUtility.T, int, ReflectionUtility.T>) : typeof(Func<ReflectionUtility.T, ReflectionUtility.T>);
						var select =
							typeof(Enumerable).GetMethodExt
							(
								"Select",
								new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), funcType }
							);
						select = select.MakeGenericMethod(memberNative, innerMethod.Builder.ReturnType);
			
						m.IL.EmitCall(OpCodes.Call, select, null);
					}
				);
		}
		
		protected override void EmitUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
					method.IL.EmitCall(OpCodes.Callvirt, typeof(ICollection<>).MakeGenericType(inner.Type.GetNative(compiler.Emitter)).GetProperty("Count").GetGetMethod(), null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					break;
				case Parse.Operator.IsNull:
					method.IL.Emit(OpCodes.Pop);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					break;
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
				|| 
				(
					!Object.ReferenceEquals(right, null) 
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType() 
						&& left.Of == right.Of
				);
		}

		public static bool operator !=(NaryType left, NaryType right)
		{
			return !(left == right);
		}
	}
}
