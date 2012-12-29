using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Execute
{
	public delegate object ExecuteHandler(Dictionary<string, object> args, CancellationToken cancelToken);
}
