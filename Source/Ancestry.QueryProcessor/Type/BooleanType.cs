using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class BooleanType : ScalarType
	{
		public BooleanType() : base(typeof(bool)) { }

		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
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
					base.EmitBinaryOperator(method, compiler, left, right, expression);
					break;
				default: throw NotSupported(expression);
			}
		}

		protected override void EmitUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
				case Parse.Operator.IsNull:
				case Parse.Operator.Not:
				case Parse.Operator.BitwiseNot:
					base.EmitUnaryOperator(method, compiler, inner, expression);
					break;

				default: throw NotSupported(expression);
			}
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = false };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Boolean") };
		}

		public override void EmitLiteral(MethodContext method, object value)
		{
			method.IL.Emit(((bool)value) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		}
	}
}
