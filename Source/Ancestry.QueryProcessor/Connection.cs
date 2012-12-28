using Ancestry.QueryProcessor.Compile;
using Ancestry.QueryProcessor.Parse;
using Ancestry.QueryProcessor.Plan;
using Ancestry.QueryProcessor.Type;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public class Connection : IConnection
	{
		private QueryOptions _defaultOptions;

		public Connection(QueryOptions defaultOptions)
		{
			_defaultOptions = defaultOptions;
		}

		public void Execute(string text, JObject args = null, QueryOptions options = null)
		{
			var actualOptions = options ?? _defaultOptions;
			var token = new CancellationTokenSource();
			var task = Task.Run
			(
				(Action)(() =>
				{
					// Parse
					var parser = new Parser();
					var script = Parser.ParseFrom(parser.Script, text);

					// Plan
					var planner = new Planner();
					var plan = planner.PlanScript(script, actualOptions);
					
					// Compile
					var compiler = new Compiler();
					var executable = compiler.CreateExecutable(plan);
					
					// Run
					executable.SetArgs(JsonInterop.JsonArgsToNative(args));
					executable.Run(token.Token);
				}),
				token.Token
			);
			try
			{
				var timeout = actualOptions != null ? actualOptions.SLA.MaximumTime : QuerySla.DefaultMaximumTime;
				if (!task.Wait(timeout))
				{
					token.Cancel();
					task.Wait();
				}
			}
			finally
			{
				task.Dispose();
			}
		}


		public JToken Evaluate(string text, JObject args = null, QueryOptions options = null, JObject argTypes = null)
		{
			throw new NotImplementedException("Not implemented.");
		}

		/// <summary> Prepares a "well-known" query. </summary>
		/// <param name="script">DotQL script.</param>
		/// <param name="argTypes"> Argument names and strings with DotQL type names. </param>
		/// <param name="options">Optionally overridden query options. </param>
		/// <returns> Handle to well-known query. </returns>
		public Guid Prepare(string text, JObject argTypes = null, QueryOptions options = null)
		{
			throw new NotImplementedException("Not implemented.");
		}

		public void Unprepare(Guid token)
		{
			throw new NotImplementedException("Not implemented.");
		}

		public void Execute(Guid token, JObject args = null)
		{
			throw new NotImplementedException("Not implemented.");
		}

		public JToken Evaluate(Guid token, JObject args = null)
		{
			throw new NotImplementedException("Not implemented.");
		}
 	}
}
