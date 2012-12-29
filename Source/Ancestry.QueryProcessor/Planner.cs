using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ancestry.QueryProcessor.Plan
{
	public class Planner
	{
		public ScriptPlan PlanScript(Parse.Script script, QueryOptions actualOptions)
		{
			return new ScriptPlan { Script = script };
		}
	}
}
