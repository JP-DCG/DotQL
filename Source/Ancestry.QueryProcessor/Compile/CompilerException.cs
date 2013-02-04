
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

			/// <summary>Error code 105102: "The generic type ({0}) passed does not match the type of the target ({1})."</summary>
			MismatchedGeneric = 105102,
			
			/// <summary>Error code 105103: "Recursive declaration.  A declaration must not reference itself directly or indirectly."</summary>
			RecursiveDeclaration = 105103,

			/// <summary>Error code 105104: "Invalid rooted identifier.  Rooted identifiers are only allowed in references."</summary>
			InvalidRootedIdentifier = 105104,

			/// <summary>Error code 105105: "Identifier conflict.  There is already an identifier by this name in scope."</summary>
			IdentifierConflict = 105105,

			/// <summary>Error code 105106: "Unknown identifier ({0})."</summary>
			UnknownIdentifier = 105106,

			/// <summary>Error code 105107: "Expecting a reference of type '{0}'; actual: '{1}'."</summary>
			IncorrectTypeReferenced = 105107,

			/// <summary>Error code 105108: "Dereferencing against type '{0}' is not supported."</summary>
			CannotDereferenceOnType = 105108,

			/// <summary>Error code 105109: "Cannot infer name from an expression of this type."</summary>
			CannotInferNameFromExpression = 105109,
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