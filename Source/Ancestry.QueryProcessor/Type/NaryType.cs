using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
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
				//case Parse.Operator.Dereference: 
				//	return CompileDereference(method, compiler, frame, left, expression, typeHint);

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

		//protected virtual ExpressionContext CompileDereference(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		//{
		//	left = compiler.MaterializeReference(method, left);

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
