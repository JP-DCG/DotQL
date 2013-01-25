using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	public interface IUpdateable<T> : IEnumerator<T>
	{
		void Update(T value);
	}
}
