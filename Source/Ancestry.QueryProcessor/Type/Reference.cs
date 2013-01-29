using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class Reference<T>
	{
		public Name ModuleName { get; set; }
		public Name VarName { get; set; }
		public Parse.Expression Condition { get; set; }
	}
}
