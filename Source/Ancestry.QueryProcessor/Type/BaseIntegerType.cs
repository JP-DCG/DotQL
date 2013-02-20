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
		public override ExpressionContext CompileOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.Operator op)
		{
			switch (op)
			{
				case Parse.Operator.Power:
					var intPower = typeof(Runtime.Runtime).GetMethod("IntPower", new[] { typeof(int), typeof(int) });
					if (intPower == null)
						throw new NotSupportedException();
					method.IL.EmitCall(OpCodes.Call, intPower, null);
					break;
				
				default: return base.CompileOperator(method, compiler, left, right, op);
			}
			return left;
		}

		public override BaseType Clone()
		{
			return new BaseIntegerType { IsRepository = this.IsRepository, Type = this.Type };
		}
	}
}
