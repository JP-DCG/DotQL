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
			var result = CompileExpression(expression.Left);
			switch (expression.Operator)
			{
				case Parse.Operator.Addition : return Expression.Add(result, CompileExpression(expression.Right));
				case Parse.Operator.Subtract : return Expression.Subtract(result, CompileExpression(expression.Right));
				case Parse.Operator.Multiply : return Expression.Multiply(result, CompileExpression(expression.Right));
				case Parse.Operator.Modulo: return Expression.Modulo(result, CompileExpression(expression.Right));
				case Parse.Operator.Divide: return Expression.Divide(result, CompileExpression(expression.Right));
				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.And : return Expression.And(result, CompileExpression(expression.Right));
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.Or: return Expression.Or(result, CompileExpression(expression.Right));
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor: return Expression.ExclusiveOr(result, CompileExpression(expression.Right));
				case Parse.Operator.Equal: return Expression.Equal(result, CompileExpression(expression.Right));
				case Parse.Operator.NotEqual : return Expression.NotEqual(result, CompileExpression(expression.Right));
				case Parse.Operator.ShiftLeft: return Expression.LeftShift(result, CompileExpression(expression.Right));
				case Parse.Operator.ShiftRight: return Expression.RightShift(result, CompileExpression(expression.Right));
				case Parse.Operator.Power: return Expression.Power(result, CompileExpression(expression.Right));
				case Parse.Operator.InclusiveGreater: return Expression.GreaterThanOrEqual(result, CompileExpression(expression.Right));
				case Parse.Operator.InclusiveLess: return Expression.LessThanOrEqual(result, CompileExpression(expression.Right));
				case Parse.Operator.Greater: return Expression.GreaterThan(result, CompileExpression(expression.Right));
				case Parse.Operator.Less: return Expression.LessThan(result, CompileExpression(expression.Right));
				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		private Expression CompileLiteral(Parse.LiteralExpression expression)
		{
			return Expression.Constant(expression.Value);
		}
	}
}

