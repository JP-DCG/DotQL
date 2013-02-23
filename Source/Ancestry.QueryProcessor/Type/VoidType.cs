using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class VoidType : BaseType
	{
		public override System.Type GetNative(Compile.Emitter emitter)
		{
			return typeof(Runtime.Void);
		}
	}
}
