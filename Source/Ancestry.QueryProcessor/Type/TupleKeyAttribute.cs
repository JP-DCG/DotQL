using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
	public class TupleKeyAttribute : Attribute
	{
		public TupleKeyAttribute(string[] attributeNames)
		{
			AttributeNames = attributeNames;
		}

		public string[] AttributeNames { get; set; }
	}
}
