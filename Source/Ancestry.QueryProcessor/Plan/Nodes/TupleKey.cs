using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Plan.Nodes
{
	public class TupleKey : List<Parse.TupleAttribute>
	{
		public TupleKey(IEnumerable<Parse.TupleAttribute> items) : base(items) { }
		public TupleKey() { }
	}
}
