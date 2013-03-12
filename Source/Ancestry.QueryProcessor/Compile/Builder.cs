using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public static class Builder
	{
		public static Expression ID(string[] components)
		{
			return Expression.MemberInit
			(
				Expression.New(typeof(Parse.ID)),
				Expression.Bind
				(
					typeof(Name).GetField("Components"),
					Expression.NewArrayInit(typeof(string), from t in components select Expression.Constant(t))
				)
			);
		}

		public static MemberInitExpression IdentifierExpression(Expression target)
		{
			return Expression.MemberInit
			(
				Expression.New(typeof(Parse.IdentifierExpression)),
				Expression.Bind(typeof(Parse.IdentifierExpression).GetProperty("Target"), target)
			);
		}

		public static MemberInitExpression BinaryExpression(Parse.Operator op, Expression leftValue, Expression rightValue)
		{
			return Expression.MemberInit
			(
				Expression.New(typeof(Parse.BinaryExpression)),
				Expression.Bind(typeof(Parse.BinaryExpression).GetProperty("Operator"), Expression.Constant(op)),
				Expression.Bind(typeof(Parse.BinaryExpression).GetProperty("Left"), leftValue),
				Expression.Bind(typeof(Parse.BinaryExpression).GetProperty("Right"), rightValue)
			);
		}
	}
}
