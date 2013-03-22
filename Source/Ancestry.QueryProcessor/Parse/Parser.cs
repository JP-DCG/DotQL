using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Ancestry.QueryProcessor.Parse
{
	// given an arbitrary input string, return a syntactically valid DotQL parse tree
	public class Parser
	{
		/// <summary> This method allows parsing from any parse method using a string. </summary>
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

		/// <summary> If true, the parser will error if there is text following a script, such as another script. </summary>
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

			while (lexer[1, false].IsSymbol(Keywords.Using))
				script.Usings.Add(Using(lexer));
			while (lexer[1, false].IsSymbol(Keywords.Module))
				script.Modules.Add(Module(lexer));
			while (lexer[1, false].IsSymbol(Keywords.Var))
				script.Vars.Add(Var(lexer));
			while (NextIsClausedSomething(lexer))
			{
				var claused = ClausedAssignmentOrExpression(lexer);
				if (claused is ClausedAssignment)
					script.Assignments.Add((ClausedAssignment)claused);
				else
				{
					script.Expression = (ClausedExpression)claused;
					break;
				}
			}

			if (ErrorIfNotEOF)
				lexer[1, false].CheckType(TokenType.EOF);

			return script;
		}

		/*
				"using" [ Alias: ID ":=" ] Target : ID Version : Version
		*/
		public Using Using(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Using);

			var @using = new Using();
			@using.SetPosition(lexer);

			var id = ID(lexer, true);

			if (lexer[1].IsSymbol(Keywords.Equal))
			{
				@using.Alias = id;
				lexer.NextToken();
				id = ID(lexer, false);
			}

			@using.Target = id;

			lexer.NextToken().CheckType(TokenType.Version);
			@using.Version = lexer[0].AsVersion;

			return @using;
		}

		/*
				"module" Name : ID Version : Version "{" Members : [ moduleMember ]^[","] "}"
		*/
		public ModuleDeclaration Module(Lexer lexer)
        {
            lexer.NextToken().DebugCheckSymbol(Keywords.Module);

            var moduleDeclaration = new ModuleDeclaration();
            moduleDeclaration.SetPosition(lexer);

            moduleDeclaration.Name = ID(lexer, true);

			lexer.NextToken().CheckType(TokenType.Version);

			moduleDeclaration.Version = lexer[0].AsVersion;

            lexer.NextToken().CheckSymbol(Keywords.BeginTupleSet);

            while (!lexer[1].IsSymbol(Keywords.EndTupleSet))
			{
				moduleDeclaration.Members.Add(ModuleMember(lexer));
				OptionalSeparator(lexer);
			}

			lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);

            return moduleDeclaration;
        }

		/*
				moduleMember =
					TypeMember | EnumMember	| ConstMember | VarMember

				TypeMember :=
					Name : ID ":" "typedef" Type : typeDeclaration

				EnumMember :=
					Name : ID ":" "enum" "{" Values : ID^[","] "}"

				ConstMember :=
					Name : ID ":" "const" Expression : expression

				VarMember :=
					Name : ID ":" Type: typeDeclaration
		*/
		public ModuleMember ModuleMember(Lexer lexer)
        {
			ModuleMember moduleMember;

			LexerToken lexerToken = lexer[1];

			var moduleMemberName = ID(lexer, true);

			lexer.NextToken().CheckSymbol(Keywords.AttributeSeparator);

			switch (lexer[1].Token)
			{
				case Keywords.TypeDef:
					lexer.NextToken();
					moduleMember = new TypeMember()
					{
						Type = TypeDeclaration(lexer)
					};
					break;
				case Keywords.Enum:
					lexer.NextToken();

					lexer.NextToken().CheckSymbol(Keywords.BeginTupleSet);

					var enumMember = new EnumMember();

					while (!lexer[1].IsSymbol(Keywords.EndTupleSet))
					{
						enumMember.Values.Add(ID(lexer, true));
						OptionalSeparator(lexer);
					}

					lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);

					moduleMember = enumMember;

					break;
				case Keywords.Const:
					lexer.NextToken();
					moduleMember = new ConstMember()
					{
						Expression = Expression(lexer)
					};
					break;
				default:
					moduleMember = new VarMember()
					{
						Type = TypeDeclaration(lexer)
					};
					break;
			}

			moduleMember.Name = moduleMemberName;
			moduleMember.SetPosition(lexerToken);

			return moduleMember;
        }

		/*
				"var" Name : ID [ ":" Type : typeDeclaration ] [ ":=" Initializer : expression )
		*/
		public VarDeclaration Var(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Var);

			var result = new VarDeclaration();
			result.SetPosition(lexer);
			result.Name = ID(lexer, true);
			if (lexer[1, false].IsSymbol(Keywords.AttributeSeparator))
			{
				lexer.NextToken();
				result.Type = TypeDeclaration(lexer);
			}
			if (lexer[1, false].IsSymbol(Keywords.Assignment))
			{
				lexer.NextToken();
				result.Initializer = Expression(lexer);
			}
			if (result.Type == null && result.Initializer == null)
				throw new ParserException(ParserException.Codes.TypeOrInitializerExpected);
			return result;
		}

		/*
				"set" Target : expression ":=" Source : expression
		*/
		public Assignment Assignment(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Set);

			var result = new Assignment();
			result.SetPosition(lexer);
			result.Target = Expression(lexer);
			lexer.NextToken().CheckSymbol(Keywords.Assignment);
			result.Source = Expression(lexer);
			
			return result;
		}

		/*
			OptionalType | requiredTypes
		*/
		public TypeDeclaration TypeDeclaration(Lexer lexer)
		{
			return OptionalType(lexer);
		}

		/*
				Type : requiredTypes IsRequired : ( "?" | "!" )
		*/
		public TypeDeclaration OptionalType(Lexer lexer)
		{
			var type = RequiredTypes(lexer);
			
			if (lexer[1, false].IsSymbol(Keywords.Optional) || lexer[1, false].IsSymbol(Keywords.Required))
			{
				lexer.NextToken();
				var result = new OptionalType();
				result.SetPosition(lexer);
				result.Type = type;
				result.IsRequired = lexer[0].IsSymbol(Keywords.Required);
				return result;
			}
			return type;
		}

		/*
				ListType | TupleType | SetType | FunctionType | IntervalType | NamedType | TypeOf
		*/
		public TypeDeclaration RequiredTypes(Lexer lexer)
		{
			lexer[1].CheckType(TokenType.Symbol);
			switch (lexer[1].Token)
			{
				case Keywords.BeginList: return ListType(lexer);
				case Keywords.BeginTupleSet: return SetOrTupleType(lexer);
				case Keywords.BeginGroup: return FunctionType(lexer);
				case Keywords.IntervalType: return IntervalType(lexer);
				case Keywords.TypeOf: return TypeOf(lexer);
				default: return NamedType(lexer);
			}
		}

		/*
				"[" Type : typeDeclaration "]"
		*/
		public TypeDeclaration ListType(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginList);

			var result = new ListType();
			result.SetPosition(lexer);
			result.Type = TypeDeclaration(lexer);

			lexer.NextToken().CheckSymbol(Keywords.EndList);

			return result;
		}

		public TypeDeclaration SetOrTupleType(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginTupleSet);

			// Check for no-attribute tuple
			if (lexer[1].IsSymbol(Keywords.AttributeSeparator) || lexer[1].IsSymbol(Keywords.Ref) || lexer[1].IsSymbol(Keywords.Key))
			{
				var result = new TupleType();
				result.SetPosition(lexer);
				TupleTypeMembers(lexer, result);
				lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
				return result;
			}
			else
			{
				// Capture the beginning token for position information
				var beginToken = lexer[0];
				var firstType = TypeDeclaration(lexer);
				if (lexer[1].IsSymbol(Keywords.AttributeSeparator))
				{
					// Treat as an attribute type

					// Validate that the first expression was nothing but an identifier (the attribute name)
					var identifier = firstType as NamedType;
					if (identifier == null)
						throw new ParserException(ParserException.Codes.InvalidAttributeName);

					lexer.NextToken();

					// Manually construct the first attribute and add it
					var attribute = new TupleAttribute();
					attribute.Name = identifier.Target;
					attribute.LineInfo = identifier.LineInfo;
					attribute.Type = TypeDeclaration(lexer);
					var result = new TupleType();
					result.SetPosition(beginToken);
					result.Attributes.Add(attribute);

					OptionalSeparator(lexer);

					// Add remaining attributes
					if (!lexer[1, false].IsSymbol(Keywords.EndTupleSet))
						TupleTypeMembers(lexer, result);

					lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
					return result;
				}
				else
				{
					// Treat as a set selector

					// Manually add already parsed first expression
					var result = new SetType();
					result.SetPosition(beginToken);
					result.Type = firstType;

					lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
					return result;
				}
			}
		}

		private static void OptionalSeparator(Lexer lexer)
		{
			if (lexer[1].IsSymbol(Keywords.Separator))
				lexer.NextToken();
		}

		/*
				( TupleAttribute | TupleReference | TupleKey )*
		*/
		public void TupleTypeMembers(Lexer lexer, TupleType result)
		{
			do
			{
				var member = TupleTypeMember(lexer);
				if (member is TupleAttribute)
					result.Attributes.Add((TupleAttribute)member);
				else if (member is TupleReference)
					result.References.Add((TupleReference)member);
				else if (member is TupleKey)
					result.Keys.Add((TupleKey)member);
				else
					break;
				OptionalSeparator(lexer);
			} while (!lexer[1].IsSymbol(Keywords.EndTupleSet));
		}

		/*
				TupleAttribute :=
					Name : ID ":" Type : typeDeclaration

				TupleReference :=
					"ref" Name : ID "{" SourceAttributeNames : ID* "}" 
						Target : ID "{" TargetAttributeNames : ID* "}"	

				TupleKey :=
					"key" "(" AttributeNames : [ ID ]* ")"
		*/
		public Statement TupleTypeMember(Lexer lexer)
		{
			switch (lexer[1].AsSymbol)
			{
				case Keywords.AttributeSeparator:
					{
						lexer.NextToken();
						// Check for empty tuple designator
						if (lexer[1].IsSymbol(Keywords.EndTupleSet))
							return null;

						// Inferred-named attribute
						var attribute = new AttributeSelector();
						attribute.SetPosition(lexer);
						attribute.Value = Expression(lexer);
						return attribute;
					}
				case Keywords.Ref: return TupleReference(lexer);
				case Keywords.Key: return TupleKey(lexer);
				default: 
					{
						// Named attribute
						var attribute = new TupleAttribute();
						attribute.SetPosition(lexer);
						attribute.Name = ID(lexer, true);
						lexer.NextToken().CheckSymbol(Keywords.AttributeSeparator);
						attribute.Type = TypeDeclaration(lexer);
						return attribute;
					}
			}
		}

		/*
			FunctionType :=
				functionParameters [ "<" TypeParameters : typeDeclaration^[","] ">" ] ":" ReturnType : typeDeclaration

			functionParameters =
				"(" Parameters : [ FunctionParameter ]^[","] ")"
		*/
		public FunctionType FunctionType(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginGroup);

			var result = new FunctionType();
			result.SetPosition(lexer);

			// Parse parameters
			while (!lexer[1].IsSymbol(Keywords.EndGroup))
			{
				result.Parameters.Add(FunctionParameter(lexer));
				OptionalSeparator(lexer);
			}
			lexer.NextToken().CheckSymbol(Keywords.EndGroup);

			// Parse type parameters
			if (lexer[1].IsSymbol(Keywords.Less))
			{
				lexer.NextToken();
				while (!lexer[1].IsSymbol(Keywords.Greater))
				{
					result.TypeParameters.Add(TypeDeclaration(lexer));
					OptionalSeparator(lexer);
				}
				lexer.NextToken();
			}

			lexer.NextToken().CheckSymbol(Keywords.AttributeSeparator);

			result.ReturnType = TypeDeclaration(lexer);

			return result;
		}

		/*
				Name : ID ":" Type : typeDeclaration
		*/
		private FunctionParameter FunctionParameter(Lexer lexer)
		{
			var param = new FunctionParameter();
			param.SetPosition(lexer[1]);
			param.Name = ID(lexer, true);
			lexer.NextToken().CheckSymbol(Keywords.AttributeSeparator);
			param.Type = TypeDeclaration(lexer);
			return param;
		}

		/*
				"interval" Type : typeDeclaration
		*/
		public TypeDeclaration IntervalType(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.IntervalType);
			var result = new IntervalType();
			result.SetPosition(lexer[1]);
			result.Type = TypeDeclaration(lexer);
			return result;
		}

		/*
				"typeof" Expression : expression
		*/
		public TypeDeclaration TypeOf(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.TypeOf);
			var result = new TypeOf();
			result.SetPosition(lexer);
			result.Expression = Expression(lexer);
			return result;
		}

		/*
				Target : ID
		*/
		public TypeDeclaration NamedType(Lexer lexer)
		{
			var result = new NamedType();
			result.SetPosition(lexer[1]);
			result.Target = ID(lexer, false);
			return result;
		}

		public Expression Expression(Lexer lexer)
		{
			return OfExpression(lexer);
		}

		#region Expression Helpers

		protected BinaryExpression AppendToBinaryExpression(Lexer lexer, Expression expression, Func<Lexer, Expression> next)
		{
			var token = lexer.NextToken();
			var result = new BinaryExpression();
			result.SetPosition(lexer);
			result.Left = expression;
			result.Operator = BinaryKeywordToOperator(token.Token);
			result.Right = next(lexer);
			return result;
		}

		public static Operator UnaryKeywordToOperator(string keyword)
		{
			switch (keyword)
			{
				case Keywords.Exists: return Operator.Exists;
				case Keywords.Negate: return Operator.Negate;
				case Keywords.Not: return Operator.Not;
				case Keywords.BitwiseNot: return Operator.BitwiseNot;
				case Keywords.ExtractSingleton: return Operator.IsNull;
				case Keywords.Successor: return Operator.Successor;
				case Keywords.Predicessor: return Operator.Predicessor;
				default: return Operator.Unknown;
			}
		}

		public static Operator BinaryKeywordToOperator(string keyword)
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
				case Keywords.Modulo: return Operator.Modulo;
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
				case Keywords.Extract: return Operator.Extract;
				case Keywords.Embed: return Operator.Embed;
				default: return Operator.Unknown;
			}
		}

		public static bool NextIsClausedSomething(Lexer lexer)
		{
			if (lexer[1].Type == TokenType.Symbol)
				switch (lexer[1].Token)
				{
					case Keywords.For:
					case Keywords.Let:
					case Keywords.Order:
					case Keywords.Where:
					case Keywords.Return:
					case Keywords.Set:
						return true;
				}

			return false;
		}

		public static object TokenToLiteralObject(LexerToken token)
		{
			object value = null;
			switch (token.Type)
			{
				case TokenType.Symbol:
					switch (token.Token)
					{
						//case Keywords.Void: value = void;
						case Keywords.True: value = true; break;
						case Keywords.False: value = false; break;
						default: value = null; break; // case Keywords.Null:
					}
					break;
				case TokenType.Long: value = token.AsLong; break;
				case TokenType.Integer: value = token.AsInteger; break;
				case TokenType.Double: value = token.AsDouble; break;
				case TokenType.String: value = token.AsString; break;
				case TokenType.Name: var name = token.AsName; CheckValidIdentifier(name); value = name; break;
				case TokenType.Char: value = token.AsChar; break;
				//case TokenType.Date: value = token.AsDateTime;	// TODO: Create 1st class Date and Time types
				case TokenType.DateTime: value = token.AsDateTime; break;
				case TokenType.Guid: value = token.AsGuid; break;
				case TokenType.Hex: value = token.AsHex; break;
				//case TokenType.Time: value = token.AsDateTime;
				case TokenType.TimeSpan: value = token.AsTimeSpan; break;
				case TokenType.Version: value = token.AsVersion; break;
				default: throw new NotSupportedException(String.Format("Token type {0} is unsupported.", token.Type));
			}
			return value;
		}

		#endregion

		/*
			( Expression : Expression "of" Type : TypeDeclaration )
		*/
		public Expression OfExpression(Lexer lexer)
		{
			var expression = LogicalBinaryExpression(lexer);
			if (lexer[1].IsSymbol(Keywords.Of))
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
				Left : expression Operator : ( "in" | "or" | "xor" | "like" | "matches" | "??" ) Right : expression
		*/
		public Expression LogicalBinaryExpression(Lexer lexer)
		{
			var expression = LogicalAndExpression(lexer);

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
						expression = AppendToBinaryExpression(lexer, expression, LogicalAndExpression);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
				Left : expression "and" Right : expression
		*/
		public Expression LogicalAndExpression(Lexer lexer)
		{
			var expression = BitwiseBinaryExpression(lexer);

			
			while (lexer[1].IsSymbol(Keywords.And))
				expression = AppendToBinaryExpression(lexer, expression, BitwiseBinaryExpression);

			return expression;
		}

		/*
				Left : expression Operator : ( "^" | "&" | "|" | "<<" | ">>" ) Right : expression
		*/
		public Expression BitwiseBinaryExpression(Lexer lexer)
		{
			var expression = ComparisonExpression(lexer);

			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.BitwiseAnd:
					case Keywords.BitwiseOr:
					case Keywords.BitwiseXor:
					case Keywords.ShiftLeft:
					case Keywords.ShiftRight:
						expression = AppendToBinaryExpression(lexer, expression, ComparisonExpression);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
				Left : expression Operator : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) Right : expression
		*/
		public Expression ComparisonExpression(Lexer lexer)
		{
			var expression = AdditiveExpression(lexer);

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
						expression = AppendToBinaryExpression(lexer, expression, AdditiveExpression);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
				Left : expression Operator : ( "+" | "-" ) Right : expression
		*/
		public Expression AdditiveExpression(Lexer lexer)
		{
			var expression = MultiplicativeExpression(lexer);

			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Addition:
					case Keywords.Subtract:
						expression = AppendToBinaryExpression(lexer, expression, MultiplicativeExpression);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
				Left : expression Operator : ( "*" | "/" | "%" ) Right : expression
		*/
		public Expression MultiplicativeExpression(Lexer lexer)
		{
			var expression = IntervalSelector(lexer);

			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Multiply:
					case Keywords.Divide:
					case Keywords.Modulo:
						expression = AppendToBinaryExpression(lexer, expression, IntervalSelector);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
			Begin : Expression ".." End : Expression
		*/
		public Expression IntervalSelector(Lexer lexer)
		{
			var expression = ExponentExpression(lexer);

			while (lexer[1].IsSymbol(Keywords.IntervalValue))
			{
				lexer.NextToken();
				var result = new IntervalSelector();
				result.SetPosition(lexer);
				result.Begin = expression;
				result.End = ExponentExpression(lexer);
				expression = result;
			}
			return expression;
		}

		/*
			Left : expression "**" Right : expression
		*/
		public Expression ExponentExpression(Lexer lexer)
		{
			var expression = CallExpression(lexer);

			while (lexer[1].IsSymbol(Keywords.Power))
				expression = AppendToBinaryExpression(lexer, expression, CallExpression);
			return expression;
		}

		/*
				Function : expression [ "`" TypeArguments : typeDeclaration^[","] "`" ] 
				( 
					( "(" Arguments : expression^[","] ")" ) 
						| ( "->" Argument : expression )
				)
		*/
		public Expression CallExpression(Lexer lexer)
		{
			var expression = UnaryExpression(lexer);

			while (lexer[1, false].IsSymbol(Keywords.BeginGroup) || lexer[1, false].IsSymbol(Keywords.Invoke) || lexer[1, false].IsSymbol(Keywords.TypeArguments))
			{
				var result = new CallExpression();
				result.Function = expression;
				result.SetPosition(lexer);

				// Type arguments
				if (lexer[1, false].IsSymbol(Keywords.TypeArguments))
				{
					lexer.NextToken();
					while (!lexer[1].IsSymbol(Keywords.TypeArguments))
					{
						result.TypeArguments.Add(TypeDeclaration(lexer));
						OptionalSeparator(lexer);
					}
					lexer.NextToken();
				}

				lexer.NextToken();
				if (lexer[0].IsSymbol(Keywords.Invoke))
				{
					// Tuple argument
					result.Argument = UnaryExpression(lexer);
				}
				else
				{
					// Arguments
					lexer[0].CheckSymbol(Keywords.BeginGroup);
					while (!lexer[1].IsSymbol(Keywords.EndGroup))
					{
						result.Arguments.Add(Expression(lexer));
						OptionalSeparator(lexer);
					}
					lexer.NextToken().CheckSymbol(Keywords.EndGroup);
				}
				expression = result;
			}
			return expression;
		}

		/*
				UnaryExpression : 
				(
					( Operator : ( "-" | "~" | "not" | "exists" ) Expression : expression )
						| ( Expression : expression Operator : ( "@@" | "++" | "--" ) )
				)
		*/
		public Expression UnaryExpression(Lexer lexer)
		{
			// Prefix operators
			if (lexer[1, false].Type == TokenType.Symbol)
				switch (lexer[1].Token)
				{
					case Keywords.Negate:
					case Keywords.BitwiseNot:
					case Keywords.Not:
					case Keywords.Exists:
					{
						lexer.NextToken();
						var result = new UnaryExpression();
						result.SetPosition(lexer);
						result.Operator = UnaryKeywordToOperator(lexer[0].Token);
						result.Expression = UnaryExpression(lexer);

						// If negation against a literal number, invert the value rather than add a negate operator
						if (result.Operator == Operator.Negate)
						{
							var literalExpression = result.Expression as LiteralExpression;
							if (literalExpression != null && literalExpression.Value != null)
							{
								switch (literalExpression.Value.GetType().Name)
								{
									case "Double":
										literalExpression.Value = -(double)literalExpression.Value;
										return literalExpression;
									case "Int32":
										literalExpression.Value = -(int)literalExpression.Value;
										return literalExpression;
									case "Int64":
										literalExpression.Value = -(long)literalExpression.Value;
										return literalExpression;
								}
							}
						}
						return result;
					}
				}

			var expression = DereferenceExpression(lexer);

			// Postfix operators
			while (lexer[1, false].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.ExtractSingleton:
					case Keywords.Successor:
					case Keywords.Predicessor:
						lexer.NextToken();
						var result = new UnaryExpression();
						result.SetPosition(lexer);
						result.Operator = UnaryKeywordToOperator(lexer[0].Token);
						result.Expression = expression;
						expression = result;
						continue;
					default: break;
				}
				break;
			}

			return expression;
		}

		/*
				( Left : expression Operator : ( "." | "@" | "<<" ) Right : expression )
		*/
		public Expression DereferenceExpression(Lexer lexer)
		{
			var expression = TermExpression(lexer);

			while (lexer[1].Type == TokenType.Symbol)
			{
				switch (lexer[1].Token)
				{
					case Keywords.Dereference:
					case Keywords.Extract:
					case Keywords.Embed:
						expression = AppendToBinaryExpression(lexer, expression, TermExpression);
						break;
					default:
						return expression;
				}
			}
			return expression;
		}

		/*
			"(" Expression ")"
				| ListSelector
				| TupleSelector
				| SetSelector
				| FunctionSelector
				| IdentifierExpression
				| IntegerLiteral
				| DoubleLiteral
				| CharacterLiteral
				| StringLiteral
				| BooleanLiteral : ( "true" | "false" )
				| NullLiteral : "null"
				| VoidLiteral : "void"
				| CaseExpression
				| IfExpression
				| TryExpression
				| ClausedExpression
		*/
		public Expression TermExpression(Lexer lexer)
		{
			switch (lexer[1].Type)
			{
				case TokenType.Symbol:
					switch (lexer[1].Token)
					{
						case Keywords.BeginList: return ListSelector(lexer);

						case Keywords.BeginTupleSet: return SetOrTupleSelector(lexer);

						case Keywords.BeginGroup: return FunctionSelectorOrGroup(lexer);
						
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

				default: throw new ParserException(ParserException.Codes.ExpressionExpected);
			}
		}

		/*
			"[" Items : [ expression ]^[","] "]"
		*/
		public Expression ListSelector(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginList);

			var result = new ListSelector();
			result.SetPosition(lexer);
			
			while (!lexer[1].IsSymbol(Keywords.EndList))
			{
				result.Items.Add(Expression(lexer));
				OptionalSeparator(lexer);
			}
			lexer.NextToken().CheckSymbol(Keywords.EndList);

			return result;
		}

		/*
			TupleSelector :=
				"{" ":" | Members : ( TupleAttributeSelector | TupleReference | TupleKey )^[","] "}"

			TupleAttributeSelector :=
				[ Name : ID ] ":" Value : expression

			SetSelector :=
				"{" Items : [ expression ]^[","] "}"
		*/
		public Expression SetOrTupleSelector(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginTupleSet);
			
			// Check for tuple
			if (lexer[1].IsSymbol(Keywords.AttributeSeparator) || lexer[1].IsSymbol(Keywords.Ref) || lexer[1].IsSymbol(Keywords.Key))
			{
				var result = new TupleSelector();
				result.SetPosition(lexer);
				TupleSelectorMembers(lexer, result);
				lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
				return result;
			}
			else
			{
				// Capture the beginning token for position information
				var beginToken = lexer[0];

				Expression firstExpression = null;

				// Check for empty set
				if (!lexer[1].IsSymbol(Keywords.EndTupleSet))
				{
					// Obtain expression, which may need to be reinterpreted as an attribute name
					firstExpression = Expression(lexer);

					// Check if an attribute selector and thus a tuple
					if (lexer[1].IsSymbol(Keywords.AttributeSeparator))
					{
						// Validate that the first expression was nothing but an identifier (the attribute name)
						var identifier = firstExpression as IdentifierExpression;
						if (identifier == null)
							throw new ParserException(ParserException.Codes.InvalidAttributeName);

						lexer.NextToken();

						// Manually construct the first attribute and add it
						var attribute = new AttributeSelector();
						attribute.Name = identifier.Target;
						attribute.LineInfo = identifier.LineInfo;
						attribute.Value = Expression(lexer);
						var result = new TupleSelector();
						result.SetPosition(beginToken);
						result.Attributes.Add(attribute);

						OptionalSeparator(lexer);

						// Add remaining attributes
						TupleSelectorMembers(lexer, result);
					
						lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
						return result;
					}
				}

				// Treat as a set selector
				{
					// Manually add already parsed first expression
					var result = new SetSelector();
					result.SetPosition(beginToken);
					if (firstExpression != null)
						result.Items.Add(firstExpression);

					// Add remaining items
					while (lexer[1].Type != TokenType.EOF && !lexer[1].IsSymbol(Keywords.EndTupleSet))
					{
						result.Items.Add(Expression(lexer));
						OptionalSeparator(lexer);
					}

					lexer.NextToken().CheckSymbol(Keywords.EndTupleSet);
					return result;
				}
			}
		}

		/*
				( TupleAttributeSelector | TupleReference | TupleKey )^[","]
		*/
		public void TupleSelectorMembers(Lexer lexer, TupleSelector result)
		{
			while (!lexer[1].IsSymbol(Keywords.EndTupleSet))
			{
				var member = TupleSelectorMember(lexer);
				if (member is AttributeSelector)
					result.Attributes.Add((AttributeSelector)member);
				else if (member is TupleReference)
					result.References.Add((TupleReference)member);
				else if (member is TupleKey)
					result.Keys.Add((TupleKey)member);
				else
					break;
				OptionalSeparator(lexer);
			}
		}

		/*
			TupleAttribute :=
				Name : ID ":" Type : typeDeclaration

			TupleReference :=
				"ref" Name : ID "{" SourceAttributeNames : ID^[","] "}" 
					Target : ID "{" TargetAttributeNames : ID^[","] "}"	

			TupleKey :=
				"key" "{" AttributeNames : [ ID ]^[","] "}"
		*/
		public Statement TupleSelectorMember(Lexer lexer)
		{
			switch (lexer[1].AsSymbol)
			{
				case Keywords.AttributeSeparator:
				{
					lexer.NextToken();
					// Check for empty tuple designator
					if (lexer[1].IsSymbol(Keywords.EndTupleSet))
						return null;

					// Inferred-named attribute
					var attribute = new AttributeSelector();
					attribute.SetPosition(lexer);
					attribute.Value = Expression(lexer);
					return attribute;
				}
				case Keywords.Ref: return TupleReference(lexer);
				case Keywords.Key: return TupleKey(lexer);
				default:
				{
					// Named attribute
					var attribute = new AttributeSelector();
					attribute.SetPosition(lexer[1]);
					attribute.Name = ID(lexer, true);
					lexer.NextToken().CheckSymbol(Keywords.AttributeSeparator);
					attribute.Value = Expression(lexer);
					return attribute;
				} 
			}
		}

		/*
				"ref" Name : ID "{" SourceAttributeNames : ID^[","] "}" 
					Target : ID "{" TargetAttributeNames : ID^[","] "}"	
		*/
		public TupleReference TupleReference(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Ref);

			var result = new TupleReference();
			result.SetPosition(lexer);
			result.Name = ID(lexer, true);

			lexer.NextToken().CheckSymbol(Keywords.BeginTupleSet);
			do
			{
				result.SourceAttributeNames.Add(ID(lexer, false));
				OptionalSeparator(lexer);
			} while (!lexer[1].IsSymbol(Keywords.EndTupleSet));
			lexer.NextToken();

			result.Target = ID(lexer, true);

			lexer.NextToken().CheckSymbol(Keywords.BeginTupleSet);
			do
			{
				result.TargetAttributeNames.Add(ID(lexer, false));
				OptionalSeparator(lexer);
			} while (!lexer[1].IsSymbol(Keywords.EndTupleSet));
			lexer.NextToken();

			return result;
		}

		/*
				"key" "{" AttributeNames : [ ID ]^[","] "}"
		*/
		public TupleKey TupleKey(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Key);

			var result = new TupleKey();
			result.SetPosition(lexer);

			lexer.NextToken().CheckSymbol(Keywords.BeginTupleSet);
			while (!lexer[1].IsSymbol(Keywords.EndTupleSet))
			{
				result.AttributeNames.Add(ID(lexer, false));
				OptionalSeparator(lexer);
			}
			lexer.NextToken();

			return result;
		}

		/*
			IsRooted : [ '\' ] Items : Identifier^'\'
		*/
		public ID ID(Lexer lexer, bool checkReserved)
		{
			var result = new ID();
			result.SetPosition(lexer[1]);

			// Determine if the id is rooted
			if (lexer[1].IsSymbol(Keywords.Qualifier))
			{
				lexer.NextToken();
				result.IsRooted = true;
			}
			else
				result.IsRooted = false;

			// Collect the components
			var items = new List<string>();
			while (true)
			{
				var identifier = Identifier(lexer, checkReserved);
				items.Add(identifier);

				// Continue if another qualifier is found
				if (lexer[1, false].IsSymbol(Keywords.Qualifier))
					lexer.NextToken();
				else
					break;
			}

			result.Components = items.ToArray();
			return result;
		}

		public string Identifier(Lexer lexer, bool checkReserved)
		{
			lexer.NextToken().CheckType(TokenType.Symbol);
			var result = lexer[0].Token;
			CheckValidIdentifier(result);
			if (checkReserved && Tokenizer.IsReservedWord(result))
				throw new ParserException(ParserException.Codes.ReservedWordIdentifier, result);
			return result;
		}

		public static void CheckValidIdentifier(string id)
		{
			if (!Tokenizer.IsValidIdentifier(id))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, id);
		}

		private static void CheckValidIdentifier(Name name)
		{
			foreach (var c in name.Components)
				CheckValidIdentifier(c);
		}

		/*
			(group) :=
				"(" expression ")"
		
			FunctionSelector :=
				functionParameters ":" [ ReturnType : typeDeclaration ] Expression : ClausedExpression
		*/
		public Expression FunctionSelectorOrGroup(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.BeginGroup);
			var startToken = lexer[0];

			// Check for function with no arguments
			if (lexer[1, false].IsSymbol(Keywords.EndGroup))
			{
				lexer.NextToken();
				var result = new FunctionSelector();
				result.SetPosition(startToken);
				if (lexer[1, false].IsSymbol(Keywords.AttributeSeparator))
				{
					lexer.NextToken();
					result.ReturnType = TypeDeclaration(lexer);
				}
				result.Expression = ClausedExpression(lexer);
				return result;
			}

			var expressionToken = lexer[1];
			var expression = Expression(lexer);

			// Check for function - look for an attribute separator
			if (lexer[1].IsSymbol(Keywords.AttributeSeparator))
			{
				if (!(expression is IdentifierExpression))
					throw new ParserException(ParserException.Codes.InvalidAttributeName);
				lexer.NextToken();

				// Manually construct the first param
				var param = new FunctionParameter();
				param.SetPosition(expressionToken);
				param.Name = ((IdentifierExpression)expression).Target;
				param.Type = TypeDeclaration(lexer);

				var result = new FunctionSelector();
				result.SetPosition(startToken);
				result.Parameters.Add(param);
				OptionalSeparator(lexer);

				// Additional parameters
				while (!lexer[1].IsSymbol(Keywords.EndGroup))
				{
					result.Parameters.Add(FunctionParameter(lexer));
					OptionalSeparator(lexer);
				}
				lexer.NextToken().CheckSymbol(Keywords.EndGroup);

				// Optional return type
				if (lexer[1, false].IsSymbol(Keywords.AttributeSeparator))
				{
					lexer.NextToken();
					result.ReturnType = TypeDeclaration(lexer);
				}

				// Body
				result.Expression = ClausedExpression(lexer);
				return result;
			}
				
			lexer.NextToken().CheckSymbol(Keywords.EndGroup);
			return expression;
		}

		/*
				"case" [ [ IsStrict : "strict" ] TestExpression : expression ]
					Items : ( "when" WhenExpression : expression "then" ThenExpression : expression )*
					"else" ElseExpression : expression
				"end"		
		*/
		public Expression CaseExpression(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Case);

			var result = new CaseExpression();
			result.SetPosition(lexer);

			// Strict and test
			if (lexer[1].IsSymbol(Keywords.Strict))
			{
				result.IsStrict = true;
				lexer.NextToken();
				result.TestExpression = Expression(lexer);
			}
			else if (!lexer[1].IsSymbol(Keywords.When))
				result.TestExpression = Expression(lexer);
			
			// Items
			do
			{
				lexer.NextToken().CheckSymbol(Keywords.When);
				var item = new CaseItem();
				item.SetPosition(lexer);
				item.WhenExpression = Expression(lexer);
				lexer.NextToken().CheckSymbol(Keywords.Then);
				item.ThenExpression = Expression(lexer);
                result.Items.Add(item);
			} while (lexer[1].IsSymbol(Keywords.When));

			// Else
			if (!result.IsStrict)
			{
				lexer.NextToken().CheckSymbol(Keywords.Else);
				result.ElseExpression = Expression(lexer);
			}

			lexer.NextToken().CheckSymbol(Keywords.End);
			return result;
		}

		/*
				"if" TestExpression : expression
					"then" ThenExpression : expression
					"else" ElseExpression : expression 
		*/
		public Expression IfExpression(Lexer lexer)
		{
			lexer.NextToken().CheckSymbol(Keywords.If);
			var result = new IfExpression();
			result.SetPosition(lexer);
			result.TestExpression = Expression(lexer);
			
			lexer.NextToken().CheckSymbol(Keywords.Then);
			result.ThenExpression = Expression(lexer);

			lexer.NextToken().CheckSymbol(Keywords.Else);
			result.ElseExpression = Expression(lexer);
			
			return result;
		}

		/*
				"try" TryExpression : expression "catch" CatchExpression : expression
		*/
		public Expression TryExpression(Lexer lexer)
		{
			lexer.NextToken().CheckSymbol(Keywords.Try);
			var result = new TryCatchExpression();
			result.SetPosition(lexer);
			result.TryExpression = Expression(lexer);
			
			lexer.NextToken().CheckSymbol(Keywords.Catch);
			result.CatchExpression = Expression(lexer);
			
			return result;
		}

		/*
			Name : ID
		*/
		public Expression IdentifierExpression(Lexer lexer)
		{
			var result = new IdentifierExpression();
			result.SetPosition(lexer[1]);
			result.Target = ID(lexer, false);
			return result;
		}

		public Expression LiteralExpression(Lexer lexer)
		{
			lexer.NextToken();
			var result = new LiteralExpression { Value = TokenToLiteralObject(lexer[0]) };
			result.SetPosition(lexer);
			return result;
		}

		/*
			ClausedAssignment :=
				ForClauses : [ ForClause ]*
				LetClauses : [ LetClause ]*
				[ "where" WhereClause : expression ]
				Assignments : ("set" Target : expression ":=" Source : expression)*
		*/
		/// <summary> Used when it isn't certain whether a claused assignment or claused expression follows. </summary>
		public Statement ClausedAssignmentOrExpression(Lexer lexer)
		{
			var expression = new ClausedExpression();
			expression.SetPosition(lexer);

			var assignment = new ClausedAssignment();
			assignment.SetPosition(lexer);

			while (lexer[1, false].IsSymbol(Keywords.For))
			{
				var fc = ForClause(lexer);
				expression.ForClauses.Add(fc);
				assignment.ForClauses.Add(fc);
			}
			while (lexer[1, false].IsSymbol(Keywords.Let))
			{
				var lc = LetClause(lexer);
				expression.LetClauses.Add(lc);
				assignment.LetClauses.Add(lc);
			}
			if (lexer[1, false].IsSymbol(Keywords.Where))
			{
				lexer.NextToken().CheckSymbol(Keywords.Where);
				var where = Expression(lexer);
				expression.WhereClause = where;
				assignment.WhereClause = where;
			}
			if (lexer[1, false].IsSymbol(Keywords.Order))
			{
				if (expression.ForClauses.Count == 0)
					throw new ParserException(ParserException.Codes.OrderOnlyValidWithForClause);
				OrderClause(lexer, expression.OrderDimensions);
				lexer.NextToken().CheckSymbol(Keywords.Return);
				expression.Expression = Expression(lexer);
				return expression;
			}
			else if (lexer[1, false].IsSymbol(Keywords.Return))
			{
				lexer.NextToken().CheckSymbol(Keywords.Return);
				expression.Expression = Expression(lexer);
				return expression;
			}
			else
			{
				lexer[1, false].CheckSymbol(Keywords.Set);
				while (lexer[1, false].IsSymbol(Keywords.Set))
					assignment.Assignments.Add(Assignment(lexer));
				return assignment;
			}
		}

		/*
				ForClauses : [ ForClause ]*
				LetClauses : [ LetClause ]*
				[ "where" WhereClause : expression ]
				[ "order" "(" OrderDimensions : OrderDimension^[","] ")" ]
				"return" Expression : expression
		*/
		public ClausedExpression ClausedExpression(Lexer lexer)
		{
			var result = new ClausedExpression();
			result.SetPosition(lexer);

			while (lexer[1].IsSymbol(Keywords.For))
				result.ForClauses.Add(ForClause(lexer));
			while (lexer[1].IsSymbol(Keywords.Let))
				result.LetClauses.Add(LetClause(lexer));
			if (lexer[1].IsSymbol(Keywords.Where))
				result.WhereClause = Expression(lexer);
			if (lexer[1].IsSymbol(Keywords.Order))
			{
				if (result.ForClauses.Count == 0)
					throw new ParserException(ParserException.Codes.OrderOnlyValidWithForClause);
				OrderClause(lexer, result.OrderDimensions);
			}
			lexer.NextToken().CheckSymbol(Keywords.Return);
			result.Expression = Expression(lexer);

			return result;
		}

		/*
			"let" Name : ID ":=" Expression : Expression
		*/
		public LetClause LetClause(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Let);

			var result = new LetClause();
			result.SetPosition(lexer);

			result.Name = ID(lexer, true);
			
			lexer.NextToken().CheckSymbol(Keywords.Assignment);

			result.Expression = Expression(lexer);

			return result;
		}

		/*
			"for" Name : ID "in" Expression : Expression
		*/
		public ForClause ForClause(Lexer lexer)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.For);

			var result = new ForClause();
			result.SetPosition(lexer);

			result.Name = ID(lexer, true);

			lexer.NextToken().CheckSymbol(Keywords.In);

			result.Expression = Expression(lexer);

			return result;
		}

		/*
				"order" "(" OrderDimensions : OrderDimension^[","] ")"

			OrderDimension :=
				Expression : expression [ Direction : ( "asc" | "desc" ) ]
		*/
		public void OrderClause(Lexer lexer, List<OrderDimension> results)
		{
			lexer.NextToken().DebugCheckSymbol(Keywords.Order);

			lexer.NextToken().CheckSymbol(Keywords.BeginGroup);

			while (!lexer[1].IsSymbol(Keywords.EndGroup))
			{
				var result = new OrderDimension();			
				result.SetPosition(lexer);
				result.Expression = Expression(lexer);

				bool asc;
				if (asc = lexer[1].IsSymbol(Keywords.Asc) || lexer[1].IsSymbol(Keywords.Desc))
					result.Ascending = asc;

				results.Add(result);

				OptionalSeparator(lexer);
			}
		}
	}
}

