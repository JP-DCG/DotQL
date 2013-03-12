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
					Left = new Parse.IdentifierExpression { Target = new Parse.ID { Components = new[] { "Name" } } },
					Operator = Parse.Operator.Equal,
					Right = new Parse.LiteralExpression { Value = moduleId }
				},
				new Set<ModuleTuple> { new ModuleTuple { Name = moduleId, Version = version, Class = module } }
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

		/// <remarks> http://www.johndcook.com/blog/2008/12/10/fast-exponentiation/ </remarks>
		public static long IntPower(int x, int power)
		{
			if (power == 0) return 1;
			if (power == 1) return x;

			int n = 15;
			while ((power <<= 1) >= 0) n--;

			long tmp = x;
			while (--n > 0)
				tmp = tmp * tmp *
					 (((power <<= 1) < 0) ? x : 1);
			return tmp;
		}
	}
}
