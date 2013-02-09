using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class LanguageTests
	{
		[TestMethod]
		public void EmptyScript()
		{
			var processor = new Processor();

			processor.Execute("//Nothing but a comment");
			var result = processor.Evaluate("//Nothing but a comment");
			Assert.IsNull(result);
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
			dynamic result = processor.Evaluate("return { x:2 y:'hello' z:2.0 }");
			Assert.AreEqual(2, result.x);
			Assert.AreEqual("hello", result.y);
			Assert.AreEqual(2.0, result.z);
		}

		[TestMethod]
		public void TupleKeyAndComparison()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { x:2 y:'hello' key{ x } } = { x:2 y:'other' key{ x } }");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void TupleAutoNameSelection()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let i := 5 return { :i }");
			Assert.AreEqual(5, result.i);
		}

		[TestMethod]
		public void SetSelector()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { 2 3 4 }");
			Assert.IsTrue(result.Count == 3);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0], 2);
			Assert.AreEqual(result[1], 3);
			Assert.AreEqual(result[2], 4);
		}

		[TestMethod]
		public void ListSelector()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return [ 2 3 4 ]");
			Assert.IsTrue(result.Length == 3);
			Assert.AreEqual(result[0], 2);
			Assert.AreEqual(result[1], 3);
			Assert.AreEqual(result[2], 4);
		}

		[TestMethod]
		public void Calls()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { 2 3 4 }->ToList()");
			result = Enumerable.ToList(result);
			Assert.IsTrue(result.Count == 3);
			Assert.AreEqual(result[0], 2);
			Assert.AreEqual(result[1], 3);
			Assert.AreEqual(result[2], 4);

			result = processor.Evaluate("return '1955/3/25'dt->AddMonth(2)");
			Assert.AreEqual(DateTime.Parse("1955/5/25"), result);
		}

		[TestMethod]
		public void Let()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let x := 5 let y := 10 return x + y");
			Assert.AreEqual(15, result);
		}

		[TestMethod]
		public void For()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in { 2 3 4 5 6 7 } return i + 10");
			result = Enumerable.ToList(result);
			Assert.AreEqual(6, result.Count);
			Assert.AreEqual(12, result[0]);
			Assert.AreEqual(17, result[5]);
		}

		[TestMethod]
		public void Where()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in { 2 3 4 5 6 7 } where i % 2 = 0 return i");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(6, result[2]);
		}

		[TestMethod]
		public void NestedFLWOR()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in { 2 4 6 } \r\n return for j in [ 0 1 ] return i + j");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(2, result[0][0]);
			Assert.AreEqual(3, result[0][1]);
			Assert.AreEqual(6, result[2][0]);
			Assert.AreEqual(7, result[2][1]);
		}

		[TestMethod]
		public void NameHiding()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let x := 5 return let x := 10 return x");
			Assert.AreEqual(10, result);
		}

		[TestMethod]
		public void SelectingModules()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return System\\Modules");
			result = Enumerable.ToList(result);
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("System", result[0].Name.ToString());
		}

		[TestMethod]
		public void PathRestrictionOfSet()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 2 3 4 5 6 }?(value >= 4)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(4, result[0]);
			Assert.AreEqual(6, result[2]);
		}

		[TestMethod]
		public void PathRestrictionOfScalar()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return 5?(value >= 4)");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void RestrictOnIndex()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 10 20 30 40 }?(index >= 2)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(30, result[0]);
			Assert.AreEqual(40, result[1]);
		}

		[TestMethod]
		public void TupleDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { x: 2 }.(x * 2)");
			Assert.AreEqual(4, result);
		}

		[TestMethod]
		public void SetDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 2 3 4 }.(value * 5)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(20, result[2]);
		}

		[TestMethod]
		public void SetAndTupleDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { { x:2 y:'blah' } { x:3 y:'blah2' } { x:4 y:'blah3' } }.(value.x * 5)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(20, result[2]);
		}

		[TestMethod]
		public void DereferenceOnIndex()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 10 20 30 40 }.(value + index)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(43, result[3]);
		}

		[TestMethod]
		public void DereferenceProducingTuple()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 10 20 30 40 }.{ v:value i:index }");
			result = Enumerable.ToList(result);
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(10, result[0].v);
			Assert.AreEqual(0, result[0].i);
			Assert.AreEqual(40, result[3].v);
			Assert.AreEqual(3, result[3].i);
		}

		[TestMethod]
		public void FunctionSelector()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let add := (x:Integer y:Integer)=>return x + y return 5->add(10)");
			Assert.AreEqual(15, result);
		}

		[TestMethod]
		public void VarAssignment()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("var x := 5 set x := 10 return x");
			Assert.AreEqual(10, result);
		}

		[TestMethod]
		public void EmptyModuleDeclaration()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { }");

			dynamic result = processor.Evaluate("return Modules");
			Assert.AreEqual(2, result.Count);
		}

		[TestMethod]
		public void ModuleSelfReferencing()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { Forward: Int  Int: typedef Integer Backward: { x: Int } }");
		}

		[TestMethod]
		public void SimpleModuleVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyVar: Integer }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyVar");
			Assert.AreEqual(0, result);
		}

		[TestMethod]
		public void SimpleModuleConst()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyConst: const 5 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyConst");
			Assert.AreEqual(5, result);
		}

		[TestMethod]
		public void SimpleModuleEnum()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyEnum: enum { Red Green } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return Green");
			Assert.AreEqual("Green", result.ToString());
		}

		[TestMethod]
		public void TupleModuleVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyVar: { x:Integer } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyVar");
			Assert.AreEqual(0, result.x);
		}

		[TestMethod]
		public void TupleModuleTypedef()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyTypedef: typedef { x:Integer y:String key{ x } } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 var v : MyTypedef return v = { x:0 y:\"\" key{ x } }");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void SimpleModuleFunctionConst()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyFunc: const (x: Integer) => return x + 1 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return 5->MyFunc()");
			Assert.AreEqual(6, result);
		}
	}
}
