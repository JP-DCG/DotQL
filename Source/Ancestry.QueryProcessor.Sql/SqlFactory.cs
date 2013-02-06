using Ancestry.QueryProcessor.Runtime;
using Ancestry.QueryProcessor.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Sql
{
    public class SqlFactory : IRepositoryFactory
    {
		public SqlFactory(string providerName, IRepositoryFactory systemFactory = null)
		{
			_dbFactory = DbProviderFactories.GetFactory(providerName);
			_systemFactory = systemFactory ?? new InMemoryFactory();
		}

		private DbProviderFactory _dbFactory;
		public DbProviderFactory DbFactory { get { return _dbFactory; } }

		private IRepositoryFactory _systemFactory;

		public IRepository<T> GetRepository<T>(System.Type module, Name varName)
		{
			// Route system module requests into another factory
			if (module == typeof(SystemModule))
				return _systemFactory.GetRepository<T>(module, varName);

			var type = typeof(T);
			if (type.IsGenericType && type.GenericTypeArguments[0].GetCustomAttributes(typeof(Type.TupleAttribute), true).Length == 0)
				throw new Exception("SqlFactory: Only sets of tuples (tables) are supported.");
			return new SqlRepository<T>(this, type.GenericTypeArguments[0], varName.ToString());
		}
	}
}
