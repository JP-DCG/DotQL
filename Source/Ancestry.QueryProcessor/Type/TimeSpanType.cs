using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TimeSpanType : ScalarType
	{
		public TimeSpanType() : base(typeof(TimeSpan)) { }

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = new TimeSpan(0L) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "TimeSpan") };
		}

		// TODO: define operators for TimeSpan

		public override void EmitLiteral(MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_I8, ((TimeSpan)value).Ticks);
			method.IL.Emit(OpCodes.Newobj, ReflectionUtility.TimeSpanTicksConstructor);
		}
	}
}
