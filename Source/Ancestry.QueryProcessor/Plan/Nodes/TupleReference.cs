using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Plan.Nodes
{
	public class TupleReference
	{
		private List<Parse.TupleAttribute> _sourceColumns = new List<Parse.TupleAttribute>();
		public List<Parse.TupleAttribute> SourceColumns { get { return _sourceColumns; } }

		public Variable Target { get; set; }

		private List<Parse.TupleAttribute> _targetColumns = new List<Parse.TupleAttribute>();
		public List<Parse.TupleAttribute> TargetColumns { get { return _targetColumns; } }
	}
}
