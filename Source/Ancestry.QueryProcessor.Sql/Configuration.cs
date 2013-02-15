using System;
using System.Configuration;

namespace Ancestry.QueryProcessor.Sql
{
	public class SqlFactoryConfiguration : ConfigurationSection
	{
		[ConfigurationProperty("connectionName", IsRequired = true)]
		public string ConnectionName
		{
			get { return (string)base["connectionName"]; }
			set { base["connectionName"] = value; }
		}

		public override string ToString()
		{
			return String.Format
			(
				"ConnectionName: {0}",
				ConnectionName
			);
		}
	}
}
