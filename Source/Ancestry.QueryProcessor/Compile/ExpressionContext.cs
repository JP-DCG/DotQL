using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class ExpressionContext
	{
		public static readonly ExpressionContext Boolean = new ExpressionContext { Type = new Type.ScalarType { Type = typeof(Boolean) } };

		public Type.BaseType Type;

		// TODO: characteristics
	}
}
