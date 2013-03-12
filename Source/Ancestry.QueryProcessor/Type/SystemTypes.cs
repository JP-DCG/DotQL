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
		public static readonly BaseIntegerType Int32 = new BaseIntegerType(typeof(int));
		public static readonly BaseIntegerType Int64 = new BaseIntegerType(typeof(long));
		public static readonly VoidType Void = new VoidType();
		public static readonly ScalarType Boolean = new BooleanType();
		public static readonly DateTimeType DateTime = new DateTimeType();
		public static readonly TimeSpanType TimeSpan = new TimeSpanType();
		public static readonly DoubleType Double = new DoubleType();
		public static readonly CharType Char = new CharType();
	}
}
