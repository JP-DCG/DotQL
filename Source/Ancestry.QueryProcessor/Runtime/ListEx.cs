using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	/// <summary> List that implements exhaustive equality comparison. </summary>
	/// <typeparam name="T"></typeparam>
	public class ListEx<T> : System.Collections.Generic.List<T>
	{
		public ListEx() : base() { }
		public ListEx(IEnumerable<T> items) : base(items) { }

		public override int GetHashCode()
		{
			var result = 0;
			foreach (var entry in this)
				result = result * 83 + entry.GetHashCode();
			return result;
		}

		public override bool Equals(object obj)
		{
			if (obj is ListEx<T>)
				return (ListEx<T>)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(ListEx<T> left, ListEx<T> right)
		{
			return Object.ReferenceEquals(left, right)
				|| left.SequenceEqual(right);
		}

		public static bool operator !=(ListEx<T> left, ListEx<T> right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return "[ " + String.Join(" ", from i in this select i.ToString()) + " ]";
		}
	}
}
