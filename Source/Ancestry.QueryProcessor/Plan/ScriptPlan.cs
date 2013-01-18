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
			Global = new Frame();
			Frames.Add(script, Global);
		}

		private Parse.Script _script;
		public Parse.Script Script { get { return _script; } }

		private Dictionary<Parse.Statement, Frame> _frames = new Dictionary<Parse.Statement, Frame>();
		public Dictionary<Parse.Statement, Frame> Frames { get { return _frames; } }

		public Frame Global { get; set; }

		private Dictionary<Parse.QualifiedIdentifier, Parse.ISymbol> _references = new Dictionary<Parse.QualifiedIdentifier, Parse.ISymbol>();
		public Dictionary<Parse.QualifiedIdentifier, Parse.ISymbol> References { get { return _references; } }

		public void AddReference(Parse.ISymbol symbol, Parse.QualifiedIdentifier id)
		{
			References.Add(id, symbol);
		}
	}
}
