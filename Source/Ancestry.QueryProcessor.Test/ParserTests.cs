using System;
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
			var statement = parser.ParseStatement("select 1, 2.0f, 3.0d, 0x4, $5, '6'");
			var emitter = new SQLTextEmitter();
			var actual = emitter.Emit(statement);
			Assert.AreEqual
			(
				@"select 
	1, 2, 3.0, 0x4, $5, '6';", 
				actual
			);
		}

		[TestMethod]
		public void ExpressionSelectWithAliasesTest()
		{
			var parser = new Parser();
			var statement = parser.ParseStatement("select 1 + 2 Three, '3' + four as Five");
			var emitter = new SQLTextEmitter();
			emitter.UseQuotedIdentifiers = true;
			var actual = emitter.Emit(statement);
			Assert.AreEqual
			(
				@"select 
	(1 + 2) as ""Three"", ('3' + four) as ""Five"";",
				actual
			);

			emitter.UseQuotedIdentifiers = false;
			emitter.UseStatementTerminator = false;
			actual = emitter.Emit(statement);
			Assert.AreEqual
			(
				@"select 
	(1 + 2) as Three, ('3' + four) as Five",
				actual
			);
		}

		[TestMethod]
		public void SelectWithJoinsTest()
		{
			var parser = new Parser();
			var statement = parser.ParseStatement("select A, A + 1 B from TableX left join TableY on TableX.A = TableY.A join TableY");
			var emitter = new SQLTextEmitter();
			var actual = emitter.Emit(statement);
			Assert.AreEqual
			(
				@"select 
	A, (A + 1) as B
	from from as ;",
				actual
			);
		}
	}
}
