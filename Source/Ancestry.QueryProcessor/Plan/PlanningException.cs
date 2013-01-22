
using System;
using System.Resources;

namespace Ancestry.QueryProcessor.Plan
{
	public class PlanningException : QPException
	{
		public enum Codes : int
		{
			/// <summary>Error code 103100: "Invalid rooted identifier.  Rooted identifiers are only allowed in references."</summary>
			InvalidRootedIdentifier = 103100,

			/// <summary>Error code 103101: "Identifier conflict.  There is already an identifier by this name in scope."</summary>
			IdentifierConflict = 103101,

			/// <summary>Error code 103102: "Unknown identifier ({0})."</summary>
			UnknownIdentifier = 103102,

			/// <summary>Error code 103103: "Expecting a reference of type '{0}'; actual: '{1}'."</summary>
			IncorrectTypeReferenced = 103103,

			/// <summary>Error code 103104: "No repository found with an instance of module '{0}'."</summary>
			NoInstanceForModule = 103104,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Ancestry.QueryProcessor.Plan.PlanningException", typeof(PlanningException).Assembly);

		// Constructors
		public PlanningException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public PlanningException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public PlanningException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public PlanningException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public PlanningException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public PlanningException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public PlanningException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public PlanningException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public PlanningException(ErrorSeverity severity, int code, string message, string details, string serverContext, AncestryException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}