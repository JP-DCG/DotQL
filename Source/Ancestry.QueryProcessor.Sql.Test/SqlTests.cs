using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Linq;

namespace Ancestry.QueryProcessor.Sql.Test
{
	[TestClass]
	public class SqlTests
	{
		[TestInitialize]
		public void InitializeProvider()
		{
			string moduleCode;
			using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Ancestry.QueryProcessor.Sql.Test.Module.dql")))
				moduleCode = reader.ReadToEnd();

			var settings = new ProcessorSettings();
			settings.RepositoryFactory = new SqlFactory("System.Data.SqlServerCe.4.0", "Data Source=TestDB.sdf;Persist Security Info=False;");
			_processor = new Processor(settings);
			_processor.Execute(moduleCode);
		}

		private Processor _processor;

		[TestMethod]
		public void TestSelectAll()
		{
			dynamic actual = _processor.Evaluate("using Test 1.0.0 return Parts");
			actual = Enumerable.ToList(actual);
			Assert.IsTrue(actual.Count > 0);
			Assert.IsNotNull(actual[0].ID);
		}
	}
}
