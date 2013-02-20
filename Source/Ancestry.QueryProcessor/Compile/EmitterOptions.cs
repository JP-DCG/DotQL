using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class EmitterOptions
	{
		public bool DebugOn { get; set; }
		public string AssemblyName { get; set; }
		public string SourceFileName { get; set; }
		public Dictionary<string, Type.BaseType> ScalarTypes { get; set; }
	}
}
