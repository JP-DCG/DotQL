
using System;
using System.Text;
using System.Runtime.Serialization;

namespace Ancestry.QueryProcessor.Parse
{
    public class Lexer
    {
		private const int LookAheadCount = 3;

		/// <remarks> It is an error to access the current TokenType until <see cref="NextToken"/> has been called. </remarks>
        public Lexer(string input)
        {
			_tokenizer = new Tokenizer(input);
			for (int i = 0; i < LookAheadCount; i++)
				_tokens[i] = new LexerToken();
			for (int i = 0; i < (LookAheadCount - 1); i++)
				if (!ReadNext(i))
					break;
        }

		private Tokenizer _tokenizer;

		/// <summary> Buffer of tokens (clock allocation). </summary>
		private LexerToken[] _tokens = new LexerToken[LookAheadCount];
		/// <summary> Current position within the token buffer of the current token. </summary>
		/// <remarks> Initialized in the constructor to last position so that the lexer starts on a "crack". </remarks>
		private int _currentIndex = LookAheadCount - 1;

		/// <summary> Reads the next token into the specified location within the buffer. </summary>
		/// <returns> True if the read token type is not EOF. </returns>
		private bool ReadNext(int index)
		{
			LexerToken token = _tokens[index];
			try
			{
				token.Type = _tokenizer.NextToken();
				token.Token = _tokenizer.Token;
			}
			catch (Exception exception)
			{
				token.Type = TokenType.Error;
				token.Error = exception;
			}
			token.Line = _tokenizer.Line;
			token.LinePos = _tokenizer.LinePos;
			return (token.Type != TokenType.EOF) && (token.Type != TokenType.Error);
		}

		/// <summary> The token a specific number of tokens ahead of the current token. </summary>
		public LexerToken this[int index, bool checkActive]
		{
			get
			{
				System.Diagnostics.Debug.Assert(index < LookAheadCount, "Lexer look ahead attempt exceeds maximum");
				LexerToken token = _tokens[(_currentIndex + index) % LookAheadCount];
				if (checkActive && (token.Type == TokenType.Unknown))
					throw new LexerException(LexerException.Codes.NoActiveToken);
				return token;
			}
		}

		public LexerToken this[int index]
		{
			get
			{
				return this[index, true];
			}
		}

		/// <summary>Advances the current token.</summary>
		/// <returns>Returns the now active token.</returns>
		public LexerToken NextToken()
		{
			ReadNext(_currentIndex);
			_currentIndex = (_currentIndex + 1) % LookAheadCount;
			LexerToken token = _tokens[_currentIndex];
			if (token.Type == TokenType.EOF)
				throw new LexerException(LexerException.Codes.UnexpectedEOF);
			if (token.Type == TokenType.Error)
				throw token.Error;
			return token;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public LexerToken PeekToken(int count)
		{
			LexerToken token = this[count];
			if (token.Type == TokenType.Error)
				throw token.Error;
			return token;
		}

		/// <summary> Gets the symbol the specified number of tokens ahead without advancing the current token. </summary>
		/// <remarks> If the token is not a symbol, returns an empty string. </remarks>
		public string PeekTokenSymbol(int count)
		{
			LexerToken token = this[count];
			if (token.Type == TokenType.Symbol)
				return token.Token;
			else if (token.Type == TokenType.Error)
				throw token.Error;
			else
				return String.Empty;
		}
    }

    public class Tokenizer
    {
		// Boolean constants
		public const string True = "true";
		public const string False = "false";
		public const string Null = "null";
		public const string Void = "void";

		public Tokenizer(string input)
			: base()
		{
			_input = input;
			ReadNext();
		}

		private string _input;
		private int _pos = -1;

		private Char _current;
		public Char Current { get { return _current; } }

