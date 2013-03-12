using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public struct CompilerResult
	{
		public Runtime.ExecuteHandler Execute;
		public Type.BaseType Type;
	}
}
