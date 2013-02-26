using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class OptionalType : BaseType, IComponentType
	{
		public OptionalType(BaseType of)
		{
			Of = of;
		}

		public BaseType Of { get; private set; }

		public override System.Type GetNative(Emitter emitter)
		{
			var memberNative = Of.GetNative(emitter);
			if (memberNative.IsValueType)
				return typeof(Nullable<>).MakeGenericType(memberNative);
			else
				return memberNative;
		}

		public override int GetHashCode()
		{
			return 1019 + Of.GetHashCode();	// Arbitrary prime
		}

		public override bool Equals(object obj)
		{
			if (obj is OptionalType)
				return (OptionalType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(OptionalType left, OptionalType right)
		{
			return Object.ReferenceEquals(left, right)
				|| (left.Of == right.Of);
		}

		public static bool operator !=(OptionalType left, OptionalType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Of.ToString() + "?";
		}
	}
}
