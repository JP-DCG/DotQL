using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public interface IRepository<T>
	{
		T Get(Parse.Expression condition);
		void Set(Parse.Expression condition, T newValue);
	}
}
