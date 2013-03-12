using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Compile
{
	[Flags]
	public enum Characteristic
	{
		Default = 0,
		Constant = 1,
		NonDeterministic = 2,
		SideEffectual = 4
	}
}
