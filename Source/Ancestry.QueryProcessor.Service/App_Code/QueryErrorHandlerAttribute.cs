using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;

namespace Ancestry.QueryProcessor.Service
{
	public class QueryErrorHandlerAttribute : HandleErrorAttribute
	{
		public override void OnException(ExceptionContext filterContext)
		{
			if (filterContext.Exception != null)
			{
				filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				var exception = filterContext.Exception;
				var data = new List<object>();
				if (exception is AggregateException)
				{
					data.AddRange
					(
						from e in ((AggregateException)exception).InnerExceptions
						let locatedException = e as ILocatedException
						select new
						{
							Message = e.Message,
							Line = (locatedException == null ? -1 : locatedException.Line),
							LinePos = (locatedException == null ? -1 : locatedException.LinePos)
						}
					);
				}
				else
				{
					var locatedException = exception as ILocatedException;
					data.Add
					(
						new
						{
							Message = exception.Message,
							Line = (locatedException == null ? -1 : locatedException.Line),
							LinePos = (locatedException == null ? -1 : locatedException.LinePos)
						}
					);
				}

				filterContext.Result = new JsonResult
				{
					JsonRequestBehavior = JsonRequestBehavior.AllowGet,
					Data = data
				}; 
				filterContext.ExceptionHandled = true;
			}
			else
				base.OnException(filterContext);
		}
	}
}