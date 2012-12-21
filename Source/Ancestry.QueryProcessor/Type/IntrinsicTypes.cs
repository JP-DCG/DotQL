using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public static class IntrinsicTypes
	{
		public static readonly BaseType Boolean = 
			new ScalarType 
			{ 
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal, 
				Name = "Boolean" 
			};

		public static readonly BaseType Integer =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "Integer"
			};

		public static readonly BaseType Long =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "Long"
			};


		public static readonly BaseType String =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "String"
			};

		public static readonly BaseType Date =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "Date"
			};

		public static readonly BaseType Time =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "Time"
			};

		public static readonly BaseType DateTime =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "DateTime"
			};

		public static readonly BaseType Double =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic,
				Name = "Double"
			};

		public static readonly BaseType Guid =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic,
				Name = "Guid"
			};

		public static readonly BaseType TimeSpan =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic | ScalarTypeFlags.Ordinal,
				Name = "TimeSpan"
			};

		public static readonly BaseType Version =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic,
				Name = "Version"
			};

		public static readonly BaseType Void =
			new ScalarType
			{
				Flags = ScalarTypeFlags.Intrinsic,
				Name = "Void"
			};
	}
}
