using Newtonsoft.Json.Linq;
using System;

namespace Ancestry.QueryProcessor
{
	public interface IConnection
	{
		JToken Evaluate(Guid token, JObject args = null);
		JToken Evaluate(string script, JObject args = null, QueryOptions options = null, JObject argTypes = null);
		void Execute(Guid token, JObject args = null);
		void Execute(string script, JObject args = null, QueryOptions options = null, JObject argTypes = null);
		Guid Prepare(string script, JObject argTypes = null, QueryOptions options = null);
		void Unprepare(Guid token);
	}
}
