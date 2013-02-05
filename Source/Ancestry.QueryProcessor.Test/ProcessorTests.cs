using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ProcessorTests
	{
		[TestMethod]
		public void Execute()
		{
			var processor = new Processor();
			
			processor.Execute("\\Do nothing");
		}

		[TestMethod]
		public void Evaluate()
		{
			var processor = new Processor();

			var result = processor.Evaluate("\\Nothing but a comment");
			Assert.AreEqual(new Newtonsoft.Json.Linq.JValue((object)null), result);
		}

		[TestMethod]
		public void EvaluateWithArguments()
		{
			var processor = new Processor();

			var result = processor.Evaluate("var x := 5 return x", new JObject(new JProperty("x", 10)));
			Assert.IsTrue(result is JValue);
			Assert.AreEqual(10, ((JValue)result).Value);
		}
	}
}
