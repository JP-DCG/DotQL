using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace Ancestry.QueryProcessor.Service.Controllers
{
	public class EvalController : ApiController
	{
		public JToken Get(string e, string a = null)
		{
			//HttpContext.Response.ContentType = "application/json";
			var service = new Processor(new ProcessorSettings());
			return service.Evaluate(e, a == null ? null : JObject.Parse(a));
		}
	}
}