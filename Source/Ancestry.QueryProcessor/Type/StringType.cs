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

		protected override ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Addition: 
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringConcat, null);
					break;

				case Parse.Operator.Equal: 
					return base.DefaultBinaryOperator(method, compiler, left, right, expression);
				case Parse.Operator.NotEqual:
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.InclusiveGreater:
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Clt);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.InclusiveLess:
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					// Not
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Ceq);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.Greater:
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Cgt);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.Less:
					method.IL.EmitCall(OpCodes.Call, ReflectionUtility.StringCompare, null);
					method.IL.Emit(OpCodes.Ldc_I4_0);
					method.IL.Emit(OpCodes.Clt);
					return new ExpressionContext(SystemTypes.Boolean);

				default: throw NotSupported(expression);
			}
			return left;
		}
	}
}
