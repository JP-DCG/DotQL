using Ancestry.QueryProcessor.Plan;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace Ancestry.QueryProcessor.Compile
{
	public class Compiler
	{
		public Action<JObject, CancellationToken> CreateExecutable(ScriptPlan plan)
		{
			throw new NotImplementedException();
		}
	}
}

