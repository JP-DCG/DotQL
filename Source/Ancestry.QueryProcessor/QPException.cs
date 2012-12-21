using System;
using System.Resources;
using System.Reflection;

namespace Ancestry.QueryProcessor
{
	// Base exception class for all exceptions thrown by the DAE.
	// Exception classes deriving from this class should be marked serializable, and provide a serializable constructor.
	public abstract class QPException : AncestryException
	{
		// Constructors
		protected QPException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(resourceManager, errorCode, severity, innerException, paramsValue) {}
		protected QPException(ErrorSeverity severity, int code, string message, string details, string serverContext, AncestryException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}

	public interface ILocatedException
	{
		int Line { get; set; }
		int LinePos { get; set; }
	}
	
	public interface ILocatorException : ILocatedException
	{
		string Locator { get; set; }
	}
}
