using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace Ancestry.QueryProcessor.Service.Controllers
{
	public class EvalController : Controller
	{
		[QueryErrorHandler]
		public JsonResult Index(string e, string a = null)
		{
			var service = new Processor(QueryConfig.Settings);
			var result = service.Evaluate(e, a == null ? null : JsonInterop.JsonArgsToNative(JObject.Parse(a)));
			return Json(result, JsonRequestBehavior.AllowGet);
		}
	}
}