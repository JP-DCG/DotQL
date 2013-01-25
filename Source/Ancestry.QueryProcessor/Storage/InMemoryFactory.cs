using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public class InMemoryFactory : IRepositoryFactory
	{
		private Dictionary<ModuleVar, object> _repositories = new Dictionary<ModuleVar, object>();

		private struct ModuleVar
		{
			public System.Type Module; 
			public QualifiedID VarName;

			public override bool Equals(object obj)
			{
				if (obj is ModuleVar)
				{
					var other = (ModuleVar)obj;
					return other.Module == this.Module && other.VarName == this.VarName;
				}
				else
					return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return Module.GetHashCode() * 83 + VarName.GetHashCode();
			}
		}

		public IRepository<T> GetRepository<T>(System.Type module, QualifiedID varName)
		{
			var moduleVar = new ModuleVar { Module = module, VarName = varName };
			object repo;
			if (_repositories.TryGetValue(moduleVar, out repo))
				return (IRepository<T>)repo;
			IRepository<T> typedRepo = new InMemoryRepository<T>();
			_repositories.Add(moduleVar, typedRepo);
			return typedRepo;
		}
	}
}
