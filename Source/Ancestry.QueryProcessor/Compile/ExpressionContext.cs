using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public struct ExpressionContext
	{
		public ExpressionContext(Type.BaseType type, System.Type nativeType = null, object member = null)
		{
			Type = type;
			NativeType = nativeType;
			Member = member;
		}

		// The DotQL data type
		public Type.BaseType Type;

		// The native type; null if the native type is "natural" (same as Type.GetNative)
		public System.Type NativeType;

		// The member information within the parent type
		public object Member;

		// TODO: characteristics

		public bool IsRepository()
		{
			return ReflectionUtility.IsRepository(NativeType);
		}
	}
}
