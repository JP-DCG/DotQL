using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class StringType : ScalarType
	{
		public StringType() : base(typeof(string)) { }

		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Addition: 
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringConcat, null);
					break;

				case Parse.Operator.Equal: 
					base.EmitBinaryOperator(method, compiler, left, right, expression);
					break;
				case Parse.Operator.NotEqual:
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					break;
				case Parse.Operator.InclusiveGreater:
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Clt);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					break;
				case Parse.Operator.InclusiveLess:
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					break;
				case Parse.Operator.Greater:
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					break;
				case Parse.Operator.Less:
					left.EmitGet(method);
					right.EmitGet(method);
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Clt);
					break;

				default: throw NotSupported(expression);
			}
		}

		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = "" };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "String") };
		}

		public override void EmitLiteral(MethodContext method, object value)
		{
			method.IL.Emit(OpCodes.Ldstr, (string)value);
		}
	}
}
