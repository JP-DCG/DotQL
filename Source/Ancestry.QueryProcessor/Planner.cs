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
			// Find all the symbols and frames
			var plan = DiscoverScript(script);

			// TODO: Resolve all references

			// TODO: Determine expression data types and characteristics

			// TODO: Determine storage support boundaries

			// TODO: Create access plans for all tables

			return plan;
		}

		private ScriptPlan DiscoverScript(Parse.Script script)
		{
			var plan = new ScriptPlan(script);

			// TODO: import symbols for usings
			//foreach (var u in script.Usings)
			//	yield return u;

			// TODO: manage symbols for modules
			//foreach (var m in script.Modules)
			//	yield return m;

			foreach (var v in script.Vars)
			{
				if (v.Name.IsRooted)
					throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);

				DiscoverStatement(plan, v.Initializer);
				plan.Frame.Add(v.Name, v);
			}

			foreach (var a in script.Assignments)
				DiscoverStatement(plan, a);

			if (script.Expression != null)
				DiscoverStatement(plan, script.Expression);
			return plan;
		}

		private void DiscoverStatement(BasePlan plan, Parse.Statement statement)
		{
			if (statement is Parse.ClausedExpression)
				DiscoverClausedExpression(plan, (Parse.ClausedExpression)statement);
			else
			{
				foreach (var s in statement.GetChildren())
					DiscoverStatement(plan, s);
			}
		}

		private void DiscoverClausedExpression(BasePlan plan, Parse.ClausedExpression expression)
		{
			var clausePlan = new ExpressionPlan(plan, expression);

			foreach (var fc in expression.ForClauses)
			{
				if (fc.Name.IsRooted)
					throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);

				DiscoverStatement(clausePlan, fc.Expression);
				clausePlan.Frame.Add(fc.Name, fc);
			}
			foreach (var lc in expression.LetClauses)
			{
				if (lc.Name.IsRooted)
					throw new PlanningException(PlanningException.Codes.InvalidRootedIdentifier);

				DiscoverStatement(clausePlan, lc.Expression);
				clausePlan.Frame.Add(lc.Name, lc);
			}
			if (expression.WhereClause != null)
				DiscoverStatement(clausePlan, expression.WhereClause);
			foreach (var od in expression.OrderDimensions)
				DiscoverStatement(clausePlan, od);
			DiscoverStatement(clausePlan, expression.Expression); 
		}
	}
}
