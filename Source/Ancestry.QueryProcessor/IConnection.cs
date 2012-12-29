using Newtonsoft.Json.Linq;
using System;

namespace Ancestry.QueryProcessor
{
	public interface IConnection
	{
		/// <summary> Evaluates the given script and returns the result encoded in JSON. </summary>
		/// <param name="args"> Any arguments (var overrides) provided by name. </param>
		/// <param name="options"> Optional option overrides. </param>
		JToken Evaluate(string script, JObject args = null, QueryOptions options = null);

		/// <summary> Executes the given script and discards any result. </summary>
		/// <param name="args"> Any arguments (var overrides) provided by name. </param>
		/// <param name="options"> Optional option overrides. </param>
		void Execute(string script, JObject args = null, QueryOptions options = null);

		/// <summary> Prepares the given script and returns a handle for future execution. </summary>
		/// <param name="options"> Optional option overrides. </param>
		Guid Prepare(string script, QueryOptions options = null);

		/// <summary> Executes a previously prepared script by the provided handle and disgards any result. </summary>
		/// <param name="token"> The handle returned by a previous call to Prepare. </param>
		/// <param name="args"> Any arguments (var overrides) provided by name. </param>
		void Execute(Guid token, JObject args = null);

		/// <summary> Evaluates a previously prepared script by the provided handle and returns the result encoded in JSON. </summary>
		/// <param name="token"> The handle returned by a previous call to Prepare. </param>
		/// <param name="args"> Any arguments (var overrides) provided by name. </param>
		JToken Evaluate(Guid token, JObject args = null);

		/// <summary> Unprepares a prepared script using the provided handle. </summary>
		void Unprepare(Guid token);
	}
}
