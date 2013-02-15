using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Runtime
{
	public delegate object ExecuteHandler(IDictionary<string, object> args, Storage.IRepositoryFactory factory, CancellationToken cancelToken);
}
