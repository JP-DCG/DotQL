using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
	public class TupleReferenceAttribute : Attribute
	{
		public TupleReferenceAttribute(string[] sourceAttributeNames, string target, string[] targetAttributeNames)
		{
			SourceAttributeNames = sourceAttributeNames;
			Target = target;
			TargetAttributeNames = targetAttributeNames;
		}

		public string[] SourceAttributeNames { get; set; }
		public string Target { get; set; }
		public string[] TargetAttributeNames { get; set; }
	}
}
