using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class IntervalType : BaseType
	{
		/// <summary> The ordinal scalar type over which the interval is defined. </summary>
		public ScalarType OfType { get; set; }

		public override string ToString()
		{
			return "interval " + OfType.Name;
		}
	}
}
