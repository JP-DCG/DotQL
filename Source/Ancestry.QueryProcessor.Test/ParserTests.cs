using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ancestry.QueryProcessor.Parse;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ParserTests
	{
		[TestMethod]
		public void LiteralSelectTest()
		{
			var parser = new Parser();
//			var statement = parser.ParseStatement("select 1, 2.0f, 3.0d, 0x4, $5, '6'");
//			var emitter = new SQLTextEmitter();
//			var actual = emitter.Emit(statement);
//			Assert.AreEqual
//			(
//				@"select 
//	1, 2, 3.0, 0x4, $5, '6';", 
//				actual
//			);
		}

		[TestMethod]
		public void Using()
		{
			Using @using = new Parser().Using(new Lexer("using Something"));
			Assert.IsNull(@using.Alias.Components);
			Assert.IsNotNull(@using.Target.Components);
			Assert.AreEqual("Something", string.Join("\\", @using.Target.Components));

			@using = new Parser().Using(new Lexer("using SomeAlias = Something"));
			Assert.IsNotNull(@using.Alias.Components);
			Assert.AreEqual("SomeAlias", string.Join("\\", @using.Alias.Components));
			Assert.IsNotNull(@using.Target.Components);
			Assert.AreEqual("Something", string.Join("\\", @using.Target.Components));
		}

		[TestMethod]
		public void Module()
		{
			ModuleDeclaration moduleDeclaration = new Parser().Module(new Lexer("module Something { }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name.Components[0]);
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.IsFalse(moduleDeclaration.Members.Any());

			moduleDeclaration = new Parser().Module(new Lexer("module Something { a: Integer }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name.Components[0]);
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.AreEqual(1, moduleDeclaration.Members.Count);
			Assert.AreEqual("a", moduleDeclaration.Members[0].Name.Components[0]);

			moduleDeclaration = new Parser().Module(new Lexer(@" module Something
            {
                    a: Integer
                    b: const 5
            }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name);
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.AreEqual(2, moduleDeclaration.Members.Count);
			Assert.AreEqual("a", moduleDeclaration.Members[0].Name.Components[0]);
			Assert.AreEqual("b", moduleDeclaration.Members[1].Name.Components[0]);
		}

		[TestMethod]
		public void ModuleMember_TypeMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: typedef Integer"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(TypeMember));
			Assert.AreEqual("a", moduleMember.Name);
			Assert.AreEqual("Integer", ((TypeMember)moduleMember).Type);
		}

		[TestMethod]
		public void ModuleMember_EnumMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: enum { red blue green }"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(EnumMember));
			Assert.AreEqual("a", moduleMember.Name);
			Assert.AreEqual("red blue green", string.Join(" ", ((EnumMember)moduleMember).Values));
		}

		[TestMethod]
		public void ModuleMember_ConstMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: const 5"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(ConstMember));
			Assert.AreEqual("a", moduleMember.Name);
			Assert.AreEqual(5, ((ConstMember)moduleMember).Expression);
		}

		[TestMethod]
		public void ModuleMember_VarMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: Integer"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(VarMember));
			Assert.AreEqual("a", moduleMember.Name);
			Assert.AreEqual("Integer", ((VarMember)moduleMember).Type);
		}
	}
}
