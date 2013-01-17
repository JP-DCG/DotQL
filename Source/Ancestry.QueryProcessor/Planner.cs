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
			var plan = new ScriptPlan(script);

			// Find all the symbols and frames and resolve
			DiscoverScript(plan);

			// TODO: Determine expression data types and characteristics

			// TODO: Determine storage support boundaries

			// TODO: Create access plans for all tables

			return plan;
		}

		private void DiscoverScript(ScriptPlan plan)
		{
			// Add root frame
			var local = new Frame();
			plan.Frames.Add(plan.Script, local);

			// TODO: import symbols for usings
			//foreach (var u in plan.Script.Usings)
			//	u;

			// TODO: manage symbols for modules
			//foreach (var m in plan.Script.Modules)
			//	m;

			foreach (var v in plan.Script.Vars)
			{
				DiscoverStatement(plan, local, v.Type);
				DiscoverStatement(plan, local, v.Initializer);
				local.AddNonRooted(v.Name, v);
			}

			foreach (var a in plan.Script.Assignments)
				DiscoverStatement(plan, local, a);

			if (plan.Script.Expression != null)
				DiscoverStatement(plan, local, plan.Script.Expression);
		}

		private void DiscoverStatement(ScriptPlan plan, Frame frame, Parse.Statement statement)
		{
			if (statement is Parse.ClausedExpression)
				DiscoverClausedExpression(plan, frame, (Parse.ClausedExpression)statement);
			if (statement is Parse.TupleType)
				DiscoverTupleType(plan, frame, (Parse.TupleType)statement);
			else
			{
				foreach (var s in statement.GetChildren())
					DiscoverStatement(plan, frame, s);
			}
		}

		private void DiscoverTupleType(ScriptPlan plan, Frame frame, Parse.TupleType tupleType)
		{
			var local = new Frame(null);
			plan.Frames.Add(tupleType, local);

			var tuple = new Nodes.TupleType();

			// Discover all attributes as symbols
			foreach (var a in tupleType.Attributes)
			{
				local.AddNonRooted(a.Name, a);
				tuple.Attributes.Add(a.Name, a);
			}

			// Resolve source reference columns and key references
			foreach (var k in tupleType.Keys)
			{
				tuple.Keys.Add(new Nodes.TupleKey(local.ResolveEach<Parse.TupleAttribute>(k.AttributeNames)));
			}
			foreach (var r in tupleType.References)
			{
				var tupleRef = new Nodes.TupleReference();
				tupleRef.SourceColumns.AddRange(local.ResolveEach<Parse.TupleAttribute>(r.SourceAttributeNames));
				tupleRef.Target = (Nodes.Variable)plan.Nodes[frame.Resolve<Parse.VarMember>(r.Target)];
				//tupleRef.Target.
				tuple.References.Add(r.Name, tupleRef);
			}

			foreach (var r in tupleType.References)
			{
				local.AddNonRooted(r.Name, r);
				Resolve(plan, r, r.Target, frame);
			}
		}

		private void Resolve(ScriptPlan plan, Parse.TupleReference r, Parse.QualifiedIdentifier id, Frame frame)
		{
			//var resolved = frame.Resolve(id);
			//plan.ResolvedLists.Add(list, resolved);
			//foreach (var i in resolved)
			//	plan.AddReferencedBy(i, statement);
		}

		private void DiscoverClausedExpression(ScriptPlan plan, Frame frame, Parse.ClausedExpression expression)
		{
			var local = new Frame(frame);

			foreach (var fc in expression.ForClauses)
			{
				DiscoverStatement(plan, local, fc.Expression);
				local.AddNonRooted(fc.Name, fc);
			}
			foreach (var lc in expression.LetClauses)
			{
				DiscoverStatement(plan, local, lc.Expression);
				local.AddNonRooted(lc.Name, lc);
			}
			if (expression.WhereClause != null)
				DiscoverStatement(plan, local, expression.WhereClause);
			foreach (var od in expression.OrderDimensions)
				DiscoverStatement(plan, local, od);
			DiscoverStatement(plan, local, expression.Expression); 
		}
	}
}
