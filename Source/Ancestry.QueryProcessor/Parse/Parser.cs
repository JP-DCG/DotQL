using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Ancestry.QueryProcessor.Type;

namespace Ancestry.QueryProcessor.Parse
{
	// given an arbitrary input string, return a syntactically valid AQL parse tree
	public class Parser
	{
		public static R ParseFrom<R>(Func<Lexer, R> func, string input)
		{
			Lexer lexer = new Lexer(input);
			try
			{
				return func(lexer);
			}
			catch (Exception e)
			{
				throw new SyntaxException(lexer, e);
			}
		}

		public bool ErrorIfNotEOF { get; set; }

		/*
			Script :=
				Usings : [ UsingClause ]*
				Modules : [ ModuleDeclaration ]*
				Vars : [ VarDeclaration ]*
				Assignments : [ Assignment ]*
				Expression : [ ClausedExpression ]
		*/
		public Script Script(Lexer lexer)
		{
			var script = new Script();
			script.SetPosition(lexer);

			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Using)
				script.Usings.Add(Using(lexer));
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Module)
				script.Modules.Add(Module(lexer));
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Var)
				script.Vars.Add(Var(lexer));
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Set)
				script.Usings.Add(Assignment(lexer));
			if (lexer[1].Type == TokenType.Symbol)
				script.Expression = ClausedExpression(lexer);

			if (ErrorIfNotEOF)
				lexer[1].CheckType(TokenType.EOF);

			return script;
		}

		public Using Using(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		public ModuleDeclaration Module(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		public VarDeclaration Var(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		public Using Assignment(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		public Expression Expression(Lexer lexer)
		{
			return OfExpression(lexer);
		}

		/*
			ListType | TupleType | SetType | FunctionType | IntervalType | NamedType
		*/
		public TypeDeclaration TypeDeclaration(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		#region Expression Helpers

		private void AppendToBinaryExpression(Lexer lexer, Expression expression, LexerToken next, ref BinaryExpression result)
		{
			lexer.NextToken();
			if (result == null)
			{
				result = new BinaryExpression();
				result.SetPosition(lexer);
				result.Expressions.Add(expression);
			}
			result.Operators.Add(BinaryKeywordToOperator(next.Token));
			result.Expressions.Add(LogicalAndExpression(lexer));
		}

		public Operator UnaryKeywordToOperator(string keyword)
		{
			switch (keyword)
			{
				case Keywords.Exists: return Operator.Exists;
				case Keywords.Negate: return Operator.Negate;
				case Keywords.Not: return Operator.Not;
				case Keywords.IsNull: return Operator.IsNull;
				case Keywords.Successor: return Operator.Successor;
				case Keywords.Predicessor: return Operator.Predicessor;
				default: return Operator.Unknown;
			}
		}

		public Operator BinaryKeywordToOperator(string keyword)
		{
			switch (keyword)
			{
				case Keywords.In: return Operator.In;
				case Keywords.Or: return Operator.Or;
				case Keywords.Xor: return Operator.Xor;
				case Keywords.Like: return Operator.Like;
				case Keywords.Matches: return Operator.Matches;
				case Keywords.And: return Operator.And;
				case Keywords.Addition: return Operator.Addition;
				case Keywords.BitwiseAnd: return Operator.BitwiseAnd;
				case Keywords.BitwiseNot: return Operator.BitwiseNot;
				case Keywords.BitwiseOr: return Operator.BitwiseOr;
				case Keywords.BitwiseXor: return Operator.BitwiseXor;
				case Keywords.Compare: return Operator.Compare;
				case Keywords.Divide: return Operator.Divide;
				case Keywords.Equal: return Operator.Equal;
				case Keywords.Exists: return Operator.Exists;
				case Keywords.Greater: return Operator.Greater;
				case Keywords.InclusiveGreater: return Operator.InclusiveGreater;
				case Keywords.Modulus: return Operator.Modulus;
				case Keywords.NotEqual: return Operator.NotEqual;
				case Keywords.InclusiveLess: return Operator.InclusiveLess;
				case Keywords.Less: return Operator.Less;
				case Keywords.Multiply: return Operator.Multiply;
				case Keywords.Power: return Operator.Power;
				case Keywords.ShiftLeft: return Operator.ShiftLeft;
				case Keywords.ShiftRight: return Operator.ShiftRight;
				case Keywords.Subtract: return Operator.Subtract;
				case Keywords.IfNull: return Operator.IfNull;
				case Keywords.Dereference: return Operator.Dereference;
				default: return Operator.Unknown;
			}
		}

		public bool NextIsClausedExpression(Lexer lexer)
		{
			if (lexer[1].Type == TokenType.Symbol)
				switch (lexer[1].Token)
				{
					case Keywords.For:
					case Keywords.Let:
					case Keywords.Order:
					case Keywords.Where:
					case Keywords.Return:
						return true;
				}

			return false;
		}

		#endregion

		/*
			( Expression : Expression "of" Type : TypeDeclaration )
		*/
		public Expression OfExpression(Lexer lexer)
		{
			var expression = LogicalBinaryExpression(lexer);
			if (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Of)
			{
				lexer.NextToken();
				var result = new OfExpression();
				result.SetPosition(lexer);
				result.Expression = expression;
				result.Type = TypeDeclaration(lexer);
				return result;
			}
			return expression;
		}

		/*
			( Expressions : Expression )^( Operators : ( "in" | "or" | "xor" | "like" | "matches" | "?" ) )
		*/
		public Expression LogicalBinaryExpression(Lexer lexer)
		{
			var expression = LogicalAndExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.In:
					case Keywords.Or:
					case Keywords.Xor:
					case Keywords.Like:
					case Keywords.Matches:
					case Keywords.IfNull:
						AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
						break;
					default:
						return result ?? expression;
				}
			}
			return result ?? expression;
		}

		/*
			( Expressions : Expression )^"and"
		*/
		public Expression LogicalAndExpression(Lexer lexer)
		{
			var expression = BitwiseBinaryExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.And)
				AppendToBinaryExpression(lexer, expression, lexer[1], ref result);

			return result ?? expression;
		}

		/*
			( Expressions : Expression )^( Operators : ( "^" | "&" | "|" | "<<" | ">>" ) )
		*/
		public Expression BitwiseBinaryExpression(Lexer lexer)
		{
			var expression = ComparisonExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.BitwiseAnd:
					case Keywords.BitwiseOr:
					case Keywords.BitwiseXor:
					case Keywords.ShiftLeft:
					case Keywords.ShiftRight:
						AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
						break;
					default:
						return result ?? expression;
				}
			}
			return result ?? expression;
		}

		/*
			( Expressions : Expression )^( Operators : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) )
		*/
		public Expression ComparisonExpression(Lexer lexer)
		{
			var expression = AdditiveExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Equal:
					case Keywords.NotEqual:
					case Keywords.Less:
					case Keywords.Greater:
					case Keywords.InclusiveLess:
					case Keywords.InclusiveGreater:
					case Keywords.Compare:
						AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
						break;
					default:
						return result ?? expression;
				}
			}
			return result ?? expression;
		}

		/*
			( Expressions : Expression )^( Operators : ( "+" | "-" ) )
		*/
		public Expression AdditiveExpression(Lexer lexer)
		{
			var expression = MultiplicativeExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Addition:
					case Keywords.Subtract:
						AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
						break;
					default:
						return result ?? expression;
				}
			}
			return result ?? expression;
		}

		/*
			( Expressions : Expression )^( Operators : ( "*" | "/" | "%" | ".." ) )
		*/
		public Expression MultiplicativeExpression(Lexer lexer)
		{
			var expression = IntervalSelector(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Multiply:
					case Keywords.Divide:
					case Keywords.Modulus:
						AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
						break;
					default:
						return result ?? expression;
				}
			}
			return result ?? expression;
		}

		/*
			Begin : Expression ".." End : Expression
		*/
		public Expression IntervalSelector(Lexer lexer)
		{
			var expression = ExponentExpression(lexer);

			if (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.IntervalValue)
			{
				lexer.NextToken();
				var result = new IntervalSelector();
				result.SetPosition(lexer);
				result.Begin = expression;
				result.End = Expression(lexer);
				return result;
			}
			return expression;
		}

		/*
			( Expressions : Expression )^"**"
		*/
		public Expression ExponentExpression(Lexer lexer)
		{
			var expression = UnaryExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Power)
				AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
			return result ?? expression;
		}

		/*
			Operator : ( "++" | "--" | "-" | "~" | "not" | "exists" | "??" ) Expression : Expression
		*/
		public Expression UnaryExpression(Lexer lexer)
		{
			var next = lexer[1];
			if (next.Type == TokenType.Symbol)
				switch (next.Token)
				{
					case Keywords.Predicessor:
					case Keywords.Successor:
					case Keywords.Negate:
					case Keywords.BitwiseNot:
					case Keywords.Not:
					case Keywords.Exists:
					case Keywords.IsNull:
						lexer.NextToken();
						var result = new UnaryExpression();
						result.SetPosition(lexer);
						result.Operator = UnaryKeywordToOperator(next.Token);
						result.Expression = DereferenceExpression(lexer);
						return result;
				}

			return DereferenceExpression(lexer);
		}

		/*
			( Expressions : Expression )^"."
		*/
		public Expression DereferenceExpression(Lexer lexer)
		{
			var expression = IndexerExpression(lexer);

			BinaryExpression result = null;
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Dereference)
				AppendToBinaryExpression(lexer, expression, lexer[1], ref result);
			return result ?? expression;
		}

		/*
			Expression : Expression "[" Indexer : [ Expression ] "]"
		*/
		public Expression IndexerExpression(Lexer lexer)
		{
			var expression = CallExpression(lexer);

			if (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.BeginIndexer)
			{
				lexer.NextToken();
				var result = new IndexerExpression();
				result.SetPosition(lexer);
				result.Expression = expression;
				result.Indexer = Expression(lexer);
				lexer.NextToken().CheckSymbol(Keywords.EndIndexer);
				return result;
			}
			return expression;
		}

		/*
			Expression : Expression [ "<" TypeArguments : TypeDeclaration* ">" ]
				( "(" Arguments : [ Expression ]* ")" ) | ( "=>" Argument : Expression ) 
		*/
		public Expression CallExpression(Lexer lexer)
		{
			var expression = TermExpression(lexer);

			if (lexer[1].Type == TokenType.Symbol)
			{
				if (lexer[1].Token == Keywords.BeginGroup)
				{
					lexer.NextToken();
					var result = new CallExpression();
					result.SetPosition(lexer);
					result.Expression = expression;

					while (lexer[1].Type == TokenType.Symbol && lexer[1].Token != Keywords.EndGroup)
						result.Arguments.Add(Expression(lexer));

					lexer.NextToken().CheckSymbol(Keywords.EndGroup);
					return result;
				}
				else if (lexer[1].Token == Keywords.Call)
				{
					lexer.NextToken();
					var result = new CallExpression();
					result.SetPosition(lexer);
					result.Expression = expression;

					result.Argument = Expression(lexer);

					return result;
				}
			}
			return expression;
		}

		public Expression TermExpression(Lexer lexer)
		{
			switch (lexer[1].Type)
			{
				case TokenType.Symbol:
					switch (lexer[1].Token)
					{
						case Keywords.BeginListSelector: return ListSelector(lexer);

						case Keywords.BeginSet: return ListOrTupleSelector(lexer);

						case Keywords.Function: return FunctionSelector(lexer);
						
						case Keywords.Case: return CaseExpression(lexer);
						
						case Keywords.If: return IfExpression(lexer);
						
						case Keywords.Try: return TryExpression(lexer);
						
						case Keywords.For:
						case Keywords.Let:
						case Keywords.Where:
						case Keywords.Order:
						case Keywords.Return: return ClausedExpression(lexer);

						case Keywords.Null:
						case Keywords.Void:
						case Keywords.True:
						case Keywords.False: return LiteralExpression(lexer);
						
						default: return IdentifierExpression(lexer);
					}
				case TokenType.Integer:
				case TokenType.Double:
				case TokenType.String:
				case TokenType.Char:
				case TokenType.Date:
				case TokenType.DateTime:
				case TokenType.Guid:
				case TokenType.Hex:
				case TokenType.Time:
				case TokenType.TimeSpan:
				case TokenType.Version: return LiteralExpression(lexer);
			}
		}

		public Expression LiteralExpression(Lexer lexer)
		{
			lexer.NextToken();
			var result = new LiteralExpression { TokenType = lexer[0].Type, Value = lexer[0].Token };
			result.SetPosition(lexer);
			return result;
		}

		/*
			ForClauses : [ ForClause ]*
			LetClauses : [ LetClause ]*
			[ "where" WhereClause : Expression ]
			[ "order" "(" OrderDimensions : OrderDimension* ")" ]
			"return" Expression : Expression
		*/
		public ClausedExpression ClausedExpression(Lexer lexer)
		{
			var result = new ClausedExpression();
			result.SetPosition(lexer);

			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.For)
				result.ForClauses.Add(ForClause(lexer));
			while (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Let)
				result.LetClauses.Add(LetClause(lexer));
			if (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Where)
				result.WhereClause = Expression(lexer);
			if (lexer[1].Type == TokenType.Symbol && lexer[1].Token == Keywords.Order)
				result.OrderDimensions = OrderDimensions(lexer);
			lexer[1].CheckSymbol(Keywords.Return);
			result.Expression = Expression(lexer);

			return result;
		}

		/*
			"let" Name : QualifiedIdentifier ":=" Expression : Expression
		*/
		public LetClause LetClause(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		/*
			"for" Name : QualifiedIdentifier "in" Expression : Expression
		*/
		public ForClause ForClause(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		/*
			( Expression : Expression [ Direction : ( "asc" | "desc" ) ] )*
		*/
		public List<OrderDimension> OrderDimensions(Lexer lexer)
		{
			throw new NotImplementedException();
		}

		//protected bool IsValidIdentifier(string AIdentifier)
		//{
		//	for (int LIndex = 0; LIndex < AIdentifier.Length; LIndex++)
		//		if 
		//			(
		//				(
		//					(LIndex == 0) && 
		//					!(Char.IsLetter(AIdentifier[LIndex]) || (AIdentifier[LIndex] == '_'))
		//				) || 
		//				(
		//					(LIndex != 0) && 
		//					!(Char.IsLetterOrDigit(AIdentifier[LIndex]) || (AIdentifier[LIndex] == '_'))
		//				)
		//			)
		//			return false;
		//	return true;
		//}
        
		//protected bool IsReservedWord(string AIdentifier)
		//{
		//	//return ReservedWords.Contains(AIdentifier);
		//	return false;
		//}
        
		//protected string Identifier(Lexer ALexer)
		//{
		//	ALexer.NextToken().CheckType(TokenType.Symbol);
		//	if (!IsValidIdentifier(ALexer[0].Token))
		//		throw new ParserException(ParserException.Codes.InvalidIdentifier, ALexer[0].Token);
		//	// TODO: Reserved words in SQL
		//	if (IsReservedWord(ALexer[0].Token))
		//		throw new ParserException(ParserException.Codes.ReservedWordIdentifier, ALexer[0].Token);

		//	return ALexer[0].Token;
		//}

	}
}

