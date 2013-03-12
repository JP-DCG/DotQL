using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class CharType : ScalarType
	{
		public CharType() : base(typeof(Char)) { }

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = Char.MinValue };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Char") };
		}

		// TODO: define operators for Char

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_I4, (char)value);
		}
	}
}
