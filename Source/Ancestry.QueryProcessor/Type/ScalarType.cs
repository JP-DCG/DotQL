using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	[Flags]
	public enum ScalarTypeFlags
	{
		Intrinsic = 1,
		Ordinal = 2
	}

	public class ScalarType : BaseType
	{
		public ScalarTypeFlags Flags { get; set; }
		public string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
