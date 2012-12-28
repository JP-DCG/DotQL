using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ancestry.QueryProcessor.Type
{
	public abstract class TupleMember
	{
	}

	public class AttributeMember : TupleMember
	{
		public TypeDeclaration Type { get; set; }
		
		public override string ToString()
		{
			return Type.ToString();
		}
	}

	public class ReferenceMember : TupleMember
	{
		public string TargetName { get; set; }
		public string[] SourceAttributeNames { get; set; }
		public string[] TargetAttributeNames { get; set; }

		public override string ToString()
		{
			return "ref (" + String.Join(" ", SourceAttributeNames) + ") to " 
				+ TargetName + "(" + String.Join(" ", TargetAttributeNames) + ")";
		}
	}
}
