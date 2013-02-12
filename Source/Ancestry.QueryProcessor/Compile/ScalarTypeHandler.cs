using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class ScalarTypeHandler : TypeHandler
	{
		public override Expression CompileBinaryExpression(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			left = compiler.MaterializeReference(left);
			
			switch (expression.Operator)
			{
				case Parse.Operator.Addition:
				case Parse.Operator.Subtract:
				case Parse.Operator.Multiply:
				case Parse.Operator.Modulo:
				case Parse.Operator.Divide:
				case Parse.Operator.Power:

				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.And:
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.Or:
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor:
				case Parse.Operator.ShiftLeft:
				case Parse.Operator.ShiftRight:
					{
						var right = compiler.MaterializeReference(compiler.CompileExpression(frame, expression.Right, typeHint));
						return CompileOperator(left, right, expression.Operator);
					}

				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
				case Parse.Operator.InclusiveGreater:
				case Parse.Operator.InclusiveLess:
				case Parse.Operator.Greater:
				case Parse.Operator.Less:
					{
						var right = compiler.MaterializeReference(compiler.CompileExpression(frame, expression.Right));	// (no type hint)
						return CompileOperator(left, right, expression.Operator);
					}

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		public virtual Expression CompileOperator(Expression left, Expression right, Parse.Operator op)
		{
			switch (op)
			{
				case Parse.Operator.Addition: return Expression.Add(left, right);
				case Parse.Operator.Subtract: return Expression.Subtract(left, right);
				case Parse.Operator.Multiply: return Expression.Multiply(left, right);
				case Parse.Operator.Modulo: return Expression.Modulo(left, right);
				case Parse.Operator.Divide: return Expression.Divide(left, right);
				case Parse.Operator.Power: return Expression.Power(left, right);

				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.And: return Expression.And(left, right);
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.Or: return Expression.Or(left, right);
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor: return Expression.ExclusiveOr(left, right);
				case Parse.Operator.ShiftLeft: return Expression.LeftShift(left, right);
				case Parse.Operator.ShiftRight: return Expression.RightShift(left, right);

				case Parse.Operator.Equal: return Expression.Equal(left, right);
				case Parse.Operator.NotEqual: return Expression.NotEqual(left, right);
				case Parse.Operator.InclusiveGreater: return Expression.GreaterThanOrEqual(left, right);
				case Parse.Operator.InclusiveLess: return Expression.LessThanOrEqual(left, right);
				case Parse.Operator.Greater: return Expression.GreaterThan(left, right);
				case Parse.Operator.Less: return Expression.LessThan(left, right);

				default: throw new NotSupportedException();
			}
		}

		public override Expression CompileUnaryExpression(Compiler compiler, Frame frame, Expression inner, Parse.UnaryExpression expression, System.Type typeHint)
		{
			inner = compiler.MaterializeReference(inner);

			switch (expression.Operator)
			{
				case Parse.Operator.Exists: return Expression.Constant(true);
				case Parse.Operator.IsNull: return Expression.Constant(false);
				case Parse.Operator.Negate: return Expression.Negate(inner);
				case Parse.Operator.Not: return Expression.Not(inner);
				//case Parse.Operator.Successor: 
				//case Parse.Operator.Predicessor:

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}
	}
}
