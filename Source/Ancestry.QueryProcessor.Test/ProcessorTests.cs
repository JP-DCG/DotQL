using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ProcessorTests
	{
		[TestMethod]
		public void Execute()
		{
			var processor = new Processor();
			
			processor.Execute("//Do nothing");
		}

		[TestMethod]
		public void Evaluate()
		{
			var processor = new Processor();

			var result = processor.Evaluate(@"//Nothing but a comment");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void EvaluateWithArguments()
		{
			var processor = new Processor();

			var result = processor.Evaluate("var x := 5 return x", new Dictionary<string, object> { { "x", 10} });
			Assert.IsTrue(result is int);
			Assert.AreEqual(10, result);
		}
	}
}
