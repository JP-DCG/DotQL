using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ProcessorTests
	{
		[TestMethod]
		public void EmptyScript()
		{
			var processor = new Processor();
			
			processor.Execute("\\Nothing but a comment");
			var result = processor.Evaluate("\\Nothing but a comment");
			Assert.AreEqual(new Newtonsoft.Json.Linq.JValue((object)null), result);
		}
		
		[TestMethod]
		public void BasicEvaluate()
		{
			var processor = new Processor();
			
			var result = processor.Evaluate("return 'Hello world.'");
			Assert.AreEqual("Hello world.", result);

			result = processor.Evaluate("return 5 - 10 * 3");
            Assert.AreEqual( ( 5 - 10 * 3 ), result );

            result = processor.Evaluate( "return 5 * 3 + 1" );
            Assert.AreEqual( ( 5 * 3 + 1 ), result );
		}

		[TestMethod]
		public void LocalSymbolEvaluate()
		{
			var processor = new Processor();
			
			var result = processor.Evaluate("let x := 'Hello world.' return x");
			Assert.AreEqual("Hello world.", result);
		}

		[TestMethod]
		public void SystemModule()
		{
			var processor = new Processor();

			var result = processor.Evaluate("return { 2 3 4 }->System\\ToList()");
			Assert.IsTrue(result is JArray);
			Assert.IsTrue(((JArray)result).Count == 3);
			Assert.AreEqual(((JValue)((JArray)result)[0]).Value, 2);
			Assert.AreEqual(((JValue)((JArray)result)[1]).Value, 3);
			Assert.AreEqual(((JValue)((JArray)result)[2]).Value, 4);
		}
	}
}
