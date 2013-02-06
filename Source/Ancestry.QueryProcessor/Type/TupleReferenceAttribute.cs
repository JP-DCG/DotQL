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
		public TupleReferenceAttribute(string name, string[] sourceAttributeNames, string target, string[] targetAttributeNames)
		{
			Name = name;
			SourceAttributeNames = sourceAttributeNames;
			Target = target;
			TargetAttributeNames = targetAttributeNames;
		}

		public string Name { get; set; }
		public string[] SourceAttributeNames { get; set; }
		public string Target { get; set; }
		public string[] TargetAttributeNames { get; set; }
	}
}
