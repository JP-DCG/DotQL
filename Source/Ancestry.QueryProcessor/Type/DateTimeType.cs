using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class DateTimeType : ScalarType
	{
		public DateTimeType() : base(typeof(DateTime)) { }

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = new DateTime(0L) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "DateTime") };
		}

		// TODO: define operators for DateTime

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_I8, ((DateTime)value).Ticks);
			method.IL.Emit(OpCodes.Newobj, ReflectionUtility.DateTimeTicksConstructor);
		}
	}
}
