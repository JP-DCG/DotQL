using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class ListType : BaseType
	{
		public BaseType OfType { get; set; }

		public override string ToString()
		{
			return "[" + OfType.ToString() + "]";
		}
	}
}
