using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class OptionalType : BaseType, IComponentType
	{
		public OptionalType(BaseType of)
		{
			Of = of;
		}

		public BaseType Of { get; private set; }

		// TODO: handle exists and isnull for optional
		//protected override void EmitUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		//{
		//	switch (expression.Operator)
		//	{
		//		case Parse.Operator.Exists:
		//			if (Of.GetNative(compiler.Emitter).IsValueType)
		//				...
		//			break;
		//		case Parse.Operator.IsNull:
		//			...
		//			break;
		//		default: throw NotSupported(expression);
		//	}
		//}

		public override System.Type GetNative(Emitter emitter)
		{
			var memberNative = Of.GetNative(emitter);
			if (memberNative.IsValueType)
				return typeof(Nullable<>).MakeGenericType(memberNative);
			else
				return memberNative;
		}

		public override int GetHashCode()
		{
			return 1019 + Of.GetHashCode();	// Arbitrary prime
		}

		public override bool Equals(object obj)
		{
			if (obj is OptionalType)
				return (OptionalType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(OptionalType left, OptionalType right)
		{
			return Object.ReferenceEquals(left, right)
				||
				(
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType()
						&& left.Of == right.Of
				);
		}

		public static bool operator !=(OptionalType left, OptionalType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Of.ToString() + "?";
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = null };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.OptionalType { Type = Of.BuildDOM() };
		}
	}
}
