using Ancestry.QueryProcessor.Parse;
using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor
{
	public class QueryOptions
	{
		public List<Using> DefaultUsings = 
			new List<Using> 
			{ 
				new Using { Target = new QualifiedIdentifier { Components = new string[] { "System" } } } 
			};

		public RequestedSla RequestedSla;
		public QueryLimits QueryLimits;
	}
}
