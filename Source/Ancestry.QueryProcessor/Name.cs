using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public struct Name
	{
		public bool IsRooted;
		public string[] Components;

		public static Name FromQualifiedIdentifier(Parse.QualifiedIdentifier id)
		{
			return new Name { IsRooted = id.IsRooted, Components = id.Components };
		}

		public static Name FromComponents(params string[] components)
		{
			return new Name { Components = components };
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Name))
				return base.Equals(obj);

			return (Name)obj == this;
		}

		public override int GetHashCode()
		{
			var result = IsRooted.GetHashCode();
			foreach (var c in Components)
				result = result * 83 + c.GetHashCode();
			return result;
		}

		public override string ToString()
		{
			return (IsRooted ? "\\" : "") + String.Join("\\", Components);
		}

		public static bool operator ==(Name left, Name right)
		{
			return left.IsRooted == right.IsRooted
				&& left.Components.Length == right.Components.Length
				&& left.Components.SequenceEqual(right.Components);
		}

		public static bool operator !=(Name left, Name right)
		{
			return !(left == right);
		}
	}
}
