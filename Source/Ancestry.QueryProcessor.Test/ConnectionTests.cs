using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ancestry.QueryProcessor.Test
{
	[TestClass]
	public class ConnectionTests
	{
		[TestMethod]
		public void BasicEvaluate()
		{
			var connection = new Connection();
			
			var result = connection.Evaluate("return 'Hello world.'");
			Assert.AreEqual("Hello world.", result);

			result = connection.Evaluate("return 5 - 10 * 3");
			Assert.AreEqual(-25, result);
		}
	}
}
