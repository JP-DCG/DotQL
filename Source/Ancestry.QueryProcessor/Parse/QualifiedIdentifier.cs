using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Parse
{
	public struct QualifiedIdentifier
	{
		public bool IsRooted;
		public string[] Components;

		public override bool Equals(object obj)
		{
			if (!(obj is QualifiedIdentifier))
				return base.Equals(obj);

			return (QualifiedIdentifier)obj == this;
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

		public static bool operator ==(QualifiedIdentifier left, QualifiedIdentifier right)
		{
			return left.IsRooted == right.IsRooted
				&& left.Components.Length == right.Components.Length
				&& left.Components.SequenceEqual(right.Components);
		}

		public static bool operator !=(QualifiedIdentifier left, QualifiedIdentifier right)
		{
			return !(left == right);
		}
	}
}
