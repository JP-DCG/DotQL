using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class BaseIntegerType : ScalarType
	{
		public BaseIntegerType(System.Type native) : base(native) { }

		protected override ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Power:
					var intPower = typeof(Runtime.Runtime).GetMethod("IntPower", new[] { typeof(int), typeof(int) });
					if (intPower == null)
						throw new NotSupportedException();
					method.IL.EmitCall(OpCodes.Call, intPower, null);
					break;
				
				default: return base.DefaultBinaryOperator(method, compiler, left, right, expression);
			}
			return left;
		}
	}
}
