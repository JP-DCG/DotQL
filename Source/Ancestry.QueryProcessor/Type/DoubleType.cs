using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class DoubleType : ScalarType
	{
		public DoubleType() : base(typeof(Double)) { }

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = 0.0 };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Double") };
		}

		// TODO: define operators for Double

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_R8, (double)value);
		}
	}
}
