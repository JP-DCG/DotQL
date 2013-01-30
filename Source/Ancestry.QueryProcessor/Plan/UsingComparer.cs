using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class UsingComparer : IEqualityComparer<Parse.Using>
	{
		public bool Equals(Parse.Using x, Parse.Using y)
		{
			return x.Target == y.Target;
		}

		public int GetHashCode(Parse.Using obj)
		{
			return obj.Target.GetHashCode();
		}
	}
}
