using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public class InMemory : IRepository
	{
		public Runtime.SystemModule _system = new Runtime.SystemModule();

		public object GetInstance(System.Type module)
		{
			return module == typeof(Runtime.SystemModule);
		}

		public int GetCost(Plan.PlanTable table, Plan.ScriptPlan plan)
		{
			throw new NotImplementedException();
		}

		public IRepositoryCursor Open(Plan.PlanTable table)
		{
			throw new NotImplementedException();
		}

		public IRepositoryTransaction Begin()
		{
			throw new NotImplementedException();
		}
	}
}
