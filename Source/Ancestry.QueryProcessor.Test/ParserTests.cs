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
		public void Literals()
		{
			var literal = new Parser().LiteralExpression(new Lexer("1234"));
			Assert.AreEqual("1234", literal.ToString());
			// TODO: complete
		}

		[TestMethod]
		public void Using()
		{
			Using @using = new Parser().Using(new Lexer("using Something 1.2.3"));
			Assert.IsNull(@using.Alias);
			Assert.IsNotNull(@using.Target.Components);
			Assert.AreEqual(@using.Version, Version.Parse("1.2.3"));
			Assert.AreEqual("Something", string.Join("\\", @using.Target.Components));

			@using = new Parser().Using(new Lexer("using SomeAlias := Something 1.2.0"));
			Assert.IsNotNull(@using.Alias.Components);
			Assert.AreEqual("SomeAlias", string.Join("\\", @using.Alias.Components));
			Assert.IsNotNull(@using.Target.Components);
			Assert.AreEqual(@using.Version, Version.Parse("1.2.0"));
			Assert.AreEqual("Something", string.Join("\\", @using.Target.Components));
		}

		[TestMethod]
		public void Module()
		{
			ModuleDeclaration moduleDeclaration = new Parser().Module(new Lexer("module Something 1.2.3 { }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name.ToString());
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.IsFalse(moduleDeclaration.Members.Any());
			Assert.AreEqual(moduleDeclaration.Version, Version.Parse("1.2.3"));

			moduleDeclaration = new Parser().Module(new Lexer("module Something 1.2.0 { a: Int32 }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name.ToString());
			Assert.AreEqual(moduleDeclaration.Version, Version.Parse("1.2.0"));
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.AreEqual(1, moduleDeclaration.Members.Count);
			Assert.AreEqual("a", moduleDeclaration.Members[0].Name.Components[0]);

			moduleDeclaration = new Parser().Module(new Lexer(@" module Something 1.0.0
            {
                    a: Int32
                    b: const 5
            }"));
			Assert.IsNotNull(moduleDeclaration);
			Assert.AreEqual("Something", moduleDeclaration.Name.ToString());
			Assert.AreEqual(moduleDeclaration.Version, Version.Parse("1.0.0"));
			Assert.IsNotNull(moduleDeclaration.Members);
			Assert.AreEqual(2, moduleDeclaration.Members.Count);
			Assert.AreEqual("a", moduleDeclaration.Members[0].Name.ToString());
			Assert.AreEqual("b", moduleDeclaration.Members[1].Name.ToString());
		}

		[TestMethod]
		public void ModuleMember_TypeMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: typedef Int32"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(TypeMember));
			Assert.AreEqual("a", moduleMember.Name.ToString());
			Assert.AreEqual("Int32", ((TypeMember)moduleMember).Type.ToString());
		}

		[TestMethod]
		public void ModuleMember_EnumMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: enum { red blue green }"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(EnumMember));
			Assert.AreEqual("a", moduleMember.Name.ToString());
			Assert.AreEqual("red blue green", string.Join(" ", ((EnumMember)moduleMember).Values));
		}

		[TestMethod]
		public void ModuleMember_ConstMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: const 5"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(ConstMember));
			Assert.AreEqual("a", moduleMember.Name.ToString());
			Assert.AreEqual("5", ((ConstMember)moduleMember).Expression.ToString());
		}

		[TestMethod]
		public void ModuleMember_VarMember()
		{
			ModuleMember moduleMember = new Parser().ModuleMember(new Lexer("a: Int32"));
			Assert.IsNotNull(moduleMember);
			Assert.IsInstanceOfType(moduleMember, typeof(VarMember));
			Assert.AreEqual("a", moduleMember.Name.ToString());
			Assert.AreEqual("Int32", ((VarMember)moduleMember).Type.ToString());
		}
	}
}
