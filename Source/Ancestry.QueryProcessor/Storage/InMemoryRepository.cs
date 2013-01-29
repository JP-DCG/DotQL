using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public class InMemoryRepository<T> : IRepository<T>
	{
		public int GetCost(Plan.ScriptPlan plan)
		{
			throw new NotImplementedException();
		}

		private T _value;

		public T Get(Parse.Expression condition)
		{
			return _value;
		}

		public void Set(Parse.Expression condition, T newValue)
		{
			_value = newValue;
		}
	}
}
