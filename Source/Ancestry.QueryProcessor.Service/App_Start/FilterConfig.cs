using System.Web;
using System.Web.Mvc;

namespace Ancestry.QueryProcessor.Service
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}