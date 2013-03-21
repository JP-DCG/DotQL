using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;

namespace Ancestry.QueryProcessor.Service
{
	public static class JsonInterop
	{
		public static Dictionary<string, object> JsonArgsToNative(JObject args)
		{
			// TODO: allow complex object to be passed

			if (args != null && args.Count > 0)
			{
				var result = new Dictionary<string, object>(args.Count);
				foreach (var p in args.Properties())
					result.Add(p.Name, ((JValue)p.Value).Value);
				return result;
			}
			else
				return null;
		}

		public static JToken NativeToJson(object result)
		{
			if (result == null)
				return new JValue((object)null);

			var resultType = result.GetType();

			return JToken.FromObject(result);
		}
	}
}
