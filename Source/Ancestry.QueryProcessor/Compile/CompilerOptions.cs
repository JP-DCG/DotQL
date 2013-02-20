using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class CompilerOptions
	{
		public List<Parse.Using> DefaultUsings { get; set; }
		public bool DebugOn { get; set; }
		public string AssemblyName { get; set; }
		public string SourceFileName { get; set; }
		public Storage.IRepositoryFactory Factory { get; set; }
		public Dictionary<string, Type.BaseType> ScalarTypes { get; set; }
	}
}
