using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class Reference<T>
	{
		public QualifiedID ModuleName { get; set; }
		public QualifiedID VarName { get; set; }
		public Parse.Expression Condition { get; set; }
	}
}
