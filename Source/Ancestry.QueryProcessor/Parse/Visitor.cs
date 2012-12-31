using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Parse
{
	public class Visitor
	{
		public virtual void VisitScript(Script script)
		{
			VisitStatement(script);
			foreach (var u in script.Usings)
				VisitStatement(u);
			foreach (var m in script.Modules)
				VisitModule(m);
			foreach (var v in script.Vars)
				VisitVar(v);
			foreach (var a in script.Assignments)
				VisitAssignment(a);
			if (script.Expression != null)
				VisitClausedExpression(script.Expression);
		}

		public virtual void VisitModule(ModuleDeclaration m)
		{
			VisitStatement(m);
			foreach (var member in m.Members)
				VisitStatement(member);		// TODO: actual visits for module members
		}

		public virtual void VisitVar(VarDeclaration v)
		{
			VisitStatement(v);
		}

		public virtual void VisitAssignment(Assignment a)
		{
			VisitStatement(a);
		}

		public virtual void VisitClausedExpression(ClausedExpression ce)
		{
			VisitStatement(ce);
		}

		public virtual void VisitStatement(Statement statement)
		{
		}
	}
}
