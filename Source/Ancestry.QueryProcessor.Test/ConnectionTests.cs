using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ConnectionTests
	{
		[TestMethod]
		public void EmptyScript()
		{
			var connection = new Connection();
			
			connection.Execute("\\Nothing but a comment");
			var result = connection.Evaluate("\\Nothing but a comment");
			Assert.AreEqual(new Newtonsoft.Json.Linq.JValue((object)null), result);
		}
		
		[TestMethod]
		public void BasicEvaluate()
		{
			var connection = new Connection();
			
			var result = connection.Evaluate("return 'Hello world.'");
			Assert.AreEqual("Hello world.", result);

			result = connection.Evaluate("return 5 - 10 * 3");
            Assert.AreEqual( ( 5 - 10 * 3 ), result );

            result = connection.Evaluate( "return 5 * 3 + 1" );
            Assert.AreEqual( ( 5 * 3 + 1 ), result );
		}

		[TestMethod]
		public void LocalSymbolEvaluate()
		{
			var connection = new Connection();
			
			var result = connection.Evaluate("let x := 'Hello world.' return x");
			Assert.AreEqual("Hello world.", result);
		}
	}
}
