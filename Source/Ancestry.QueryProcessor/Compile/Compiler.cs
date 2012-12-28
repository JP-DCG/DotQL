using Ancestry.QueryProcessor.Execute;
using Ancestry.QueryProcessor.Plan;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Ancestry.QueryProcessor.Compile
{
	public class Compiler
	{
		public ExecutableHandler CreateExecutable(ScriptPlan plan)
		{
			//// TODO: setup app domain with appropriate cache path, shadow copying etc.
			//var domainName = "plan" + DateTime.Now.Ticks.ToString();
			//var domain = AppDomain.CreateDomain(domainName);
			//var an = new AssemblyName("Dynamic." + domainName);

			//// TODO: use RunAndCollect for transient execution
			//var assembly = domain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);

			//var module = assembly.DefineDynamicModule(an.Name + ".dll");

			//var type = module.DefineType("Executable", TypeAttributes.Public, typeof(ExecutableBase));

			//var method = type.DefineMethod("Execute", MethodAttributes.Static | MethodAttributes.Public);

			var execute = 
				Expression.Lambda<ExecuteHandler, object>>
				(
					CompileScript(plan)
				);
			return execute.Compile();

			//// TODO: Pass debug info
			//execute.CompileToMethod(method);
		}

		private Expression CompileScript(ScriptPlan plan)
		{
			return 
				Expression.Block
				(
					plan.Script.Expression != null ? CompileClausedExpression(plan.Script.Expression) : null
				);
		}

		private Expression CompileClausedExpression(Parse.ClausedExpression clausedExpression)
		{
			return CompileExpression(clausedExpression.Expression);
		}

		private Expression CompileExpression(Parse.Expression expression)
		{
			switch (expression.GetType().Name)
			{
				case "LiteralExpression": return CompileLiteral((Parse.LiteralExpression)expression);
				default : return null;
			}
		}

		private Expression CompileLiteral(Parse.LiteralExpression expression)
		{
			switch (expression.TokenType)
			{
				case Parse.TokenType.Char: return Expression.Constant(
			}
		}

		public virtual void SetArgs(Dictionary<string, object> args)
		{
			var t = GetType();
			foreach (var arg in args)
			{
				var field = t.GetField(arg.Key);
				if (field == null)
					throw new ArgumentException("No variable matching given key", arg.Key);
				t.GetField(arg.Key).SetValue(this, arg.Value);
			}
		}
	}
}

