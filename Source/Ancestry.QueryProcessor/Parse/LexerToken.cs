using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Parse
{
	public enum TokenType { Unknown, Symbol, Integer, Hex, Double, Version, String, Char, Date, Time, DateTime, Guid, TimeSpan, EOF, Error }

	public class LexerToken
	{
		public TokenType Type;
		public string Token;
		public int Line;
		public int LinePos;
		public Exception Error;

		/// <summary> Returns the currently active TokenType as a symbol. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a symbol. </remarks>
		public string AsSymbol
		{
			get
			{
				CheckType(TokenType.Symbol);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as a string. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a string. </remarks>
		public string AsString
		{
			get
			{
				CheckType(TokenType.String);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as an integer value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not an integer. </remarks>
		public long AsInteger
		{
			get
			{
				CheckType(TokenType.Integer);
				return Int64.Parse(Token, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		public double AsDouble
		{
			get
			{
				CheckType(TokenType.Double);
				return Double.Parse(Token, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a money value. </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if the current TokenType is not a money literal. </remarks>
		public long AsHex
		{
			get
			{
				CheckType(TokenType.Hex);
				return Int64.Parse(Token, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Ensures that the current TokenType is of the given type.  </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if it is not. </remarks>
		public void CheckType(TokenType token)
		{
			if (Type == TokenType.Error)
				throw Error;
			if (Type != token)
				throw new LexerException(LexerException.Codes.TokenExpected, Enum.GetName(typeof(TokenType), token));
		}

		/// <summary> Ensures that the current TokenType is a symbol equal to the given symbol.  </summary>
		/// <remarks> Will raise a <see cref="LexerException"/> if it is not. </remarks>
		public void CheckSymbol(string symbol)
		{
			if ((Type != TokenType.Symbol) || !String.Equals(Token, symbol, StringComparison.Ordinal))
				throw new LexerException(LexerException.Codes.SymbolExpected, symbol);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public void DebugCheckSymbol(string symbol)
		{
			CheckSymbol(symbol);
		}

		/// <summary> Return true if the token's type is Symbol it matches the given symbol. </summary>
		public bool IsSymbol(string symbol)
		{
			return Type == TokenType.Symbol && Token == symbol;
		}
	}
}
