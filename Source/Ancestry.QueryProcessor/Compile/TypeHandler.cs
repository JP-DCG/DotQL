using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Compile
{
	public class TypeHandler
	{
		public virtual Expression CompileBinaryExpression(Compiler compiler, Frame frame, Expression left, Parse.BinaryExpression expression, System.Type typeHint)
		{
			throw new NotSupportedException();
		}

		public virtual Expression CompileUnaryExpression(Compiler compiler, Frame frame, Expression inner, Parse.UnaryExpression expression, System.Type typeHint)
		{
			throw new NotImplementedException();
		}
	}
}
