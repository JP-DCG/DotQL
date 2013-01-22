using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ancestry.QueryProcessor;
using System.Reflection;

namespace Ancestry.QueryProcessor.Plan
{
	public class Planner
	{
		private QueryOptions _options;

		public Planner(QueryOptions options)
		{
			_options = options;
		}

		public ScriptPlan PlanScript(Parse.Script script)
		{
			var plan = new ScriptPlan(script);

			var modules = FindModules();

			// Find all the symbols and frames and resolve
			DiscoverScript(plan, modules);

			// TODO: Determine expression data types and characteristics

			// TODO: Determine storage support boundaries

			// TODO: Create access plans for all tables

			return plan;
		}

		private IEnumerable<System.Type> FindModules()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(Type.ModuleAttribute), true).Length > 0))
					yield return type;
		}

		private void DiscoverScript(ScriptPlan plan, IEnumerable<System.Type> modules)
		{
			// Add root frame
			var local = plan.Global;

			var modulesByName = new Frame();
			foreach (var module in modules)
				modulesByName.AddNonRooted(new QualifiedID { Components = module.FullName.Split('.') }, module);

			// Import symbols for usings
			foreach (var u in plan.Script.Usings)
			{
				var module = modulesByName.Resolve<System.Type>(u.Target);
				object instance = null;
				foreach (var repo in _options.Repositories)
				{
					instance = repo.GetInstance(module);
					if (instance != null)
					{
						local.AddNonRooted(u.Target, instance);
						break;
					}
				}
				if (instance == null)
					throw new PlanningException(PlanningException.Codes.NoInstanceForModule, u.Target.ToString());
				
				foreach (var method in module.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
					local.AddNonRooted(new QualifiedID { Components = u.Target.Components.Union(new string[] { method.Name }).ToArray() }, method);
			}

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
			switch (statement.GetType().Name)
			{
				case "ClausedExpression" : DiscoverClausedExpression(plan, frame, (Parse.ClausedExpression)statement); break;
				case "TupleType" : DiscoverTupleType(plan, frame, (Parse.TupleType)statement); break;
				case "IdentifierExpression" : DiscoverIdentifierExpression(plan, frame, (Parse.IdentifierExpression)statement); break;
				case "NamedType" : DiscoverNamedType(plan, frame, (Parse.NamedType)statement); break;
				case "ModuleDeclaration" : DiscoverModuleDeclaration(plan, frame, (Parse.ModuleDeclaration)statement); break;
				case "FunctionSelector" : DiscoverFunctionSelector(plan, frame, (Parse.FunctionSelector)statement); break;
				default :
					foreach (var s in statement.GetChildren())
						DiscoverStatement(plan, frame, s);
					break;
			}
		}

		private void DiscoverFunctionSelector(ScriptPlan plan, Frame frame, Parse.FunctionSelector functionSelector)
		{
			var local = new Frame(frame);
			plan.Frames.Add(functionSelector, local);
			
			foreach (var p in functionSelector.Parameters)
				local.AddNonRooted(p.Name, p);
			
			DiscoverStatement(plan, local, functionSelector.Expression);
		}

		private void DiscoverModuleDeclaration(ScriptPlan plan, Frame frame, Parse.ModuleDeclaration moduleDeclaration)
		{
			var local = new Frame(frame);
			plan.Frames.Add(moduleDeclaration, local);

			// Gather the module's symbols
			foreach (var member in moduleDeclaration.Members)
			{
				local.AddNonRooted(member.Name, member);

				// Populate qualified enumeration members
				if (member is Parse.EnumMember)
					foreach (var e in ((Parse.EnumMember)member).Values)
						local.AddNonRooted(new QualifiedID() { Components = member.Name.Components.Union(e.Components).ToArray() }, member);
			}

			foreach (var member in moduleDeclaration.Members)
				DiscoverStatement(plan, local, member);
		}

		private void DiscoverNamedType(ScriptPlan plan, Frame frame, Parse.NamedType namedType)
		{
			plan.AddReference(frame.Resolve<Parse.ISymbol>(namedType.Target), namedType.Target);
		}

		private void DiscoverIdentifierExpression(ScriptPlan plan, Frame frame, Parse.IdentifierExpression identifier)
		{
			plan.AddReference(frame.Resolve<Parse.ISymbol>(identifier.Target), identifier.Target);
		}

		private void DiscoverTupleType(ScriptPlan plan, Frame frame, Parse.TupleType tupleType)
		{
			var local = new Frame(null);
			plan.Frames.Add(tupleType, local);

			// Discover all attributes as symbols
			foreach (var a in tupleType.Attributes)
			{
				local.AddNonRooted(a.Name, a);
			}

			// Resolve source reference columns
			foreach (var k in tupleType.Keys)
			{
				Resolve(plan, k.AttributeNames, local);
			}

			// Resolve key reference columns
			foreach (var r in tupleType.References)
			{
				Resolve(plan, r.SourceAttributeNames, local);
				var target = plan.Global.Resolve<Parse.Statement>(r.Target);
				plan.AddReference((Parse.ISymbol)target, r.Target);
				Resolve(plan, r.TargetAttributeNames, plan.Frames[target]);
			}
		}

		private void DiscoverClausedExpression(ScriptPlan plan, Frame frame, Parse.ClausedExpression expression)
		{
			var local = new Frame(frame);
			plan.Frames.Add(expression, local);

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
				DiscoverStatement(plan, local, od.Expression);
			DiscoverStatement(plan, local, expression.Expression); 
		}

		private void Resolve(ScriptPlan plan, Parse.QualifiedIdentifier id, Frame frame)
		{
			plan.AddReference(frame.Resolve<Parse.ISymbol>(id), id);
		}

		private void Resolve(ScriptPlan plan, IEnumerable<Parse.QualifiedIdentifier> list, Frame frame)
		{
			foreach (var item in list)
				plan.AddReference(frame.Resolve<Parse.ISymbol>(item), item);
		}
	}
}
