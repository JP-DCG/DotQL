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
		public ExecuteHandler CreateExecutable(ScriptPlan plan)
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

			var args = Expression.Parameter(typeof(Dictionary<string, object>), "args");
			var cancelToken = Expression.Parameter(typeof(CancellationToken), "cancelToken");

			var execute = 
				Expression.Lambda<ExecuteHandler>
				(
					CompileScript(plan, args, cancelToken),
					args,
					cancelToken
				);
			return execute.Compile();

			//// TODO: Pass debug info
			//execute.CompileToMethod(method);
		}

		private Expression CompileScript(ScriptPlan plan, Expression args, Expression cancelToken)
		{
			// Compute result and convert to object (box if needed)
			var result = CompileClausedExpression(plan.Script.Expression);
			if (result.Type.IsValueType)
				result = Expression.Convert(result, typeof(object));

			return 
				Expression.Block
				(
					// TODO: usings, modules, vars (with arg overrides), assignments
					plan.Script.Expression != null ? result : null
				);
		}

		private Expression CompileClausedExpression(Parse.ClausedExpression clausedExpression)
		{
			// TODO: FLWOR functionality
			return CompileExpression(clausedExpression.Expression);
		}

		private Expression CompileExpression(Parse.Expression expression)
		{
			switch (expression.GetType().Name)
			{
				case "LiteralExpression": return CompileLiteral((Parse.LiteralExpression)expression);
				case "BinaryExpression": return CompileBinaryExpression((Parse.BinaryExpression)expression);
				case "ClausedExpression": return CompileClausedExpression((Parse.ClausedExpression)expression);
				default : throw new NotSupportedException(String.Format("Expression type {0} is not supported", expression.GetType().Name));
			}
		}

		private Expression CompileBinaryExpression(Parse.BinaryExpression expression)
		{
			// TODO: if intrinsic type...
			var result = CompileExpression(expression.Expressions[expression.Expressions.Count - 1]);
			for (int i = expression.Operators.Count - 1; i >= 0; i--)
			{
				switch (expression.Operators[i])
				{
					case Parse.Operator.Addition : result = Expression.Add(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Subtract : result = Expression.Subtract(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Multiply : result = Expression.Multiply(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Modulo: result = Expression.Modulo(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Divide: result = Expression.Divide(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.BitwiseAnd:
					case Parse.Operator.And : result = Expression.And(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.BitwiseOr:
					case Parse.Operator.Or: result = Expression.Or(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.BitwiseXor:
					case Parse.Operator.Xor: result = Expression.ExclusiveOr(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Equal: result = Expression.Equal(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.NotEqual : result = Expression.NotEqual(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.ShiftLeft: result = Expression.LeftShift(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.ShiftRight: result = Expression.RightShift(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Power: result = Expression.Power(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.InclusiveGreater: result = Expression.GreaterThanOrEqual(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.InclusiveLess: result = Expression.LessThanOrEqual(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Greater: result = Expression.GreaterThan(CompileExpression(expression.Expressions[i]), result); break;
					case Parse.Operator.Less: result = Expression.LessThan(CompileExpression(expression.Expressions[i]), result); break;
					default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operators[i]));
				}
			}
			return result;
		}

		private Expression CompileLiteral(Parse.LiteralExpression expression)
		{
			return Expression.Constant(expression.Value);
		}
	}
}

