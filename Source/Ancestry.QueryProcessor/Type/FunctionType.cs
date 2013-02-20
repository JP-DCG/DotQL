using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class FunctionType : BaseType
	{
		public override Compile.ExpressionContext CompileBinaryExpression(Compile.MethodContext method, Compile.Compiler compiler, Compile.Frame frame, Compile.ExpressionContext left, Parse.BinaryExpression expression, BaseType typeHint)
		{
			return base.CompileBinaryExpression(method, compiler, frame, left, expression, typeHint);
		}

		public override Compile.ExpressionContext CompileUnaryExpression(Compile.MethodContext method, Compile.Compiler compiler, Compile.Frame frame, Compile.ExpressionContext inner, Parse.UnaryExpression expression, BaseType typeHint)
		{
			return base.CompileUnaryExpression(method, compiler, frame, inner, expression, typeHint);
		}

		public override System.Type GetNative(Compile.Emitter emitter)
		{
			throw new NotImplementedException();
		}

		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}

		public override bool Equals(object obj)
		{
			if (obj is FunctionType)
				return (FunctionType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(FunctionType left, FunctionType right)
		{
			throw new NotImplementedException();
			//return Object.ReferenceEquals(left, right)
			//	|| ;
		}

		public static bool operator !=(FunctionType left, FunctionType right)
		{
			return !(left == right);
		}

		public override BaseType Clone()
		{
			throw new NotImplementedException();
		}
	}
}
