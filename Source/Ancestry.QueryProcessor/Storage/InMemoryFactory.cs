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
		private IEnumerable<System.Type> FindModules()
		{
			foreach 
			(
				var assembly in 
					AppDomain.CurrentDomain.GetAssemblies()
						.Union(from an in AdditionalAssemblies select Assembly.Load(an))
			)
				foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(Type.ModuleAttribute), true).Length > 0))
					yield return type;
		}

		private static Runtime.ModuleTuple GetModuleTuple(System.Type module)
		{
			var attribute = (Type.ModuleAttribute)module.GetCustomAttribute(typeof(Type.ModuleAttribute));
			if (attribute != null)
				return new Runtime.ModuleTuple { Name = attribute.Name, Version = new Version(1, 0), Class = module };
			else
				return new Runtime.ModuleTuple { Name = Name.FromComponents(module.FullName.Split('.')), Version = new Version(1, 0), Class = module };
		}

		private Dictionary<ModuleVar, object> _repositories = new Dictionary<ModuleVar, object>();

		private IList<AssemblyName> _additionalAssemblies = new List<AssemblyName>();
		/// <summary> Additional assemblies to look in for modules. </summary>
		public IList<AssemblyName> AdditionalAssemblies { get { return _additionalAssemblies; } }

		private HashSet<Runtime.ModuleTuple> _modules;
		public ISet<Runtime.ModuleTuple> Modules 
		{ 
			get 
			{ 
				if (_modules == null)
				{
					_modules = new HashSet<Runtime.ModuleTuple>();
					foreach (var module in FindModules())
						_modules.Add(GetModuleTuple(module));
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
			if (module == typeof(Runtime.SystemModule))
			{
				// if (varName == "Modules")
				if (_moduleRepository == null)
					_moduleRepository = new InMemoryModuleRepository(this);
				return (IRepository<T>)_moduleRepository;
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
