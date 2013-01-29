﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ancestry.QueryProcessor.Plan;

namespace Ancestry.QueryProcessor.Storage
{
	public interface IRepository<T>
	{
		int GetCost(ScriptPlan plan);
		
		T Get(Parse.Expression condition);
		void Set(Parse.Expression condition, T newValue);
	}
}
