using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public static class Utility
	{
		/// <summary> Returns true if the two dictionaries have equivalent keys and values, regardless of enumeration order. </summary>
		/// <remarks> Uses Equals() for key and value comparison.  Note: Returns true if both dictionaries are null. </remarks>
		public static bool Equivalent<TKey, TValue>(this IDictionary<TKey, TValue> left, IDictionary<TKey, TValue> right)
		{
			if (Object.ReferenceEquals(left, right))
				return true;
			if ((left == null) != (right == null))
				return false;
			if (left != null)
			{
				if (left.Count != right.Count)
					return false;
				foreach (var item in left)
				{
					TValue value;
					if (!right.TryGetValue(item.Key, out value))
						return false;
					if (!value.Equals(item.Value))
						return false;
				}
			}
			return true;
		}

		/// <summary> Returns true if the two sets have equivalent entries, regardless of enumeration order. </summary>
		/// <remarks> Uses Equals() for key and value comparison.  Note: Returns true if both sets are null. </remarks>
		public static bool Equivalent<T>(this ISet<T> left, ISet<T> right)
		{
			if (Object.ReferenceEquals(left, right))
				return true;
			if ((left == null) != (right == null))
				return false;
			if (left != null)
			{
				if (left.Count != right.Count)
					return false;
				foreach (var item in left)
					if (!right.Contains(item))
						return false;
			}
			return true;
		}
	}
}
