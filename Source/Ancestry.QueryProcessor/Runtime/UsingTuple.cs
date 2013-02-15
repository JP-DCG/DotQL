using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	[Type.Tuple]
	public class UsingTuple
	{
		public Name Target;
		public Version Version;

		public override int GetHashCode()
		{
			return Target.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is UsingTuple)
				return (UsingTuple)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(UsingTuple left, UsingTuple right)
		{
			return left.Target == right.Target;
		}

		public static bool operator !=(UsingTuple left, UsingTuple right)
		{
			return !(left == right);
		}
	}
}
