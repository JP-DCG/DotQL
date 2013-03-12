using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class SetType : NaryType
	{
		public SetType(BaseType of)
		{
			Of = of;
		}

		public override System.Type GetNative(Emitter emitter)
		{
			return typeof(Runtime.Set<>).MakeGenericType(Of.GetNative(emitter));
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.ListSelector();
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.ListType { Type = Of.BuildDOM() };
		}
	}
}
