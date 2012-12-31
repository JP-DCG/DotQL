using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class ScriptPlan
	{
		public Parse.Script Script { get; set; }

		public Frame Frame { get; set; }

		private Dictionary<Parse.ClausedExpression, Frame> _expressionFrames = new Dictionary<Parse.ClausedExpression,Frame>();
		public Dictionary<Parse.ClausedExpression, Frame> ExpressionFrames { get { return _expressionFrames; } }
	}
}
