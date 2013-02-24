using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class VoidType : BaseType
	{
		public override ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, BaseType typeHint)
		{
			throw NotSupported(expression);
		}

		public override ExpressionContext CompileUnaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, BaseType typeHint)
		{
			throw NotSupported(expression);
		}

		public override System.Type GetNative(Compile.Emitter emitter)
		{
			return typeof(Runtime.Void);
		}
	}
}