		private Char _next;
		public Char Next
		{
			get
			{
				if (_nextEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return _next;
			}
		}

		private bool _nextEOF;
		public bool NextEOF { get { return _nextEOF; } }

		public Char Peek
		{
			get
			{
				if (PeekEOF)
					throw new LexerException(LexerException.Codes.UnexpectedEOF);
				return _input[_pos + 1];
			}
		}

		public bool PeekEOF
		{
			get
			{
				return _pos + 1 >= _input.Length;
			}
		}

		private int _line = 1;
		private int _linePos = 1;
		private int _tokenLine = 1;
		private int _tokenLinePos = 1;

		/// <summary> The line number (one-based) of the beginning of the last read token. </summary>
		public int Line { get { return _tokenLine; } }
		
		/// <summary> The offset position (one-based) of the beginning of the last read token. </summary>
		public int LinePos { get { return _tokenLinePos; } }

		private TokenType _tokenType;
		public TokenType TokenType { get { return _tokenType; } }

		private string _token;
		public string Token
		{
			get { return _token; }
		}

		private StringBuilder _builder = new StringBuilder();

		/// <summary> Pre-reads the next TokenType (does not affect current). </summary>
		private void ReadNext()
		{
			_pos++;
			_nextEOF = _pos >= _input.Length;
			if (!_nextEOF) 
				_next = _input[_pos];
		}

		public void Advance()
		{
			_current = Next;
			ReadNext();
			if (_current == '\n')
			{
				_line++;
				_linePos = 1;
			}
			else
				_linePos++;
		}

		/// <summary> Skips any whitespace. </summary>
		public void SkipWhiteSpace()
		{
			while (!_nextEOF && Char.IsWhiteSpace(_next))
				Advance();
		}

		/// <summary> Skips all comments and whitepace. </summary>
		public void SkipComments()
		{
			SkipWhiteSpace();
			while (!PeekEOF) // There is only the possibility of a comment if there are at least two characters
			{
				if (SkipLineComments())
					continue;

				// Skip block comments
				if ((_next == '/') && !PeekEOF && (Peek == '*'))
				{
					Advance();
					Advance();
					int blockCommentDepth = 1;
					while (blockCommentDepth > 0)
					{
						if (SkipLineComments())		// Line comments may be \\* block delimiters, so ignore them inside block comments
							continue;
						if (PeekEOF)
							throw new LexerException(LexerException.Codes.UnterminatedComment);
						Char peek = Peek;
						bool peekEOF = PeekEOF;
						if ((_next == '/') && !peekEOF && (peek == '*'))
						{
							blockCommentDepth++;
							Advance();
						}
						else if ((_next == '*') && !peekEOF && (peek == '/'))
						{
							blockCommentDepth--;
							Advance();
						}
						Advance();
					}
					SkipWhiteSpace();
					continue;
				}
				break;
			}
		}

		/// <remarks> Used internally by SkipComments(). </remarks>
		/// <returns> True if comment parsing should continue. </return>
		private bool SkipLineComments()
		{
			if ((_next == '-') && !PeekEOF && (Peek == '-'))
			{
				Advance();
				Advance();
				while (!_nextEOF && (_next != '\n'))
					Advance();
				if (!_nextEOF)
				{
					Advance();
					SkipWhiteSpace();
					return true;
				}
			}
			return false;
		}

		private bool IsSymbol(char charValue)
		{
			switch (charValue)
			{
				case '.':
				case ',':
				case ';':
				case '?':
				case ':':
				case '(':
				case ')':
				case '{':
				case '}':
				case '[':
				case ']':
				case '*':
				case '/':
				case '%':
				case '\\':
				case '~':
				case '&':
				case '|':
				case '^':
				case '+':
				case '-':
				case '>':
				case '<':
				case '=':
					return true;
				default:
					return false;
			}
		}

		public TokenType NextToken()
		{
			SkipComments();

			// Clear the TokenType
			_builder.Length = 0;
			_token = String.Empty;
			_tokenLine = _line;
			_tokenLinePos = _linePos;

			if (!_nextEOF)
			{
				if (Char.IsLetter(_next) || (_next == '_'))			// Identifiers
				{
					Advance();
					_builder.Append(_current);
					while (!_nextEOF && (Char.IsLetterOrDigit(_next) || (_next == '_')))
					{
						Advance();
						_builder.Append(_current);
					}
					_token = _builder.ToString();
					_tokenType = TokenType.Symbol;
				}
				else if (IsSymbol(_next))							// Symbols
				{
					_tokenType = TokenType.Symbol;
					Advance();
					_builder.Append(_current);
					switch (_current)
					{
						case ':':
							if (!_nextEOF && (_next == '='))
							{
								Advance();
								_builder.Append(_current);
							}
							break;

						case '?':
							if (!_nextEOF)
								switch (_next)
								{
									case '=':
									case '?':
										Advance();
										_builder.Append(_current);
									break;
								}
							break;

						case '*':
							if (!_nextEOF && (_next == '*'))
							{
								Advance();
								_builder.Append(_current);
							}
							break;
							
						case '>':
							if (!_nextEOF)
								switch (_next)
								{
									case '=':
									case '>':
										Advance();
										_builder.Append(_current);
									break;
								}
							break;

						case '<':
							if (!_nextEOF)
								switch (_next)
								{
									case '=':
									case '>':
									case '<':
										Advance();
										_builder.Append(_current);
									break;
								}
							break;
						case '=':
							if (!_nextEOF && _next == '>')
							{
								Advance();
								_builder.Append(_current);
								break;
							}
							break;
						case '-':
							if (!_nextEOF && _next == '>')
							{
								Advance();
								_builder.Append(_current);
								break;
							}
							break;
						case '.':
							if (!_nextEOF && _next == '.')
							{
								Advance();
								_builder.Append(_current);
								break;
							}
							break;
							
					}
					_token = _builder.ToString();
				}
				else if	(Char.IsDigit(_next))		// Numbers
				{
					Advance();

					bool digitSatisfied = false;	// at least one digit required

					if ((_current == '0') && (!_nextEOF && ((_next == 'x') || (_next == 'X'))))
					{
						_tokenType = TokenType.Hex;
						Advance();
					}
					else
					{
						digitSatisfied = true;
						_builder.Append(_current);
						_tokenType = TokenType.Integer;
					}

					bool done = false;
					int periodsHit = 0;
					bool hitScalar = false;

					while (!done && !_nextEOF)
					{
						switch (_next)
						{
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
								if (_tokenType == TokenType.Integer && ExceedsInt32(_builder))
									_tokenType = TokenType.Long;
								else if (_tokenType == Parse.TokenType.Hex && ExceedsInt32Hex(_builder))
									_tokenType = TokenType.LongHex;
								Advance();
								digitSatisfied = true;
								_builder.Append(_current);
								break;

							case 'A':
							case 'a':
							case 'B':
							case 'b':
							case 'C':
							case 'c':
							case 'D':
							case 'd':
							case 'f':
							case 'F':
								if (_tokenType != TokenType.Hex && _tokenType != TokenType.LongHex)
								{
									done = true;
									break;
								}
								Advance();
								digitSatisfied = true;
								_builder.Append(_current);
								break;
								
							case 'E':
							case 'e':
								if ((_tokenType != TokenType.Hex && _tokenType != TokenType.LongHex) || (!hitScalar && digitSatisfied))
								{
									done = true;
									break;
								}
								Advance();
								if (_tokenType == TokenType.Hex || _tokenType == TokenType.LongHex)
								{
									digitSatisfied = true;
									_builder.Append(_current);
								}
								else if (!hitScalar && digitSatisfied)
								{
									hitScalar = true;
									digitSatisfied = false;
									_tokenType = TokenType.Double;
									_builder.Append(_current);
									if ((_next == '-') || (_next == '+'))
									{
										Advance();
										_builder.Append(_current);
									}
								}
								break;

							case '.':
								// If interval, or we've fully satisfied a version, stop looking for digits
								if ((!PeekEOF && Peek == '.') || periodsHit >= 3)
								{
									done = true;	
									break;
								}

								if (!digitSatisfied || hitScalar)
									throw new LexerException(LexerException.Codes.InvalidNumericValue);

								if (periodsHit == 0)
								{
									if (_tokenType == TokenType.Hex || _tokenType == TokenType.LongHex)
										throw new LexerException(LexerException.Codes.InvalidNumericValue);
									if (_tokenType == TokenType.Integer)
										_tokenType = TokenType.Double;
								}
								else if (periodsHit == 1)
									_tokenType = TokenType.Version;

								Advance();
								_builder.Append(_current);
								periodsHit++;
								digitSatisfied = false;
								break;

							default:
								done = true;
								break;
						}
					}
					if (!digitSatisfied)
						throw new LexerException(LexerException.Codes.InvalidNumericValue);
					_token = _builder.ToString();
				}	
				else if (_next == '"')	// C-style string
				{
					_tokenType = TokenType.String;
					Advance();
					var terminated = false;
					while (!terminated)
					{
						if (_nextEOF)
							throw new LexerException(LexerException.Codes.UnterminatedString);	// Better error than EOF
						Advance();
						switch (_current)
						{
							case '\\' :
								if (_nextEOF)
									throw new LexerException(LexerException.Codes.InvalidEscapeCharacter);
								switch (_next)
								{
									case '\\' : 
									case '\"' : _builder.Append(_next); Advance(); break;
									case 'n' : _builder.Append('\n'); Advance(); break;
									case 'r' : _builder.Append('\r'); Advance(); break;
									case 't' : _builder.Append('\t'); Advance(); break;
									default: throw new LexerException(LexerException.Codes.InvalidEscapeCharacter, _next);
								}
								break;

							case '\"' : 
								terminated = true; 
								break;
							
							default : 
								_builder.Append(_current); 
								break;
						}
					}
					_token = _builder.ToString();
				}
				else if (_next == '\'')	// Pascal-style string
				{
					_tokenType = TokenType.String;
					Advance();
					while (true)
					{
						if (_nextEOF)
							throw new LexerException(LexerException.Codes.UnterminatedString);	// Better error than EOF
						Advance();
						if (_current == '\'')
						{
							if (!_nextEOF && _next == '\'')
								Advance();
							else
								break;
						}
						_builder.Append(_current);
					}
					if (!_nextEOF)
						switch (_next)
						{
							case 'c' :
								Advance(); 
								_tokenType = TokenType.Char;
								if (_builder.Length != 1)
									throw new LexerException(LexerException.Codes.InvalidCharacter);
								break;
							case 'n' :
								Advance();
								_tokenType = TokenType.Name;
								break;
							case 'd' :
								Advance(); 
								if (!_nextEOF && _next == 't')
								{
									Advance();
									_tokenType = TokenType.DateTime;
								}
								else
									_tokenType = TokenType.Date;
								break;
							case 't' :
								Advance(); 
								if (!_nextEOF && _next == 's')
								{
									Advance();
									_tokenType = TokenType.TimeSpan;
								}
								else
									_tokenType = TokenType.Time;
								break;
							case 'g' :
								Advance();
								_tokenType = TokenType.Guid;
								break;
						}
					_token = _builder.ToString();
				}
				else if (_next == '#')
				{
					_tokenType = TokenType.String;
					Advance();
					var digitSatisfied = false;
					int code = 0;
					while (!_nextEOF && Char.IsDigit(_next))
					{
						digitSatisfied = true;
						Advance();
						code = code * 10 + (_current - '0');
					}
					if (!digitSatisfied)
						throw new LexerException(LexerException.Codes.InvalidCharacterCode);
					_token = Convert.ToChar(code).ToString();
				}
				else
					throw new LexerException(LexerException.Codes.IllegalInputCharacter, _next); 
			}
			else
				_tokenType = TokenType.EOF;

			return _tokenType;
		}

		private static bool ExceedsInt32(StringBuilder builder)
		{
			int x;
			return builder.Length >= 10 && !Int32.TryParse(builder.ToString(), out x);
		}

		private static bool ExceedsInt32Hex(StringBuilder builder)
		{
			return builder.Length >= 8;
		}

		public static bool IsValidIdentifier(string subject)
		{
			if (subject.Length < 1)
				return false;
			if (!(Char.IsLetter(subject[0]) || (subject[0] == '_')))
				return false;
			for (int i = 1; i < subject.Length; i++)
				if (!(Char.IsLetterOrDigit(subject[i]) || (subject[i] == '_')))
					return false;
			return true;
		}

		public static bool IsReservedWord(string identifier)
		{
			return ReservedWords.Contains(identifier);
		}
	}
}

