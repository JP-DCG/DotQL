using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Ancestry.QueryProcessor.Type.Module(typeof(Ancestry.QueryProcessor.Runtime.SystemModule), new[] { "System" })]

namespace Ancestry.QueryProcessor.Runtime
{
	public class SystemModule
	{
		public static readonly Boolean Boolean;
		public static readonly Int32 Integer;
		public static readonly Int64 Long;
		public static readonly Char Char;
		public static readonly String String;
		// TODO: public static readonly Date Date;
		// TODO: public static readonly Time Time;
		public static readonly DateTime DateTime;
		public static readonly Name Name;
		public static readonly Double Double;
		public static readonly Guid GUID;
		public static readonly TimeSpan TimeSpan;
		public static readonly Version Version;

		public Storage.IRepository<ISet<ModuleTuple>> Modules;

		public Storage.IRepository<ISet<UsingTuple>> DefaultUsings;

		public static IList<T> ToList<T>(ISet<T> setValue)
		{
			// TODO: ensure that the items coming from the set are ordered so that this is deterministic
			return new ListEx<T>(setValue);
		}

		public static ISet<T> ToSet<T>(IList<T> listValue)
		{
			return new Set<T>(listValue);
		}

		public static DateTime AddMonth(DateTime start, int months)
		{
			return start.AddMonths(months);
		}
	}
}
