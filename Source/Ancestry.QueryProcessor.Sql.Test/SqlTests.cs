using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;

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
			var actual = _processor.Evaluate("using Test 1.0.0 return Parts");
			Assert.IsTrue(actual is JArray);
			Assert.IsTrue(((JArray)actual).Count > 0);
			Assert.IsTrue(((JArray)actual)[0] is JObject);
			Assert.IsNotNull(((JObject)((JArray)actual)[0])["ID"]);
		}
	}
}
