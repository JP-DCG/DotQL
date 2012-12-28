using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ancestry.QueryProcessor.Plan;

namespace Ancestry.QueryProcessor.Storage
{
	public interface IStorageModule
	{
		int GetCost(PlanTable table, ScriptPlan plan);
		
		ICursor Open(PlanTable table);

		IStorageTransaction Begin();
	}

	public interface ICursor
	{
	}

	public interface IStorageTransaction
	{
	}
}
