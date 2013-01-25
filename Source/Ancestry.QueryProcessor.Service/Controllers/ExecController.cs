using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Ancestry.QueryProcessor.Service.Controllers
{
    public class ExecController : ApiController
    {
		public void Post(string e, string a = null)
		{
			var service = new Processor(new ProcessorSettings());
			service.Execute(e, a == null ? null : JObject.Parse(a));
		}
	}
}
