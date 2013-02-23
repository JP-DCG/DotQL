using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	/// <summary> This class works around the fact that the .NET void type cannot be used as a generic type argument. </summary>
	public struct Void 
	{
		public override string ToString()
		{
			return "void";
		} 
	}
}
