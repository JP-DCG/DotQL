using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	static class ReflectionUtility
	{
		public static readonly MethodInfo ObjectEquals = typeof(object).GetMethod("Equals", new System.Type[] { typeof(object) });
		public static readonly MethodInfo ObjectGetHashCode = typeof(object).GetMethod("GetHashCode");
	}
}
