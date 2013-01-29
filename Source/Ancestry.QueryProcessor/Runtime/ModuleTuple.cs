using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Runtime
{
	[Type.Tuple]
	public struct ModuleTuple
	{
		public Name Name;
		public Version Version;
		public System.Type Class;

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is ModuleTuple)
				return (ModuleTuple)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(ModuleTuple left, ModuleTuple right)
		{
			return left.Name == right.Name;
		}

		public static bool operator !=(ModuleTuple left, ModuleTuple right)
		{
			return !(left == right);
		}
	}
}
