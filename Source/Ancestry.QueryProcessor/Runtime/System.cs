using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	[Type.Module(new[] { "System" })]
	public class System
	{
		public static List<T> ToList<T>(IEnumerable<T> setValue)
		{
			return new List<T>(setValue);
		}

		public static HashSet<T> ToSet<T>(IEnumerable<T> listValue)
		{
			return new HashSet<T>(listValue);
		}
	}
}
