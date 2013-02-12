using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class NaryTypeHandler : TypeHandler
	{
		public override Expression CompileBinaryExpression(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Dereference: return CompileDereference(compiler, frame, left, expression, typeHint);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		protected virtual Expression CompileDereference(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			left = compiler.MaterializeReference(left);

			var local = compiler.AddFrame(frame, expression);
			var memberType = left.Type.GenericTypeArguments[0];
			var parameters = new List<ParameterExpression>();

			var valueParam = compiler.CreateValueParam(expression, local, left, memberType);
			parameters.Add(valueParam);

			var indexParam = compiler.CreateIndexParam(expression, local);
			parameters.Add(indexParam);

			var right = 
				compiler.MaterializeReference
				(
					compiler.CompileExpression(local, expression.Right, typeHint)
				);

			var selection = Expression.Lambda(right, parameters);
			var select = 
				typeof(Enumerable).GetMethodExt
				(
					"Select", 
					new System.Type[] { typeof(IEnumerable<ReflectionUtility.T>), typeof(Func<ReflectionUtility.T, int, ReflectionUtility.T>) }
				);
			select = select.MakeGenericMethod(memberType, selection.ReturnType);
			return Expression.Call(select, left, selection);
		}

		public override Expression CompileUnaryExpression(Compiler compiler, Frame frame, Expression inner, Parse.UnaryExpression expression, System.Type typeHint)
		{
			inner = compiler.MaterializeReference(inner);

			switch (expression.Operator)
			{
				case Parse.Operator.Exists: 
					return Expression.GreaterThan
					(
						Expression.Property(inner, typeof(ICollection<>).MakeGenericType(inner.Type).GetProperty("Count")), 
						Expression.Constant(0)
					);
				case Parse.Operator.IsNull: return Expression.Constant(false);

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}
	}
}
