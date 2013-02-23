using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public static class SystemTypes
	{
		public static readonly StringType String = new StringType();
		public static readonly BaseIntegerType Integer = new BaseIntegerType(typeof(int));
		public static readonly BaseIntegerType Long = new BaseIntegerType(typeof(long));
		public static readonly VoidType Void = new VoidType();
		public static readonly ScalarType Boolean = new BooleanType();
	}
}
