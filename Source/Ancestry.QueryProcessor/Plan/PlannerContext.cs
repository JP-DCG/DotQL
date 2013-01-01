using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Plan
{
	public class PlannerContext
	{
		public Frame Frame { get; set; }
		public Parse.TypeDeclaration Type { get; set; }
		public Characteristics Characteristics { get; set; }
	}
}
