using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ancestry.QueryProcessor.Plan;

namespace Ancestry.QueryProcessor.Storage
{
	public interface IRepository
	{
		object GetInstance(System.Type module);

		int GetCost(PlanTable table, ScriptPlan plan);
		
		IRepositoryCursor Open(PlanTable table);

		IRepositoryTransaction Begin();
	}

	public interface IRepositoryCursor
	{
	}

	public interface IRepositoryTransaction
	{
	}
}
