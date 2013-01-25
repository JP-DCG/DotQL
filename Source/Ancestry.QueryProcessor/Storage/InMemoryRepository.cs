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

		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}
	}
}
