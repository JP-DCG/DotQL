using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ancestry.QueryProcessor;

namespace Ancestry.QueryProcessor.Plan
{
	public class Planner
	{
		public ScriptPlan PlanScript(Parse.Script script, QueryOptions actualOptions)
		{
			var plan = new ScriptPlan { Script = script };
			ResolveScript(plan);
			return plan;
		}

		private void ResolveScript(ScriptPlan plan)
		{
			plan.Frame = new Frame();
			// TODO: populate script frame w/ variables
			if (plan.Script.Expression != null)
			{
				var frame = new Frame(plan.Frame);
				plan.ExpressionFrames.Add(plan.Script.Expression, frame);
				ResolveClausedExpression(frame, plan.Script.Expression);
			}
		}

		private void ResolveClausedExpression(Frame frame, Parse.ClausedExpression clausedExpression)
		{
			
			foreach (var lc in clausedExpression.LetClauses)
			{
				if (lc.Name.IsRooted)
					throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);

				frame.Add(lc.Name, lc);
			}
		}
	}
}
