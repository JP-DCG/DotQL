using Ancestry.QueryProcessor.Parse;
using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor
{
	public class QueryOptions
	{
		public List<Using> DefaultUsings = new List<Using>();
		public List<IStorageModule> StorageModules = new List<IStorageModule>();
		public QuerySla Sla;
	}
}
