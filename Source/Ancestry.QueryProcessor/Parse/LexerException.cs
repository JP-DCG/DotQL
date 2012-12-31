
using System;
using System.Resources;

namespace Ancestry.QueryProcessor.Parse
{
	/// <summary>Indicates an exception encoutered during lexical analysis.</summary>
	/// <remarks>
	/// The LexerException is thrown whenever the lexical analyzer encouters an error state.
	/// Only the lexer should throw exceptions of this type.
	/// </remarks>
	public class LexerException : QPException
	{
		public enum Codes : int
		{
			/// <summary>Error code 102100: "Unterminated string constant."</summary>
			UnterminatedString = 102100,

			/// <summary>Error code 102101: "Invalid numeric value."</summary>
			InvalidNumericValue = 102101,

			/// <summary>Error code 102102: "Illegal character "{0}" in input string."</summary>
			IllegalInputCharacter = 102102,

			/// <summary>Error code 102103: "{0} expected."</summary>
			TokenExpected = 102103,

			/// <summary>Error code 102104: ""{0}" expected."</summary>
			SymbolExpected = 102104,

			/// <summary>Error code 102105: "No active token."</summary>
			NoActiveToken = 102105,
		
			/// <summary>Error code 102106: "Unexpected end-of-file."</summary>
			UnexpectedEOF = 102106,
			
			/// <summary>Error code 102107: "Unterminated comment."</summary>
			UnterminatedComment = 102107,

			/// <summary>Error code 102108: "Invalid character."</summary>
			InvalidCharacter = 102108,

			/// <summary>Error code 102109: "Invalid character code."</summary>
			InvalidCharacterCode = 102109,

			/// <summary>Error code 102110: "Unsupported escape character ({0})."</summary>
			InvalidEscapeCharacter = 102110,

			/// <summary>Error code 102111: "Invalid type declaration."</summary>
			InvalidTypeDeclaration = 102111
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Ancestry.QueryProcessor.Parse.LexerException", typeof(LexerException).Assembly);

		// Constructors
		public LexerException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public LexerException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public LexerException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public LexerException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public LexerException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public LexerException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public LexerException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public LexerException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public LexerException(ErrorSeverity severity, int code, string message, string details, string serverContext, AncestryException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}