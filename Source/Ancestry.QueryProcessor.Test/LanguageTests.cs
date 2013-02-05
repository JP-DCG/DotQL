using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class LanguageTests
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
			Assert.AreEqual((5 - 10 * 3), result);

			result = processor.Evaluate("return 5 * 3 + 1");
			Assert.AreEqual((5 * 3 + 1), result);
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
		public void TupleKeyAndComparison()
		{
			var processor = new Processor();
			var result = processor.Evaluate("return { x:2 y:'hello' key{ x } } = { x:2 y:'other' key{ x } }");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("True", ((JValue)result).Value.ToString());
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
		public void SetSelector()
		{
			var processor = new Processor();

			var result = processor.Evaluate("return { 2 3 4 }");
			Assert.IsTrue(result is JArray);
			Assert.IsTrue(((JArray)result).Count == 3);
			Assert.AreEqual(((JValue)((JArray)result)[0]).Value, 2);
			Assert.AreEqual(((JValue)((JArray)result)[1]).Value, 3);
			Assert.AreEqual(((JValue)((JArray)result)[2]).Value, 4);
		}

		[TestMethod]
		public void ListSelector()
		{
			var processor = new Processor();

			var result = processor.Evaluate("return [ 2 3 4 ]");
			Assert.IsTrue(result is JArray);
			Assert.IsTrue(((JArray)result).Count == 3);
			Assert.AreEqual(((JValue)((JArray)result)[0]).Value, 2);
			Assert.AreEqual(((JValue)((JArray)result)[1]).Value, 3);
			Assert.AreEqual(((JValue)((JArray)result)[2]).Value, 4);
		}

		[TestMethod]
		public void Calls()
		{
			var processor = new Processor();

			var result = processor.Evaluate("return { 2 3 4 }->ToList()");
			Assert.IsTrue(result is JArray);
			Assert.IsTrue(((JArray)result).Count == 3);
			Assert.AreEqual(((JValue)((JArray)result)[0]).Value, 2);
			Assert.AreEqual(((JValue)((JArray)result)[1]).Value, 3);
			Assert.AreEqual(((JValue)((JArray)result)[2]).Value, 4);

			result = processor.Evaluate("return '1955/3/25'dt->AddMonth(2)");
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
		public void NestedFLWOR()
		{
			var processor = new Processor();
			var result = processor.Evaluate("for i in { 2 4 6 } \r\n return for j in { 0 1 } return i + j");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(3, ((JArray)result).Count);
			Assert.AreEqual("2", ((JArray)((JArray)result)[0])[0].ToString());
			Assert.AreEqual("3", ((JArray)((JArray)result)[0])[1].ToString());
			Assert.AreEqual("6", ((JArray)((JArray)result)[2])[0].ToString());
			Assert.AreEqual("7", ((JArray)((JArray)result)[2])[1].ToString());
		}

		[TestMethod]
		public void NameHiding()
		{
			var processor = new Processor();
			var result = processor.Evaluate("let x := 5 return let x := 10 return x");
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

		[TestMethod]
		public void FunctionSelector()
		{
			var processor = new Processor();
			var result = processor.Evaluate("let add := (x:Integer y:Integer)=>return x + y return 5->add(10)");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("15", result.ToString());
		}

		[TestMethod]
		public void VarAssignment()
		{
			var processor = new Processor();
			var result = processor.Evaluate("var x := 5 set x := 10 return x");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("10", result.ToString());
		}

		[TestMethod]
		public void EmptyModuleDeclaration()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { }");

			var result = processor.Evaluate("return Modules");
			Assert.IsTrue(result is JArray);
			Assert.AreEqual(2, ((JArray)result).Count);
		}

		[TestMethod]
		public void SimpleModuleVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyVar: Integer }");

			var result = processor.Evaluate("using TestModule return MyVar");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("0", result.ToString());
		}

		[TestMethod]
		public void SimpleModuleTypedef()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyTypedef: typedef { x:Integer y:String key{ x } } }");

			var result = processor.Evaluate("using TestModule var v : MyTypedef return v = { x:0 y:\"\" key{ x } }");
			Assert.IsTrue(result is JValue);
			Assert.AreEqual("True", result.ToString());
		}
	}
}
