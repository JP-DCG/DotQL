using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Storage
{
	public interface IRepositoryFactory
	{
		/// <summary> Given module and variable information, return a repository to serve up data for that variable. </summary>
		IRepository<T> GetRepository<T>(System.Type module, QualifiedID varName);
	}
}
