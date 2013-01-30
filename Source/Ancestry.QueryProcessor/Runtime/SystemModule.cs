using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	[Type.Module(new[] { "System" })]
	public class SystemModule
	{
		public Storage.IRepository<ISet<ModuleTuple>> Modules;

		public static IList<T> ToList<T>(ISet<T> setValue)
		{
			return new List<T>(setValue);
		}

		public static ISet<T> ToSet<T>(IList<T> listValue)
		{
			return new HashSet<T>(listValue);
		}

		public static DateTime AddMonth(DateTime start, int months)
		{
			return start.AddMonths(months);
		}
	}
}
