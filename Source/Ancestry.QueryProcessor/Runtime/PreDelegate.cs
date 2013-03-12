using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	/// <summary> This structure provides information necessary to construct a delegate. </summary>
	/// <remarks> .NET delegates can only be instantiated with generics resolved; this allows us to hold 
	/// the information we'll need to construct the once we know the generic arguments.  This allows, for 
	/// instance, a delegate-like variable of a generic type. </remarks>
	public struct PreDelegate
	{
		public object Instance;
		public IntPtr Method;
	}
}
