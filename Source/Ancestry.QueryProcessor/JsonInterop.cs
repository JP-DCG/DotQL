using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor
{
	public static class JsonInterop
	{
		//public static TupleType InferArgumentTypes(JObject args, JObject argTypes)
		//{
		//	var result = new TupleType();

		//	// Use argument types if specified
		//	if (argTypes != null)
		//	{
		//		var parser = new Parse.Parser();
		//		foreach (var att in argTypes)
		//		{
		//			// TODO: assert that arr.value is a string for better error
		//			result.Members.Add(att.Key, new AttributeMember { Type = Parse.Parser.ParseFrom(parser.TypeDeclaration, (string)((JValue)att.Value).Value) });
		//		}
		//	}

		//	// Infer any types not known
		//	if (args != null)
		//		foreach (var att in args)
		//			if (!result.Members.ContainsKey(att.Key))
		//				result.Members.Add(att.Key, new AttributeMember { Type = InferTypeFromValue(att.Value) });

		//	return result;
		//}

		//public static BaseType InferTypeFromValue(JToken jToken)
		//{
		//	switch (jToken.Type)
		//	{
		//		case JTokenType.Boolean: return IntrinsicTypes.Boolean;
		//		case JTokenType.Date: return IntrinsicTypes.Date;
		//		case JTokenType.Float: return IntrinsicTypes.Double;
		//		case JTokenType.Guid: return IntrinsicTypes.Guid;
		//		case JTokenType.Integer: return IntrinsicTypes.Long;
		//		case JTokenType.Object: return InferTupleTypeFromValue((JObject)jToken);
		//		case JTokenType.Array: return InferListTypeFromValue((JArray)jToken);
		//		case JTokenType.String: return IntrinsicTypes.String;
		//		case JTokenType.TimeSpan: return IntrinsicTypes.TimeSpan;
		//		default: return null;
		//	}
		//}

		//public static ListType InferListTypeFromValue(JArray jArray)
		//{
		//	return jArray.Count > 0 ? new ListType { OfType = InferTypeFromValue(jArray[0]) } : null;
		//}

		//public static TupleType InferTupleTypeFromValue(JObject jObject)
		//{
		//	var result = new TupleType();
		//	foreach (var a in jObject)
		//		result.Members.Add(a.Key, new AttributeMember { Type = InferTypeFromValue(a.Value) });
		//	return result;
		//}

		public static Dictionary<string, object> JsonArgsToNative(JObject args)
		{
			return null;
			// TODO: dynamically construct class
		}

		public static JToken NativeToJson(object result)
		{
			if (result == null)
				return new JValue((object)null);

			var resultType = result.GetType();

			// TODO: handle set, list, tuple, and custom types
			switch (resultType.Name)
			{
				case "String" : 
				case "Int32" :
				case "Int64" :
				case "DateTime" :
				case "TimeSpan" :
				case "Guid" :
				case "Double" :
				case "Char" : return new JValue(result);

				default:
					 if (resultType.GetCustomAttributes(typeof(Type.TupleAttribute), false).Length > 0)
						return JObject.FromObject(result);
					 else
						return new JValue(result.ToString());
			}
		}
	}
}
