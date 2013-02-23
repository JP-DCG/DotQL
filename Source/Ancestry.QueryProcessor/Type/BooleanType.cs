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

		public override ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
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
					return base.CompileBinaryExpression(method, compiler, frame, left, expression, typeHint);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		public override ExpressionContext CompileUnaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, Type.BaseType typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
				case Parse.Operator.IsNull:
				case Parse.Operator.Not:
				case Parse.Operator.BitwiseNot:
					return base.CompileUnaryExpression(method, compiler, frame, inner, expression, typeHint);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}
	}
}
