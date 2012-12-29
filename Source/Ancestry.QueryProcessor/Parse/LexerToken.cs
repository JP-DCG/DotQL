using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Parse
{
	public enum TokenType { Unknown, Symbol, Long, Integer, Hex, LongHex, Double, Version, String, Char, Date, Time, DateTime, Guid, TimeSpan, EOF, Error }

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
				DebugCheckType(TokenType.Symbol);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as a string. </summary>
		public string AsString
		{
			get
			{
				DebugCheckType(TokenType.String);
				return Token;
			}
		}

		/// <summary> Returns the currently active TokenType as a char. </summary>
		public char AsChar
		{
			get
			{
				DebugCheckType(TokenType.Char);
				return Token[0];
			}
		}

		/// <summary> Returns the currently active TokenType as a long integer value. </summary>
		public long AsLong
		{
			get
			{
				DebugCheckType(TokenType.Long);
				return Int64.Parse(Token, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as an integer value. </summary>
		public int AsInteger
		{
			get
			{
				DebugCheckType(TokenType.Integer);
				return Int32.Parse(Token, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a DateTime value. </summary>
		public DateTime AsDateTime
		{
			get
			{
				DebugCheckType(TokenType.DateTime);
				return DateTime.Parse(Token, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
			}
		}

		/// <summary> Returns the currently active TokenType as a TimeSpan value. </summary>
		public TimeSpan AsTimeSpan
		{
			get
			{
				DebugCheckType(TokenType.TimeSpan);
				return TimeSpan.Parse(Token, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a Double value. </summary>
		public double AsDouble
		{
			get
			{
				DebugCheckType(TokenType.Double);
				return Double.Parse(Token, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a hex value. </summary>
		public int AsHex
		{
			get
			{
				DebugCheckType(TokenType.Hex);
				return Int32.Parse(Token, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a long hex value. </summary>
		public long AsLongHex
		{
			get
			{
				DebugCheckType(TokenType.LongHex);
				return Int64.Parse(Token, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary> Returns the currently active TokenType as a GUID value. </summary>
		public Guid AsGuid
		{
			get
			{
				DebugCheckType(TokenType.Guid);
				return Guid.Parse(Token);
			}
		}

		/// <summary> Returns the currently active TokenType as a Version value. </summary>
		public Version AsVersion
		{
			get
			{
				DebugCheckType(TokenType.Version);
				return Version.Parse(Token);
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

		[System.Diagnostics.Conditional("DEBUG")]
		public void DebugCheckType(TokenType token)
		{
			CheckType(token);
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
