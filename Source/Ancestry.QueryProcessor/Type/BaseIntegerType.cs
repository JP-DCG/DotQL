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

		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Power:
					var intPower = typeof(Runtime.Runtime).GetMethod("IntPower", new[] { typeof(int), typeof(int) });
					if (intPower == null)
						throw new NotSupportedException();
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, intPower, null);
					break;
				
				default: 
					base.EmitBinaryOperator(method, compiler, left, right, expression);
					break;
			}
		}

		public override Parse.Expression BuildDefault()
		{
			if (System.Runtime.InteropServices.Marshal.SizeOf(Native) == 4)
				return new Parse.LiteralExpression { Value = 0 };
			else if (System.Runtime.InteropServices.Marshal.SizeOf(Native) == 8)
				return new Parse.LiteralExpression { Value = 0L };
			else
				throw new NotSupportedException();
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			switch (System.Runtime.InteropServices.Marshal.SizeOf(Native))
			{
				case 4: return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Int32") };
				case 8: return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Int64") };
				default: throw new NotSupportedException();
			}
		}

		public override void EmitLiteral(MethodContext method, object value)
		{
			switch (System.Runtime.InteropServices.Marshal.SizeOf(Native))
			{
				case 4:
					method.IL.Emit(OpCodes.Ldc_I4, (int)value);
					break;
				case 8:
					method.IL.Emit(OpCodes.Ldc_I8, (long)value);
					break;
				default: throw new NotSupportedException();
			}
		}
	}
}
