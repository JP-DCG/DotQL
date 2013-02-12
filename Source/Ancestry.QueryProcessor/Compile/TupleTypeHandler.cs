using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class TupleTypeHandler : TypeHandler
	{
		public override Expression CompileBinaryExpression(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Equal: 
				case Parse.Operator.NotEqual:
					left = compiler.MaterializeReference(left);
					var right = compiler.MaterializeReference(compiler.CompileExpression(frame, expression.Right));
					
					switch (expression.Operator)
					{
						case Parse.Operator.Equal: return Expression.Equal(left, right);
						case Parse.Operator.NotEqual: return Expression.NotEqual(left, right);
						default: throw new NotSupportedException();
					}

				case Parse.Operator.Dereference: return CompileDereference(compiler, frame, left, expression, typeHint);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		private Expression CompileDereference(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			left = compiler.MaterializeReference(left);

			var local = compiler.AddFrame(frame, expression);
			foreach (var field in left.Type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				local.Add(expression, Name.FromNative(field.Name), field);
				var fieldExpression = Expression.Field(left, field);
				compiler.ExpressionsBySymbol.Add(field, fieldExpression);
			}
			return compiler.CompileExpression(local, expression.Right, typeHint);
		}

	}
}
