using Ancestry.QueryProcessor.Compile;
using Ancestry.QueryProcessor.Parse;
using Ancestry.QueryProcessor.Plan;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public class Processor : IProcessor
	{
		public ProcessorSettings Settings { get; set; }

		public Processor(ProcessorSettings settings = null)
		{
			Settings = settings ?? new ProcessorSettings();
		}

		public void Execute(string text, JObject args = null, QueryOptions options = null)
		{
			AdHocCall((ao, ct) => { InternalExecute(text, args, ao, ct); }, options);
		}

		public JToken Evaluate(string text, JObject args = null, QueryOptions options = null)
		{
			JToken result = null;
			AdHocCall
			(
				(ao, ct) => 
				{ 
					result = JsonInterop.NativeToJson(InternalExecute(text, args, ao, ct)); 
				}, 
				options
			);
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
		private void AdHocCall(Action<QueryOptions, CancellationToken> makeCall, QueryOptions options)
		{
			var actualOptions = options ?? Settings.DefaultOptions;
			EnforceLimits(actualOptions);
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
				var timeout = actualOptions.QueryLimits.MaximumTime;
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

		/// <summary> Adjusts the limits according to policy. </summary>
		private void EnforceLimits(QueryOptions actualOptions)
		{
			var limits = actualOptions.QueryLimits ?? new QueryLimits();
			limits.MaximumTime = Math.Min(limits.MaximumTime, Settings.AdHocMaximumTime);
			limits.MaximumRows = Math.Min(limits.MaximumRows, Settings.AdHocMaximumRows);
			actualOptions.QueryLimits = limits;
		}

		private object InternalExecute(string text, JObject args, QueryOptions actualOptions, CancellationToken cancelToken)
		{
			// Convert arguments
			var convertedArgs = JsonInterop.JsonArgsToNative(args);

			// Create assembly and source file names
			var name = "DotQL" + DateTime.Now.ToString("yyyyMMddhhmmssss");
			var sourceFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.ChangeExtension(name, "dql"));

			// Save the file if we're debugging
			if (Settings.DebugOn)
				System.IO.File.WriteAllText(sourceFileName, text);

			// Parse
			var parser = new Parser();
			var script = Parser.ParseFrom(parser.Script, text);

			// Plan
			var planner = new Planner(actualOptions, Settings.RepositoryFactory);
			var plan = planner.PlanScript(script);

			// Compile
			var compiler = new Compiler(actualOptions, Settings.DebugOn, name, sourceFileName);
			var executable = compiler.CreateExecutable(plan);

			// Run
			return executable(convertedArgs, Settings.RepositoryFactory, cancelToken);
		}
	}
}
