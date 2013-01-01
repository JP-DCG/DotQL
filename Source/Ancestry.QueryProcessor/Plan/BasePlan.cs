using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class BasePlan
	{
		public BasePlan(BasePlan parent)
		{
			Frame = new Frame(parent != null ? parent.Frame : null);
		}

		public Frame Frame { get; set; }

		private Dictionary<Parse.Expression, ExpressionPlan> _expressionPlans = new Dictionary<Parse.Expression, ExpressionPlan>();
		public Dictionary<Parse.Expression, ExpressionPlan> ExpressionPlans { get { return _expressionPlans; } }
	}
}
