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
		public override System.Type GetNative(Emitter emitter)
		{
			return typeof(HashSet<>).MakeGenericType(Of.GetNative(emitter));
		}

		public override BaseType Clone()
		{
			return new SetType { IsRepository = this.IsRepository, Of = this.Of };
		}
	}
}
