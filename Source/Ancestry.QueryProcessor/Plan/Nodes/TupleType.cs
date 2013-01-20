using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Plan.Nodes
{
	public class TupleType
	{
		private Dictionary<Parse.QualifiedIdentifier, Parse.TupleAttribute> _attributes = new Dictionary<Parse.QualifiedIdentifier, Parse.TupleAttribute>();
		public Dictionary<Parse.QualifiedIdentifier, Parse.TupleAttribute> Attributes { get { return _attributes; } }
		
		private HashSet<TupleKey> _keys = new HashSet<TupleKey>();
		public HashSet<TupleKey> Keys { get { return _keys; } }

		private Dictionary<Parse.QualifiedIdentifier, TupleReference> _references = new Dictionary<Parse.QualifiedIdentifier, TupleReference>();
		public Dictionary<Parse.QualifiedIdentifier, TupleReference> References { get { return _references; } }
	}
}
