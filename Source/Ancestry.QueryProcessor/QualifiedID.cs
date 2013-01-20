using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public struct QualifiedID
	{
		public bool IsRooted;
		public string[] Components;

		public static QualifiedID FromQualifiedIdentifier(Parse.QualifiedIdentifier id)
		{
			return new QualifiedID { IsRooted = id.IsRooted, Components = id.Components };
		}

		public override bool Equals(object obj)
		{
			if (!(obj is QualifiedID))
				return base.Equals(obj);

			return (QualifiedID)obj == this;
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

		public static bool operator ==(QualifiedID left, QualifiedID right)
		{
			return left.IsRooted == right.IsRooted
				&& left.Components.Length == right.Components.Length
				&& left.Components.SequenceEqual(right.Components);
		}

		public static bool operator !=(QualifiedID left, QualifiedID right)
		{
			return !(left == right);
		}
	}
}
