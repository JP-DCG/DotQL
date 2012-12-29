using Ancestry.QueryProcessor.Compile;
using Ancestry.QueryProcessor.Parse;
using Ancestry.QueryProcessor.Plan;
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

		public Connection(QueryOptions defaultOptions = null)
		{
			_defaultOptions = defaultOptions ?? new QueryOptions();
		}

		public void Execute(string text, JObject args = null, QueryOptions options = null)
		{
			InternalCall((ao, ct) => { InternalExecute(text, args, ao, ct); }, options);
		}

		public JToken Evaluate(string text, JObject args = null, QueryOptions options = null)
		{
			JToken result = null;
			InternalCall((ao, ct) => { result = InternalExecute(text, args, ao, ct); }, options);
			return result;
		}

		public Guid Prepare(string text, QueryOptions options = null)
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

		/// <summary> All calls pass through this method. </summary>
		/// <remarks> This handles enforcement of SLA and other cross-cutting concerns. </remarks>
		/// <param name="options"> Optional option overrides. </param>
		private void InternalCall(Action<QueryOptions, CancellationToken> makeCall, QueryOptions options)
		{
			var actualOptions = options ?? _defaultOptions;
			var token = new CancellationTokenSource();
			var task = Task.Run
			(
				(Action)(() =>
				{
					makeCall(actualOptions, token.Token);
				}),
				token.Token
			);
			try
			{
				var timeout = 
					actualOptions != null && actualOptions.Sla != null 
						? actualOptions.Sla.MaximumTime 
						: QuerySla.DefaultMaximumTime;

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

		private static JToken InternalExecute(string text, JObject args, QueryOptions actualOptions, CancellationToken cancelToken)
		{
			// Convert arguments
			var convertedArgs = JsonInterop.JsonArgsToNative(args);

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
			var result = executable(convertedArgs, cancelToken);

			// Convert results
			return JsonInterop.NativeToJson(result);
		}
	}
}
