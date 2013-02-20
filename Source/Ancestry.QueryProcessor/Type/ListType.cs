using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class ListType : NaryType
	{
		public override System.Type GetNative(Emitter emitter)
		{
			return typeof(List<>).MakeGenericType(Of.GetNative(emitter));
		}

		public override BaseType Clone()
		{
			return new ListType { IsRepository = this.IsRepository, Of = this.Of };
		}
	}
}
