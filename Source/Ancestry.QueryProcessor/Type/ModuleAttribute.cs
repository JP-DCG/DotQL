using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class ModuleAttribute : Attribute
	{
		public System.Type ModuleClass { get; set; }
		public Name Name { get; set; }

		public ModuleAttribute(System.Type moduleClass, string[] nameComponents)
		{
			ModuleClass = moduleClass;
			Name = new Name { Components = nameComponents };
		}
	}
}
