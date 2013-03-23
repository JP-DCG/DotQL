using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class ExpressionContext
	{
		public ExpressionContext(Parse.Expression expression, Type.BaseType type, Characteristic characteristics, Action<MethodContext> emitGet)
		{
			Expression = expression;
			Type = type;
			Characteristics = characteristics;
			EmitGet = emitGet;
		}

		public Parse.Expression Expression;

		public Characteristic Characteristics;

		/// <summary> The DotQL data type. </summary>
		public Type.BaseType Type;

		/// <summary> The native type; null if the native type is "natural" (same as Type.GetNative). </summary>
		public System.Type NativeType;

		/// <summary> The member information within the parent type. </summary>
		public object Member;

		public Action<MethodContext> EmitGet;

		public Action<MethodContext, Action<MethodContext>> EmitSet;

		public Action<MethodContext> EmitMethod;
						
		public bool IsRepository()
		{
			return ReflectionUtility.IsRepository(NativeType);
		}

		public System.Type ActualNative(Emitter emitter)
		{
			return NativeType ?? Type.GetNative(emitter);
		}

		public ExpressionContext Clone()
		{
			return
				new ExpressionContext
				(
					Expression,
					Type,
					Characteristics,
					EmitGet
				)
				{
					EmitMethod = EmitMethod,
					EmitSet = EmitSet,
					Member = Member,
					NativeType = NativeType
				};
		}
	}
}
