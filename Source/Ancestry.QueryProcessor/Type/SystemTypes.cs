using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public static class SystemTypes
	{
		public static readonly StringType String = new StringType { Type = typeof(string) };
		public static readonly BaseIntegerType Integer = new BaseIntegerType { Type = typeof(int) };
		public static readonly BaseIntegerType Long = new BaseIntegerType { Type = typeof(long) };
	}
}
