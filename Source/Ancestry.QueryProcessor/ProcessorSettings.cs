using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public class ProcessorSettings
	{
		public const int DefaultAdHocMaximumTime = 3000;
		public int AdHocMaximumTime = DefaultAdHocMaximumTime;

		public const int DefaultAdHocMaximumRows = 5000;
		public int AdHocMaximumRows = DefaultAdHocMaximumRows;

		public IEnumerable<AssemblyName> AdditionalAssemblies = new AssemblyName[] { };

		public QueryOptions DefaultOptions = new QueryOptions();

		public Storage.IRepositoryFactory RepositoryFactory = new Storage.InMemoryFactory();

		public bool DebugOn = true;
	}
}
