using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using Ancestry.QueryProcessor.Type;

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
			Assert.IsNull(result.Result);
			Assert.AreEqual(result.Type, SystemTypes.Void);
		}

		[TestMethod]
		public void ReturnLiterals()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return 5");
			Assert.AreEqual(result.Result, 5);
			result = processor.Evaluate("return 'Test String'");
			Assert.AreEqual(result.Result, "Test String");
			result = processor.Evaluate("return '2/3/2013'dt");
			Assert.AreEqual(result.Result, DateTime.Parse("2/3/2013"));
			result = processor.Evaluate("return '01:02:00'ts");
			Assert.AreEqual(result.Result, TimeSpan.Parse("01:02:00"));
			result = processor.Evaluate("return 23.45");
			Assert.AreEqual(result.Result, 23.45);
			result = processor.Evaluate("return true");
			Assert.AreEqual(result.Result, true);
			// TODO: remaining literal types
			//result = processor.Evaluate("return '59A94476-175E-4A83-875B-BD23F71ABDC1'g");
			//Assert.AreEqual(result.Result, Guid.Parse("59A94476-175E-4A83-875B-BD23F71ABDC1"));
		}

		[TestMethod]
		public void IntOperators()
		{
			var processor = new Processor();

			// Test basic precedence
			dynamic result = processor.Evaluate("return 5 - 10 * 3");
			Assert.AreEqual((5 - 10 * 3), result.Result);

			result = processor.Evaluate("return 5 * 3 + 1");
			Assert.AreEqual((5 * 3 + 1), result.Result);

			// Test remaining operators
			result = processor.Evaluate("return ~(5**3 / 1 ^ 23 % 2)");
			Assert.AreEqual((~(Runtime.Runtime.IntPower(5, 3) / 1 ^ 23 % 2)), result.Result);
		}

		[TestMethod]
		public void StringOperators()
		{
			var processor = new Processor();

			// Test basic precedence
			dynamic result = processor.Evaluate("return 'hello ' + 'world'");
			Assert.AreEqual("hello world", result.Result);

			result = processor.Evaluate("return 'hello ' <> 'hello' and ('hello' = 'hello') and ('hello' <> 'HELLO')");
			Assert.IsTrue(result.Result);

			result = processor.Evaluate("return ('zebra' > 'apple') and ('bob' < 'fran') and ('Z' <= 'Z') and ('Z' <= 'Zoe') and ('Z' >= 'Z') and ('Z' >= 'Y')");
			Assert.IsTrue(result.Result);
		}

		[TestMethod]
		public void TupleSelection()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { x:2 y:'hello' z:2.0, a:1 + 5, b:0x11 }");
			result = result.Result;
			Assert.AreEqual(2, result.x);
			Assert.AreEqual("hello", result.y);
			Assert.AreEqual(2.0, result.z);
			Assert.AreEqual(6, result.a);
			Assert.AreEqual(0x11, result.b);

			result = processor.Evaluate("return { : }");
			Assert.IsNotNull(result);
		}

		[TestMethod]
		public void TupleKeyAndComparison()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { x:2 y:'hello' key{ x } } = { x:2 y:'other' key{ x } }");
			Assert.IsTrue(result.Result);

			result = processor.Evaluate("return { x:2 y:'hello' key{ y } } = { x:2 y:'other' key{ y } }");
			Assert.IsFalse(result.Result);

			result = processor.Evaluate("return { x:2 y:'hello' } = { x:2 y:'other' }");
			Assert.IsFalse(result.Result);

			result = processor.Evaluate("return { x:2 y:'hello' } = { x:2 y:'hello' }");
			Assert.IsTrue(result.Result);

			result = processor.Evaluate("return { : } = { : }");
			Assert.IsTrue(result.Result);
		}

		[TestMethod]
		public void TupleOperators()
		{
			var processor = new Processor();

			//// TODO: Tuple operators
			//dynamic result = processor.Evaluate("return { x:2 y:'hello' key{ x } } | { z:5 key{ z } }");
			//result = result.Result;
			//Assert.AreEqual(2, result.x);
			//Assert.AreEqual("hello", result.y);
			//Assert.AreEqual(5, result.z);
		}

		[TestMethod]
		public void TupleAutoNameSelection()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let i := 5 return { :i }");
			result = result.Result;
			Assert.AreEqual(5, result.i);
		}

		[TestMethod]
		public void SetSelector()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { -5, 2, 3 4 }");
			result = result.Result;
			Assert.IsTrue(result.Count == 4);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0], -5);
			Assert.AreEqual(result[1], 2);
			Assert.AreEqual(result[2], 3);
			Assert.AreEqual(result[3], 4);

			result = processor.Evaluate("return { }");
			result = result.Result;
			Assert.IsTrue(result.Count == 0);

			result = processor.Evaluate("return { 'a', 'b', 'c' }");
			result = result.Result;
			Assert.IsTrue(result.Count == 3);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0], "a");
			Assert.AreEqual(result[1], "b");
			Assert.AreEqual(result[2], "c");

			result = processor.Evaluate("return { { x:1 }, { x:2 }, { x:3 } }");
			result = result.Result;
			Assert.IsTrue(result.Count == 3);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0].x, 1);
			Assert.AreEqual(result[1].x, 2);
			Assert.AreEqual(result[2].x, 3);

			result = processor.Evaluate("return { { x:1, y:'a', key{ x } }, { x:1, y:'b', key{ x } } }");
			result = result.Result;
			Assert.IsTrue(result.Count == 1);
			result = Enumerable.ToList(result);
			Assert.AreEqual(result[0].x, 1);
		}

		[TestMethod]
		public void SetOperations()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return { 2 3 } = { 3 2 }");
			Assert.IsTrue(result.Result);

			result = processor.Evaluate("return { } = { }");
			Assert.IsTrue(result.Result);

			//// TODO: remaining set operations
			//result = processor.Evaluate("return { 2 3 } | { 3 4 }");
			//result = result.Result;
			//Assert.AreEqual(3, result.Count);
			//Assert.IsTrue(result.Contains(2));
			//Assert.IsTrue(result.Contains(3));
			//Assert.IsTrue(result.Contains(4));

			//result = processor.Evaluate("return { 2 3 } & { 3 4 }");
			//result = result.Result;
			//Assert.AreEqual(1, result.Count);
			//Assert.IsTrue(result.Contains(3));
		}

		[TestMethod]
		public void ListSelector()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return [2 3 4]");
			result = result.Result;
			Assert.IsTrue(result.Count == 3);
			Assert.AreEqual(result[0], 2);
			Assert.AreEqual(result[1], 3);
			Assert.AreEqual(result[2], 4);

			result = processor.Evaluate("return ['blah' 'boo']");
			result = result.Result;
			Assert.IsTrue(result.Count == 2);
			Assert.AreEqual(result[0], "blah");
			Assert.AreEqual(result[1], "boo");

			result = processor.Evaluate("return []");
			result = result.Result;
			Assert.IsTrue(result.Count == 0);
			Assert.IsTrue(result.GetType().GenericTypeArguments[0] == typeof(Runtime.Void));
		}

		[TestMethod]
		public void ListOperations()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return [2 3] = [2 3]");
			Assert.IsTrue(result.Result);

			result = processor.Evaluate("return [] = []");
			Assert.IsTrue(result.Result);

			// TODO: Remaining list operations

			//result = processor.Evaluate("return [2 3] | [2 3]");
			//result = result.Result;
			//Assert.AreEqual(4, result.Count);
			//Assert.AreEqual(2, result[0]);
			//Assert.AreEqual(3, result[1]);
			//Assert.AreEqual(2, result[2]);
			//Assert.AreEqual(3, result[3]);
		}

		[TestMethod]
		public void StaticCalls()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate("return ToList({ 2 3 4 })");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);
			Assert.IsTrue(result.Count == 3);
			Assert.AreEqual(result[0], 2);
			Assert.AreEqual(result[1], 3);
			Assert.AreEqual(result[2], 4);

			result = processor.Evaluate("return AddMonth('1955/3/25'dt, 2)");
			Assert.AreEqual(DateTime.Parse("1955/5/25"), result.Result);
		}

		[TestMethod]
		public void InstanceCalls()
		{
			var processor = new Processor();

			dynamic result = processor.Evaluate(@"module Test 1.0.0 { X: Int32, AddX: (y : Int32) return X + y }");
			result = processor.Evaluate
			(
				@"
					set X := 5
					return AddX(2)
				"
			);
			Assert.AreEqual(result.Result, 7);
		}

		[TestMethod]
		public void Let()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("let x := 5 let y := 10 return x + y");
			Assert.AreEqual(15, result.Result);
		}

		[TestMethod]
		public void For()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in { 2 3 4 5 6 7 } return i + 10");
			Assert.IsTrue(result.Type is SetType);
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(6, result.Count);
			Assert.AreEqual(12, result[0]);
			Assert.AreEqual(17, result[5]);

			result = processor.Evaluate("for i in [ 2 3 4 ] return -i");
			Assert.IsTrue(result.Type is ListType);
			Assert.AreEqual(3, result.Result.Count);
			Assert.AreEqual(-2, result.Result[0]);
			Assert.AreEqual(-4, result.Result[2]);

			result = processor.Evaluate("for i in { 2 4 } for j in [ 0 1 ] return i + j");
			Assert.IsTrue(result.Type is ListType);
			Assert.AreEqual(4, result.Result.Count);
			Assert.AreEqual(2, result.Result[0]);
			Assert.AreEqual(3, result.Result[1]);
			Assert.AreEqual(4, result.Result[2]);
			Assert.AreEqual(5, result.Result[3]);
		}

		[TestMethod]
		public void Where()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("for i in [ 2 3 4 5 6 7 ] where i % 2 = 0 return i");
			Assert.AreEqual(3, result.Result.Count);
			Assert.AreEqual(2, result.Result[0]);
			Assert.AreEqual(6, result.Result[2]);
		}

		[TestMethod]
		public void NestedFLWOR()
		{
			// Note: the for clauses in this test are not part of the same claused expression, one is nested in the return expression
			var processor = new Processor();
			dynamic result = 
				processor.Evaluate
				(
					@"
						for i in { 2 4 6 } 
						return 
							for j in [ 0 1 ] return i + j
					"
				);
			result = Enumerable.ToList(result.Result);
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
			Assert.AreEqual(10, result.Result);

			result = processor.Evaluate("let x := 5 let x := 10 return x");
			Assert.AreEqual(10, result.Result);
		}

		[TestMethod]
		public void SelectingModules()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return System\\Modules");
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("System", result[0].Name.ToString());
		}

		[TestMethod]
		public void PathRestrictionOfSet()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 2 3 4 5 6 }(value >= 4)");
			Assert.IsTrue(result.Type is SetType);
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(4, result[0]);
			Assert.AreEqual(6, result[2]);

			result = processor.Evaluate("return { { x:2 } { x:3 } { x:4 } }(x < 3)");
			Assert.IsTrue(result.Type is SetType);
			Assert.AreEqual(1, Enumerable.Count(result.Result));
			Assert.AreEqual(2, Enumerable.First(result.Result).x);
		}

		[TestMethod]
		public void PathRestrictionOfList()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return [2 3 4 5 6](value >= 4)");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);	// IEnumerable, must still convert to list
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(4, result[0]);
			Assert.AreEqual(6, result[2]);

			result = processor.Evaluate("return [{ x:2 } { x:3 } { x:4 }](x < 3)");
			Assert.AreEqual(1, Enumerable.Count(result.Result));
			Assert.AreEqual(2, Enumerable.First(result.Result).x);
		}

		[TestMethod]
		public void PathRestrictionOfScalar()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return 5(value >= 4)");
			Assert.AreEqual(5, result.Result);

			result = processor.Evaluate("return 5(value < 4)");
			Assert.IsNull(result.Result);
		}

		[TestMethod]
		public void PathRestrictOnIndex()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return [10 20 30 40](index >= 2)");
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(30, result[0]);
			Assert.AreEqual(40, result[1]);

			//// TODO: enable when set sorted
			//result = processor.Evaluate("return { 0 20 10 }(index > 1)");
			//result = Enumerable.ToList(result.Result);
			//Assert.AreEqual(1, result.Count);
			//Assert.AreEqual(20, result[0]);
		}

		[TestMethod]
		public void TupleDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { x: 2 }.(x * 2)");
			Assert.AreEqual(4, result.Result);

			result = processor.Evaluate("return { x: 2 }.{ :x y:x * 2 }");
			Assert.AreEqual(2, result.Result.x);
			Assert.AreEqual(4, result.Result.y);
		}

		[TestMethod]
		public void NaryDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 2 3 4 }.(value * 5)");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(20, result[2]);

			result = processor.Evaluate("return { 2 3 4 }.{ x:value * 5 }");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0].x);
			Assert.AreEqual(20, result[2].x);

			result = processor.Evaluate("return { { x:2 } { x:3 } { x:4 } }.(x * 5)");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(20, result[2]);

			result = processor.Evaluate("return [2 2 3].{ x:value * 5 }");
			Assert.IsTrue(result.Type is ListType);
			result = Enumerable.ToList(result.Result);	// convert from enumerable
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0].x);
			Assert.AreEqual(10, result[1].x);
			Assert.AreEqual(15, result[2].x);
		}

		[TestMethod]
		public void SetAndTupleDereference()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { { x:2 y:'blah' } { x:3 y:'blah2' } { x:4 y:'blah3' } }.(value.x * 5)");
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(20, result[2]);
		}

		[TestMethod]
		public void DereferenceOnIndex()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 10 20 30 40 }.(value + index)");
			result = Enumerable.ToList(result.Result);
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(10, result[0]);
			Assert.AreEqual(43, result[3]);
		}

		[TestMethod]
		public void DereferenceProducingTuple()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("return { 10 20 30 40 }.{ v:value i:index }");
			result = Enumerable.ToList(result.Result);
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
			dynamic result = processor.Evaluate
			(
				@"let add := (x:Int32 y:Int32) return x + y	
				return add(5, 10)"
			);
			Assert.AreEqual(15, result.Result);
		}

		[TestMethod]
		public void Variables()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("var x := 5 return x");
			Assert.AreEqual(5, result.Result);

			result = processor.Evaluate("var x : Int32 := 5 return x");
			Assert.AreEqual(5, result.Result);

			result = processor.Evaluate("var x : Int32 return x");
			Assert.AreEqual(0, result.Result);

			// TODO: conversions
			//result = processor.Evaluate("var x : Int32 := 5.0 return x");
			//Assert.IsTrue(result.Type is BaseIntegerType);
			//Assert.AreEqual(5, result.Result);
		}

		[TestMethod]
		public void VarAssignment()
		{
			var processor = new Processor();
			dynamic result = processor.Evaluate("var x := 5 set x := 10 return x");
			Assert.AreEqual(10, result.Result);

			result = processor.Evaluate("var x : Int32 set x := 10 return x");
			Assert.AreEqual(10, result.Result);

			// TODO: Conversions
			//result = processor.Evaluate("var x : Int32 set x := 10.0 return x");
			//Assert.AreEqual(10, result.Result);
		}

		[TestMethod]
		public void EmptyModuleDeclaration()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { }");

			dynamic result = processor.Evaluate("return Modules");
			Assert.AreEqual(2, result.Result.Count);
		}

		[TestMethod]
		public void ModuleSelfReferencing()
		{
			var processor = new Processor();
			processor.Execute
			(
				@"
					module TestModule 1.0.0 
					{ 
						Forward: Int,  
						Int: typedef Int32, 
						Backward: { x: Int } 
					}
				"
			);
		}

		[TestMethod]
		public void SimpleModuleVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyVar: Int32 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyVar");
			Assert.AreEqual(0, result.Result);
		}

		[TestMethod]
		public void SimpleModuleConst()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyConst: const 5 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyConst");
			Assert.AreEqual(5, result.Result);
		}

		[TestMethod]
		public void SimpleModuleEnum()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyEnum: enum { Red, Green } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return Green");
			Assert.AreEqual("Green", result.Result.ToString());
		}

		[TestMethod]
		public void TupleModuleVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyTup: { x:Int32 },  MyInt: Int32,  MySet: { Int32 } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyTup.x");
			Assert.AreEqual(0, result.Result);

			result = processor.Evaluate("using TestModule 1.0.0 return MyInt + 5");
			Assert.AreEqual(5, result.Result);

			result = processor.Evaluate("using TestModule 1.0.0 set MySet := { -5, 5 } return MySet(value > 0)");
			Assert.AreEqual(1, Enumerable.Count(result.Result));

			result = processor.Evaluate
			(
				@"using TestModule 1.0.0 
				let f := (x: Int32, y: Int32) return x + y 
				return { a: f(MyInt, 5), b: f(5, MyInt) }"
			);
			Assert.AreEqual(5, result.Result.a);
			Assert.AreEqual(5, result.Result.b);
		}

		[TestMethod]
		public void TupleModuleTypedef()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyTypedef: typedef { x:Int32, y:String, key{ x } } }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 var v : MyTypedef return v = { x:0, y:\"\", key{ x } }");
			Assert.IsTrue(result.Result);
		}

		[TestMethod]
		public void SimpleModuleFunctionConst()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyFunc: const (x: Int32) return x + 1 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 return MyFunc(5)");
			Assert.AreEqual(6, result.Result);
		}

		[TestMethod]
		public void SimpleModuleFunctionVar()
		{
			var processor = new Processor();
			processor.Execute("module TestModule 1.0.0 { MyFunc: (x: Int32) return Int32 }");

			dynamic result = processor.Evaluate("using TestModule 1.0.0 set MyFunc := (x: Int32) return return x + 1 return MyFunc(5)");
			Assert.AreEqual(6, result.Result);
		}

        [TestMethod]
        public void IfExpression()
        {
            var processor = new Processor();
            dynamic result = processor.Evaluate("return if true then 1 else 0");
            Assert.AreEqual(1, result.Result);

            result = processor.Evaluate("return if false then 1 else 0");
            Assert.AreEqual(0, result.Result);

            result = processor.Evaluate("return if 5 > 3 then 1 else 0");
            Assert.AreEqual(1, result.Result);

            result = processor.Evaluate("return if 5 < 3 then 1 else 5 + 6");
            Assert.AreEqual(11, result.Result);


            //Test datatype conversions
            //result = processor.Evaluate("return if true then 1.1 else 0");
            //Assert.AreEqual(1, result.Result);
        }

        [TestMethod]
        public void CaseExpression()
        {

            //"Switch" case
            var processor = new Processor();
            dynamic result = processor.Evaluate
            (
                @"return
                    case 0
                        when 0 then 'zero'
			            when 1 then 'one'
			            else 'two'
		            end"
            );
            Assert.AreEqual("zero", result.Result);

            result = processor.Evaluate
            (
                @"return
                    case 1
                        when 0 then 'zero'
			            when 1 then 'one'
			            else 'two'
		            end"
            );
            Assert.AreEqual("one", result.Result);

            result = processor.Evaluate
           (
              @"return
                    case 2
                        when 0 then 'zero'
			            when 1 then 'one'
			            else 'two'
		            end"
           );
            Assert.AreEqual("two", result.Result);


            //Stacked if case
            result = processor.Evaluate
            (
                @"return
                    case 
                        when 1 > 0 then 'first'
			            when 1 < 0  then 'second'
			            else 'neither'
		            end"
            );
            Assert.AreEqual("first", result.Result);

            result = processor.Evaluate
           (
               @"return
                    case 
                        when 1 < 0 then 'first'
			            when 1 > 0  then 'second'
			            else 'neither'
		            end"
           );
            Assert.AreEqual("second", result.Result);

            result = processor.Evaluate
            (
            @"return
                    case 
                        when 1 < 0 then 'first'
			            when 1 = 0  then 'second'
			            else 'neither'
		            end"
            );
            Assert.AreEqual("neither", result.Result);
            

            //TODO: Case strict


        }
	}
}
