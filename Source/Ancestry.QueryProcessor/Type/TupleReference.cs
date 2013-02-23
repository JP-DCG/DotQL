using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TupleReference
	{
		// Note: these arrays are assumed immutable
		public Name[] SourceAttributeNames { get; set; }
		public Name Target { get; set; }
		public Name[] TargetAttributeNames { get; set; }

		public override int GetHashCode()
		{
			var running = 83;
			foreach (var san in SourceAttributeNames)
				running = running * 83 + san.GetHashCode();
			running = running * 83 + Target.GetHashCode();
			foreach (var tan in TargetAttributeNames)
				running = running * 83 + tan.GetHashCode();
			return running;
		}

		public override bool Equals(object obj)
		{
			if (obj is TupleReference)
				return (TupleReference)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(TupleReference left, TupleReference right)
		{
			return Object.ReferenceEquals(left, right)
				|| 
				(
					left.SourceAttributeNames.SequenceEqual(right.SourceAttributeNames)
						&& left.Target == right.Target
						&& left.TargetAttributeNames.SequenceEqual(right.TargetAttributeNames)
				);
		}

		public static bool operator !=(TupleReference left, TupleReference right)
		{
			return !(left == right);
		}

		public static TupleReference FromParseReference(Parse.TupleReference reference)
		{
			return 
				new TupleReference 
				{ 
					SourceAttributeNames = (from san in reference.SourceAttributeNames select Name.FromQualifiedIdentifier(san)).ToArray(),
					Target = Name.FromQualifiedIdentifier(reference.Target),
					TargetAttributeNames = (from tan in reference.TargetAttributeNames select Name.FromQualifiedIdentifier(tan)).ToArray()
				};
		}
	}
}
