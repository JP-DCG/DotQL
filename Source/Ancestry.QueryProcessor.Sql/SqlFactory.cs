using Ancestry.QueryProcessor.Runtime;
using Ancestry.QueryProcessor.Storage;
using System;
using System.Configuration;
using System.Data.Common;
using System.Reflection;

namespace Ancestry.QueryProcessor.Sql
{
    public class SqlFactory : IRepositoryFactory
    {
		public SqlFactory()
		{
			var configuration = (SqlFactoryConfiguration)ConfigurationManager.GetSection("sqlFactory");
			if (configuration == null)
				throw new Exception("'sqlFactory' configuration section not configured.");
			
			var connectionInfo = ConfigurationManager.ConnectionStrings[configuration.ConnectionName];
			if (connectionInfo == null)
				throw new Exception(String.Format("Connection '{0}' is not configured.", configuration.ConnectionName));

			Configure(connectionInfo.ProviderName, connectionInfo.ConnectionString, null);
		}

		public SqlFactory(string providerName, string connectionString, IRepositoryFactory systemFactory = null)
		{
			Configure(providerName, connectionString, systemFactory);
		}

		private void Configure(string providerName, string connectionString, IRepositoryFactory systemFactory)
		{
			_dbFactory = DbProviderFactories.GetFactory(providerName);
			_connectionString = connectionString;
			_systemFactory = systemFactory ?? new InMemoryFactory();
		}

		private DbProviderFactory _dbFactory;
		public DbProviderFactory DbFactory { get { return _dbFactory; } }

		private IRepositoryFactory _systemFactory;

		private string _connectionString;
		public string ConnectionString { get { return _connectionString; } }

		public IRepository<T> GetRepository<T>(System.Type module, Name varName)
		{
			// Route system module requests into another factory
			if (module == typeof(SystemModule))
				return _systemFactory.GetRepository<T>(module, varName);

			var type = typeof(T);
			if (type.IsGenericType && type.GenericTypeArguments[0].GetCustomAttribute(typeof(Type.TupleAttribute), true) == null)
				throw new Exception("SqlFactory: Only sets of tuples (tables) are supported.");
			return new SqlRepository<T>(this, type.GenericTypeArguments[0], varName.ToString());
		}
	}
}
