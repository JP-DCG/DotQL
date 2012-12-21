using System;
using System.Text;

namespace Ancestry.QueryProcessor.Parse
{
	public class BasicTextEmitter : TextEmitter
	{
		protected virtual void EmitExpression(Expression expression)
		{
			if (expression is UnaryExpression)
				EmitUnaryExpression((UnaryExpression)expression);
			else if (expression is BinaryExpression)
				EmitBinaryExpression((BinaryExpression)expression);
			else if (expression is CallExpression)
				EmitCallExpression((CallExpression)expression);
			else if (expression is ValueExpression)
				EmitValueExpression((ValueExpression)expression);
			else if (expression is IdentifierExpression)
				EmitIdentifierExpression((IdentifierExpression)expression);
			else if (expression is CaseExpression)
				EmitCaseExpression((CaseExpression)expression);
			else if (expression is BetweenExpression)
				EmitBetweenExpression((BetweenExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression == null ? "null" : expression.GetType().Name);
		}

		public static string OperatorToKeyword(Operator op)
		{
			switch (op)
			{
				case Operator.Not: return "not";
				case Operator.Negate: return "-";
				case Operator.BitwiseNot: return "~";
				case Operator.Exists: return "exists";
				case Operator.Addition: return "+";
				case Operator.Subtraction: return "-";
				case Operator.Multiplication: return "*";
				case Operator.Division: return "/";
				case Operator.Div: return "div";
				case Operator.Mod: return "mod";
				case Operator.Power: return "**";
				case Operator.Equal: return "=";
				case Operator.NotEqual: return "<>";
				case Operator.Less: return "<";
				case Operator.InclusiveLess: return "<=";
				case Operator.Greater: return ">";
				case Operator.InclusiveGreater: return ">=";
				case Operator.Compare: return "?=";
				case Operator.And: return "and";
				case Operator.Or: return "or";
				case Operator.Xor: return "xor";
				case Operator.In: return "in";
				case Operator.Like: return "like";
				case Operator.Matches: return "matches";
				case Operator.BitwiseAnd: return "&";
				case Operator.BitwiseOr: return "|";
				case Operator.BitwiseXor: return "^";
				case Operator.ShiftLeft: return "<<";
				case Operator.ShiftRight: return ">>";
				default: throw new LanguageException(LanguageException.Codes.UnknownInstruction, op.ToString());
			}
		}
		
		protected virtual string GetOperatorKeyword(Operator op)
		{
			return OperatorToKeyword(op);
		}

		protected virtual void EmitUnaryExpression(UnaryExpression expression)
		{
			AppendFormat("{0}{1}", GetOperatorKeyword(expression.Operator), "(");
			EmitExpression(expression.Expression);
			Append(")");
		}
		
		protected virtual void EmitBinaryExpression(BinaryExpression expression)
		{
			Append("(");
			EmitExpression(expression.LeftExpression);
			AppendFormat(" {0} ", GetOperatorKeyword(expression.Operator));
			EmitExpression(expression.RightExpression);
			Append(")");
		}
		
		protected virtual void EmitCallExpression(CallExpression expression)
		{
			AppendFormat("{0}{1}", expression.Identifier, "(");
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					AppendFormat("{0} ", ",");
				EmitExpression(expression.Expressions[index]);
			}
			Append(")");
		}
		
		protected virtual void EmitBetweenExpression(BetweenExpression expression)
		{
			Append("(");
			EmitExpression(expression.Expression);
			Append(" between ");
			EmitExpression(expression.LowerExpression);
			Append(" and ");
			EmitExpression(expression.UpperExpression);
			Append(")");
		}
		
		protected virtual void EmitValueExpression(ValueExpression expression)
		{
			switch (expression.Token)
			{
				case TokenType.Nil : Append(Keywords.Null); break;
				case TokenType.String : Append("'" + ((string)expression.Value).Replace("'", "''") + "'"); break;
				case TokenType.Decimal: Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}d", expression.Value)); break;
				case TokenType.Money : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "${0}", expression.Value)); break;
				case TokenType.Boolean : Append(((bool)expression.Value ? "true" : "false")); break;
				case TokenType.Hex: Append("0x" + ((long)expression.Value).ToString("X")); break;
				default : Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", expression.Value)); break;
			}
		}
		
		protected virtual void EmitIdentifierExpression(IdentifierExpression expression)
		{
			Append(expression.Identifier);
		}
		
		protected virtual void EmitCaseExpression(CaseExpression expression)
		{
			AppendFormat("{0}", "case");

			if (expression.Expression != null)
			{
				Append(" ");
				EmitExpression(expression.Expression);
			}
			
			for (int index = 0; index < expression.CaseItems.Count; index++)
			{
				AppendFormat(" {0} ", "when");
				EmitExpression(expression.CaseItems[index].WhenExpression);
				AppendFormat(" {0} ", "then");
				EmitExpression(expression.CaseItems[index].ThenExpression);
			}
			
			if (expression.ElseExpression != null)
			{
				AppendFormat(" {0} ", "else");
				EmitExpression(((CaseElseExpression)expression.ElseExpression).Expression);
			}

			AppendFormat(" {0}", "end");
		}
		
		protected virtual void EmitStatement(Statement statement)
		{
			throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected override void InternalEmit(Statement statement)
		{
			if (statement is Expression)
				EmitExpression((Expression)statement);
			else if (statement is EmptyStatement)
			{
				// do nothing;
			}
			else
				EmitStatement(statement);
		}
		
		protected virtual void EmitListSeparator()
		{
			Append(", ");
		}
		
		protected virtual void EmitStatementTerminator()
		{
			Append(";");
		}
	}
}

