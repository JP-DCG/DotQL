
using System;
using System.Resources;

namespace Ancestry.QueryProcessor.Compile
{
	public class CompilerException : QPException
	{
		public enum Codes : int
		{
			/// <summary>Error code 105100: "Identifier '{0}' not found."</summary>
			IdentifierNotFound = 105100,

			/// <summary>Error code 105101: "Reference to type '{0}', expecting '{1}'."</summary>
			IncorrectType = 105101,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Ancestry.QueryProcessor.Compile.CompilerException", typeof(CompilerException).Assembly);

		// Constructors
		public CompilerException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public CompilerException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public CompilerException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public CompilerException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public CompilerException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public CompilerException(ErrorSeverity severity, int code, string message, string details, string serverContext, AncestryException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}