using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	public static class Runtime
	{
		public static void DeclareModule(Name moduleId, Version version, System.Type module, Storage.IRepositoryFactory factory)
		{
			GetModulesRepository(factory).Set
			(
				new Parse.BinaryExpression 
				{ 
					Left = new Parse.IdentifierExpression { Target = new Parse.QualifiedIdentifier { Components = new[] { "Name" } } },
					Operator = Parse.Operator.Equal,
					Right = new Parse.LiteralExpression { Value = moduleId }
				},
				new HashSet<ModuleTuple> { new ModuleTuple { Name = moduleId, Version = version, Class = module } }
			);
		}

		public static Storage.IRepository<ISet<ModuleTuple>> GetModulesRepository(Storage.IRepositoryFactory factory)
		{
			return factory.GetRepository<ISet<ModuleTuple>>(typeof(SystemModule), Name.FromComponents("Modules"));
		}

		public static T GetInitializer<T>(T initializer, IDictionary<string, object> args, Name name)
		{
			object arg;
			if (args != null && args.TryGetValue(name.ToString(), out arg))
				return (T)arg;
			return initializer;
		}
	}
}
