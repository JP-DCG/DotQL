using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ModuleAttribute : Attribute
	{
		public QualifiedID Name { get; set; }

		public ModuleAttribute(string[] nameComponents)
		{
			Name = new QualifiedID { Components = nameComponents };
		}
	}
}
