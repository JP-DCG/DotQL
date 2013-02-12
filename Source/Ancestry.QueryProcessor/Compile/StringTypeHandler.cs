using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class StringTypeHandler : ScalarTypeHandler
	{
		public override Expression CompileOperator(Expression left, Expression right, Parse.Operator op)
		{
			switch (op)
			{
				case Parse.Operator.Addition: return Expression.Call(typeof(string).GetMethod("Concat", new System.Type[] { typeof(string), typeof(string) }), left, right);

				case Parse.Operator.Equal: return Expression.Equal(left, right);
				case Parse.Operator.NotEqual: return Expression.NotEqual(left, right);
				case Parse.Operator.InclusiveGreater: return Expression.GreaterThanOrEqual(left, right);
				case Parse.Operator.InclusiveLess: return Expression.LessThanOrEqual(left, right);
				case Parse.Operator.Greater: return Expression.GreaterThan(left, right);
				case Parse.Operator.Less: return Expression.LessThan(left, right);

				default: throw new NotSupportedException();
			}
		}
	}
}
