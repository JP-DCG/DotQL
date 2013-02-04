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
		public void TupleSelection()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { x:2 y:'hello' z:2.0 }");
			Assert.IsTrue(result is JObject);
			Assert.AreEqual("2", ((JObject)result)["x"].ToString());
			Assert.AreEqual("hello", ((JObject)result)["y"].ToString());
			Assert.AreEqual(2.0, ((JValue)((JObject)result)["z"]).Value);
		}

		[TestMethod]
		public void TupleAutoNameSelection()
		{
			var processor = new Processor();
			var result = processor.Evaluate("let i := 5 return { :i }");
			Assert.IsTrue(result is JObject);
			Assert.AreEqual("5", ((JObject)result)["i"].ToString());
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

			result = processor.Evaluate("return '1955/3/25'dt->System\\AddMonth(2)");
			Assert.AreEqual(DateTime.Parse("1955/5/25"), ((JValue)result).Value);
		}

		[TestMethod]
		public void Let()
		{
			var processor = new Processor();
			var result = processor.Evaluate("let x := 5 let y := 10 return x + y");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual(15, ((JValue)result).Value);
		}

		[TestMethod]
		public void For()
		{
			var processor = new Processor();
			var result = processor.Evaluate("for i in { 2 3 4 5 6 7 } return i + 10");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(6, ((JArray)result).Count);
			Assert.AreEqual("12", ((JArray)result)[0].ToString());
			Assert.AreEqual("17", ((JArray)result)[5].ToString());
		}

		[TestMethod]
		public void Where()
		{
			var processor = new Processor();
			var result = processor.Evaluate("for i in { 2 3 4 5 6 7 } where i % 2 = 0 return i");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(3, ((JArray)result).Count);
			Assert.AreEqual("2", ((JArray)result)[0].ToString());
			Assert.AreEqual("6", ((JArray)result)[2].ToString());
		}

		[TestMethod]
		public void PassingArguments()
		{
			var processor = new Processor();

			var result = processor.Evaluate("var x := 5 return x", new JObject(new JProperty("x", 10)));
			Assert.IsTrue(result is JValue);
			Assert.AreEqual(10, ((JValue)result).Value);
		}

		[TestMethod]
		public void SelectingModules()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return System\\Modules");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(1, ((JArray)result).Count);
		}

		[TestMethod]
		public void PathRestrictionOfSet()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { 2 3 4 5 6 }?(value >= 4)");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(3, ((JArray)result).Count);
			Assert.AreEqual("4", ((JArray)result)[0].ToString());
			Assert.AreEqual("6", ((JArray)result)[2].ToString());
		}

		[TestMethod]
		public void PathRestrictionOfScalar()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return 5?(value >= 4)");
			Assert.IsTrue(result is JValue);
		}

		[TestMethod]
		public void RestrictOnIndex()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { 10 20 30 40 }?(index >= 2)");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(2, ((JArray)result).Count);
			Assert.AreEqual("30", ((JArray)result)[0].ToString());
			Assert.AreEqual("40", ((JArray)result)[1].ToString());
		}

		[TestMethod]
		public void TupleDereference()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { x: 2 }.(x * 2)");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("4", ((JValue)result).ToString());
		}

		[TestMethod]
		public void SetDereference()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { 2 3 4 }.(value * 5)");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(3, ((JArray)result).Count);
			Assert.AreEqual("10", ((JArray)result)[0].ToString());
			Assert.AreEqual("20", ((JArray)result)[2].ToString());
		}

		[TestMethod]
		public void SetAndTupleDereference()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { { x:2 y:'blah' } { x:3 y:'blah2' } { x:4 y:'blah3' } }.(value.x * 5)");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(3, ((JArray)result).Count);
			Assert.AreEqual("10", ((JArray)result)[0].ToString());
			Assert.AreEqual("20", ((JArray)result)[2].ToString());
		}

		[TestMethod]
		public void DereferenceOnIndex()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { 10 20 30 40 }.(value + index)");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(4, ((JArray)result).Count);
			Assert.AreEqual("10", ((JArray)result)[0].ToString());
			Assert.AreEqual("43", ((JArray)result)[3].ToString());
		}

		[TestMethod]
		public void DereferenceProducingTuple()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { 10 20 30 40 }.{ v:value i:index }");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(4, ((JArray)result).Count);
			Assert.AreEqual("10", ((JObject)((JArray)result)[0])["v"].ToString());
			Assert.AreEqual("0", ((JObject)((JArray)result)[0])["i"].ToString());
			Assert.AreEqual("40", ((JObject)((JArray)result)[3])["v"].ToString());
			Assert.AreEqual("3", ((JObject)((JArray)result)[3])["i"].ToString());
		}
	}
}
