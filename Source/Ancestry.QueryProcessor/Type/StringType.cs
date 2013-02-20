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
		public override ExpressionContext CompileOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.Operator op)
		{
			switch (op)
			{
				case Parse.Operator.Addition: 
					method.IL.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", new System.Type[] { typeof(string), typeof(string) }), null);
					break;
				case Parse.Operator.Equal: 
				case Parse.Operator.NotEqual: 
				case Parse.Operator.InclusiveGreater: 
				case Parse.Operator.InclusiveLess: 
				case Parse.Operator.Greater: 
				case Parse.Operator.Less: return base.CompileOperator(method, compiler, left, right, op);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", op));
			}
			return left;
		}

		public override BaseType Clone()
		{
			return new StringType { IsRepository = this.IsRepository, Type = this.Type };
		}
	}
}
