using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TupleType
	{
		private Dictionary<Name, System.Type> _attributes = new Dictionary<Name, System.Type>();
		public Dictionary<Name, System.Type> Attributes { get { return _attributes; } }

		private Dictionary<Name, TupleReference> _references = new Dictionary<Name,TupleReference>();
		public Dictionary<Name, TupleReference> References { get { return _references; } }

		private HashSet<TupleKey> _keys = new HashSet<TupleKey>();
		public HashSet<TupleKey> Keys { get { return _keys; } }

		public override int GetHashCode()
		{
			var running = 83;
			foreach (var a in _attributes)
				running ^= a.Key.GetHashCode() * 83 + a.Value.GetHashCode();
			foreach (var r in _references)
				running ^= r.Key.GetHashCode() * 83 + r.Value.GetHashCode();
			foreach (var k in _keys)
				running ^= k.GetHashCode();
			return running;
		}

		public override bool Equals(object obj)
		{
			if (obj is TupleType)
				return (TupleType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(TupleType left, TupleType right)
		{
			return Object.ReferenceEquals(left, right)
				|| 
				(
					left.Attributes.SequenceEqual(right.Attributes)
						&& left.References.SequenceEqual(right.References)
						&& left.Keys.SequenceEqual(right.Keys)
				);
		}

		public static bool operator !=(TupleType left, TupleType right)
		{
			return !(left == right);
		}

		public IEnumerable<Name> GetKeyAttributes()
		{
			if (Keys.Count == 0)
			{
				foreach (var a in Attributes)
					yield return a.Key;
			}
			else
			{
				// Return distinct set of all attributes from all keys
				var attributes = new HashSet<Name>();
				foreach (var k in Keys)
					foreach (var an in k.AttributeNames)
					{
						if (attributes.Add(an))
							yield return an;
					}
			}
		}
	}
}
