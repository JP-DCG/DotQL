using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public class InMemoryFactory : IRepositoryFactory
	{
		private IEnumerable<KeyValuePair<Name, System.Type>> FindModules()
		{
			foreach 
			(
				var assembly in 
					AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
						.Union(from an in AdditionalAssemblies select Assembly.Load(an))
			)
				foreach (var moduleAttribute in assembly.GetCustomAttributes<Type.ModuleAttribute>())
					yield return new KeyValuePair<Name, System.Type>(moduleAttribute.Name, moduleAttribute.ModuleClass);
		}

		private Dictionary<ModuleVar, object> _repositories = new Dictionary<ModuleVar, object>();

		private IList<AssemblyName> _additionalAssemblies = new List<AssemblyName>();
		/// <summary> Additional assemblies to look in for modules. </summary>
		public IList<AssemblyName> AdditionalAssemblies { get { return _additionalAssemblies; } }

		private Runtime.Set<Runtime.ModuleTuple> _modules;
		public ISet<Runtime.ModuleTuple> Modules 
		{ 
			get 
			{ 
				if (_modules == null)
				{
					_modules = new Runtime.Set<Runtime.ModuleTuple>();
					foreach (var module in FindModules())
						_modules.Add(new Runtime.ModuleTuple { Name = module.Key, Version = new Version(1, 0), Class = module.Value });
				}
				return _modules; 
			} 
		}

		private struct ModuleVar
		{
			public System.Type Module; 
			public Name VarName;

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

		private InMemoryModuleRepository _moduleRepository;

		public IRepository<T> GetRepository<T>(System.Type module, Name varName)
		{
			if (module == typeof(Runtime.SystemModule) && varName.ToString() == "Modules")
			{
				switch (varName.ToString())
				{
					case "Modules":
						if (_moduleRepository == null)
							_moduleRepository = new InMemoryModuleRepository(this);
						return (IRepository<T>)_moduleRepository;
					//case "DefaultUsings":
					//	if (_usingsRepository == null)
					//		_usingsRepository = new InMemoryUsingRepository(this);
					//	return _usingsRepository;
					default:
						throw new NotSupportedException();
				}
			}
			else
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
}
