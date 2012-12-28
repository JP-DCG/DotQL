using Ancestry.QueryProcessor.Type;
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
		public Planner()
		{
		}

		public ScriptPlan PlanScript(Parse.Script script, TupleType fullArgumentTypes, QueryOptions actualOptions)
		{
			throw new NotImplementedException();
		}
	}
}
