using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class EnumType: BaseType
	{
		public EnumType(System.Type nativeType)
		{
			NativeType = nativeType;
		}

		public System.Type NativeType { get; private set; }

		public override System.Type GetNative(Compile.Emitter emitter)
		{
			return NativeType;
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = ReflectionUtility.GetDefaultValue(NativeType) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			throw new NotImplementedException("Enum type doesn't support DOM");
		}

		// TODO: define operators for Enum

		public override void EmitLiteral(Compile.MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldc_I4, (int)value);
		}
	}
}
