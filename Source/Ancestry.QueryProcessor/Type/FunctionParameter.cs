using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class FunctionParameter
	{
		public Name Name { get; set; }
		public BaseType Type { get; set; }

		public override int GetHashCode()
		{
			return Name.GetHashCode() * 83 + Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is FunctionParameter)
				return (FunctionParameter)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(FunctionParameter left, FunctionParameter right)
		{
			return Object.ReferenceEquals(left, right)
				||
				(
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.Name == right.Name
						&& left.Type == right.Type
				);
		}

		public static bool operator !=(FunctionParameter left, FunctionParameter right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Name.ToString() + ": " + Type.ToString();
		}
	}
}
