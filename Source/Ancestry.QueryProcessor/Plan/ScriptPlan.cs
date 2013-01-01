using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class ScriptPlan : BasePlan
	{
		public ScriptPlan(Parse.Script script) : base(null) 
		{ 
			Script = script;
		}

		public Parse.Script Script { get; set; }
	}
}
