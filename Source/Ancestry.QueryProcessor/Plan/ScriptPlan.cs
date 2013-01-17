using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Plan
{
	public class ScriptPlan
	{
		public ScriptPlan(Parse.Script script)
		{ 
			_script = script;
		}

		private Parse.Script _script;
		public Parse.Script Script { get { return _script; } }

		private Dictionary<Parse.Statement, Frame> _frames = new Dictionary<Parse.Statement, Frame>();
		public Dictionary<Parse.Statement, Frame> Frames { get { return _frames; } }

		private Dictionary<Parse.Statement, Nodes.BaseNode> _nodes = new Dictionary<Parse.Statement,Nodes.BaseNode>();
		public Dictionary<Parse.Statement, Nodes.BaseNode> Nodes { get { return _nodes; } }

		private Dictionary<Parse.ISymbol, List<Parse.Statement>> _referencedBy = new Dictionary<Parse.ISymbol,List<Parse.Statement>>();
		public Dictionary<Parse.ISymbol, List<Parse.Statement>> ReferencedBy { get { return _referencedBy; } }

		public void AddReferencedBy(Parse.ISymbol symbol, Parse.Statement statement)
		{
			List<Parse.Statement> item;
			if (!ReferencedBy.TryGetValue(symbol, out item))
			{
				item = new List<Parse.Statement>();
				ReferencedBy.Add(symbol, item);
			}
			item.Add(statement);
		}
	}
}
