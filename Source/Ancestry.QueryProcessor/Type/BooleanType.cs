using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class BooleanType : ScalarType
	{
		public BooleanType() : base(typeof(bool)) { }

		protected override ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor:
				case Parse.Operator.ShiftLeft:
				case Parse.Operator.ShiftRight:
				case Parse.Operator.And:
				case Parse.Operator.Or:
				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
				case Parse.Operator.InclusiveGreater:
				case Parse.Operator.InclusiveLess:
				case Parse.Operator.Greater:
				case Parse.Operator.Less:
					return base.DefaultBinaryOperator(method, compiler, left, right, expression);

				default: throw NotSupported(expression);
			}
		}

		protected override ExpressionContext DefaultUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
				case Parse.Operator.IsNull:
				case Parse.Operator.Not:
				case Parse.Operator.BitwiseNot:
					return base.DefaultUnaryOperator(method, compiler, inner, expression);

				default: throw NotSupported(expression);
			}
		}
	}
}
