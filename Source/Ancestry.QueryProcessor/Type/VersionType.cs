using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	class VersionType : ScalarType
	{
		public VersionType() : base(typeof(Version)) { }

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = new Version(0, 0, 0) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Version") };
		}

		// TODO: define operators for Version

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.EmitVersion((Version)value);
		}
	}
}
