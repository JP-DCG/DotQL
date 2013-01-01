using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class ExpressionPlan : BasePlan
	{
		public ExpressionPlan(BasePlan parent, Parse.Expression expression) : base(parent) 
		{ 
			parent.ExpressionPlans.Add(expression, this);
			Expression = expression;
		}

		public Parse.Expression Expression { get; set; }
	}
}
