using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TupleType : BaseType
	{
		private Dictionary<string, TupleMember> _members = new Dictionary<string, TupleMember>();
		public Dictionary<string, TupleMember> Members 
		{ 
			get { return _members; } 
			set { _members = value; }
		}

		private List<Key> _keys = new List<Key>();
		public List<Key> Keys
		{
			get { return _keys; }
			set { _keys = value; }
		}

		public override string ToString()
		{
			return "{" 
				+ 
				(
					Members.Count == 0 
						? " : " 
						: String.Join
						(
							" ", 
							(from m in Members select m.Key + ": " + m.Value.ToString())
								.Union(from k in Keys select k.ToString()).ToArray()
						)
				) 
				+ "}";
		}
	}

	public class Key
	{
		public string AttributeNames { get; set; }

		public override string ToString()
		{
			return "key (" + String.Join(" ", AttributeNames) + ")";
		}
	}

}
