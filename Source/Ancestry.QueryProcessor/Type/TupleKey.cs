using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TupleKey
	{
		// Note: this array is assumed immutable
		public Name[] AttributeNames { get; set; }

		public override string ToString()
		{
			return "key (" + String.Join(" ", from id in AttributeNames select id.ToString()) + ")"; ;
		}

		public override int GetHashCode()
		{
			var running = 83;
			foreach (var an in AttributeNames)
				running = running * 83 + an.GetHashCode();
			return running;
		}

		public override bool Equals(object obj)
		{
			if (obj is TupleKey)
				return (TupleKey)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator==(TupleKey left, TupleKey right)
		{
			return Object.ReferenceEquals(left, right) 
				|| 
				(
					!Object.ReferenceEquals(right, null) 
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType() 
						&& left.AttributeNames.SequenceEqual(right.AttributeNames)
				);
		}

		public static bool operator!=(TupleKey left, TupleKey right)
		{
			return !(left == right);
		}

		public static TupleKey FromParseKey(Parse.TupleKey key)
		{
			return new TupleKey { AttributeNames = (from an in key.AttributeNames select Name.FromID(an)).ToArray() };
		}
	}
}
