using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace Ancestry.QueryProcessor.Service.Controllers
{
    public class ExecController : Controller
    {
		[QueryErrorHandler]
		public void Index(string e, string a = null)
		{
			var service = new Processor(QueryConfig.Settings);
			service.Execute(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
		}
	}
}
