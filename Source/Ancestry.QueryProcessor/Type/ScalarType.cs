using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class ScalarType : BaseType
	{
		public ScalarType(System.Type native)
		{
			Native = native;
		}

		public System.Type Native { get; set; }

		public override System.Type GetNative(Emitter emitter)
		{
			return Native;
		}

		public override int GetHashCode()
		{
			return Native.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is ScalarType)
				return (ScalarType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(ScalarType left, ScalarType right)
		{
			return Object.ReferenceEquals(left, right)
				|| (left.Native == right.Native);
		}

		public static bool operator !=(ScalarType left, ScalarType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Native.Name;
		}
	}
}
