using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	/// <summary> Set that implements exhaustive equality comparison. </summary>
	public class Set<T> : HashSet<T>
	{
		public Set() : base() { }
		public Set(IEnumerable<T> items) : base(items) { }

		public override int GetHashCode()
		{
			var result = 0;
			foreach (var entry in this)
				result ^= entry.GetHashCode();
			return result;
		}

		public override bool Equals(object obj)
		{
			if (obj is Set<T>)
				return (Set<T>)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(Set<T> left, Set<T> right)
		{
			return Object.ReferenceEquals(left, right) 
				|| 
				(
					!Object.ReferenceEquals(right, null) 
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType() 
						&& left.Equivalent(right)
				);
		}

		public static bool operator !=(Set<T> left, Set<T> right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return "{ " + String.Join(" ", from i in this select i.ToString()) + " }";
		}
	}
}
