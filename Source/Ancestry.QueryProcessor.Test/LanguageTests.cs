using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;

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
		public void ReturnLiterals()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return 5");
			Assert.AreEqual(result, 5);
			result = processor.Evaluate("return 'Test String'");
			Assert.AreEqual(result, "Test String");
			result = processor.Evaluate("return '2/3/2013'dt");
			Assert.AreEqual(result, DateTime.Parse("2/3/2013"));
			result = processor.Evaluate("return '01:02:00'ts");
			Assert.AreEqual(result, TimeSpan.Parse("01:02:00"));
			result = processor.Evaluate("return 23.45");
			Assert.AreEqual(result, 23.45);
			result = processor.Evaluate("return true");
			Assert.AreEqual(result, true);
			// TODO: remaining literal types
			//result = processor.Evaluate("return '59A94476-175E-4A83-875B-BD23F71ABDC1'g");
			//Assert.AreEqual(result, Guid.Parse("59A94476-175E-4A83-875B-BD23F71ABDC1"));
		}

		[TestMethod]
		public void IntOperators()
		{
			var processor = new Processor();

			// Test basic precedence
			dynamic result = processor.Evaluate("return 5 - 10 * 3");
			Assert.AreEqual((5 - 10 * 3), result);

			result = processor.Evaluate("return 5 * 3 + 1");
			Assert.AreEqual((5 * 3 + 1), result);

			// Test remaining operators
			result = processor.Evaluate("return ~(5**3 / 1 ^ 23 % 2)");
			Assert.AreEqual((~(Runtime.Runtime.IntPower(5, 3) / 1 ^ 23 % 2)), result);
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

			result = processor.Evaluate("return { x:2 y:'hello' key{ y } } = { x:2 y:'other' key{ y } }");
			Assert.IsFalse(result);

			result = processor.Evaluate("return { x:2 y:'hello' } = { x:2 y:'other' }");
			Assert.IsFalse(result);

			result = processor.Evaluate("return { x:2 y:'hello' } = { x:2 y:'hello' }");
			Assert.IsTrue(result);

			result = processor.Evaluate("return { : } = { : }");
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

			result = processor.Evaluate("return { }");
			Assert.IsTrue(result.Count == 0);

			result = processor.Evaluate("return { 'a' 'b' 'c' }");
			Assert.IsTrue(result.Count == 3);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0], "a");
			Assert.AreEqual(result[1], "b");
			Assert.AreEqual(result[2], "c");

			result = processor.Evaluate("return { { x:1 } { x:2 } { x:3 } }");
			Assert.IsTrue(result.Count == 3);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0].x, 1);
			Assert.AreEqual(result[1].x, 2);
			Assert.AreEqual(result[2].x, 3);

			result = processor.Evaluate("return { { x:1 y:'a' key{ x } } { x:1 y:'b' key{ x } } }");
			Assert.IsTrue(result.Count == 1);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0].x, 1);
		}

		[TestMethod]
		public void SetOperations()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { 2 3 } = { 3 2 }");
			Assert.IsTrue(result);

			result = processor.Evaluate("return { } = { }");
			Assert.IsTrue(result);

			result = processor.Evaluate("return { 2 3 } | { 3 4 }");
			Assert.AreEqual(3, result.Count);
			Assert.IsTrue(result.Contains(2));
			Assert.IsTrue(result.Contains(3));
			Assert.IsTrue(result.Contains(4));

			// TODO: remaining set operations
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

			result = processor.Evaluate("return [ 'blah' 'boo' ]");
			Assert.IsTrue(result.Length == 2);
			Assert.AreEqual(result[0], "blah");
			Assert.AreEqual(result[1], "boo");

			result = processor.Evaluate("return [ ]");
			Assert.IsTrue(result.Length == 0);
		}

		[TestMethod]
		public void ListOperations()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return [ 2 3 ] = [ 2 3 ]");
			Assert.IsTrue(result);

			result = processor.Evaluate("return [ 2 3 ] | [ 2 3 ]");
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(3, result[1]);
			Assert.AreEqual(2, result[2]);
			Assert.AreEqual(3, result[3]);

			// TODO: Remaining list operations
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
			Assert.IsTrue(result is ISet<int>);
			result = Enumerable.ToList(result);
			Assert.AreEqual(6, result.Count);
			Assert.AreEqual(12, result[0]);
			Assert.AreEqual(17, result[5]);

			result = processor.Evaluate("for i in [ 2 3 4 ] return -i");
			Assert.IsTrue(!(result is ISet<int>));
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(-2, result[0]);
			Assert.AreEqual(-4, result[2]);

			result = processor.Evaluate("for i in { 2 4 } for j in [ 0 1 ] return i + j");
			Assert.IsTrue(!(result is ISet<int>));
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(3, result[1]);
			Assert.AreEqual(4, result[2]);
			Assert.AreEqual(5, result[3]);
		}

		[TestMethod]
		public void Where()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in [ 2 3 4 5 6 7 ] where i % 2 = 0 return i");
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(6, result[2]);
		}

		[TestMethod]
		public void NestedFLWOR()
		{
			// Note: the for clauses in this test are not part of the same claused expression, one is nested in the return expression
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

			// TODO: change this to (x < 4) when tuple attributes are available in restriction
			result = processor.Evaluate("return { { x:2 } { x:3 } { x:4 } }?(value.x < 4)");
			result = Enumerable.ToList(result);
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(3, result[1]);
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

			result = processor.Evaluate("return { x: 2 }.{ :x y:x * 2)");
			Assert.AreEqual(2, result.x);
			Assert.AreEqual(4, result.y);
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

			result = processor.Evaluate("return { 2 3 4 }.{ x:value * 5 }");
			result = Enumerable.ToList(result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0].x);
			Assert.AreEqual(20, result[2].x);

			// TODO: change this to (x * 5) once tuple attributes are made available
			result = processor.Evaluate("return { { x:2 } { x:3 } { x:4 } }.(value.x * 5)");
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
			processor.Execute("module TestModule 1.0.0 { MyTup: { x:Integer }  MyInt: Integer  MySet: { Integer } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyTup.x");
			Assert.AreEqual(0, result);

			result = processor.Evaluate("using TestModule 1.0.0 return MyInt + 5");
			Assert.AreEqual(5, result);

			result = processor.Evaluate("using TestModule 1.0.0 set MySet := { -5 5 } return MySet?(value > 0)");
			Assert.AreEqual(1, Enumerable.Count(result));

			result = processor.Evaluate
			(
				@"using TestModule 1.0.0 
				let f := (x: Integer y: Integer) => return x + y 
				return { a:MyInt->f(5) b:5->f(MyInt) }"
			);
			Assert.AreEqual(5, result.a);
			Assert.AreEqual(5, result.b);
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

		[TestMethod]
		public void SimpleModuleFunctionVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyFunc: (x: Integer) => Integer }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 set MyFunc := (x: Integer) => return x + 1 return 5->MyFunc()");
			Assert.AreEqual(6, result);
		}
	}
}
