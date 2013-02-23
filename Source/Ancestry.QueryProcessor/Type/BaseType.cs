using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public abstract class BaseType
	{
		public abstract System.Type GetNative(Emitter emitter);

		public virtual ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			throw new NotSupportedException(String.Format("Operator '{0}' is not supported for type '{1}'.", expression.Operator, GetType()));
		}

		public virtual ExpressionContext CompileUnaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, Type.BaseType typeHint)
		{
			throw new NotSupportedException(String.Format("Operator '{0}' is not supported for type '{1}'.", expression.Operator, GetType()));
		}

		/// <summary> Attempt to invoke an operator overload on the left-hand class if there is one. </summary>
		protected bool CallClassOp(MethodContext method, string opName, params System.Type[] types)
		{
			var classOp = types[0].GetMethod(opName, types);
			if (classOp != null)
			{
				method.IL.EmitCall(OpCodes.Call, classOp, null);
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public override bool Equals(object obj)
		{
			throw new NotSupportedException();
		}

		public static bool operator ==(BaseType left, BaseType right)
		{
			return Object.ReferenceEquals(left, right) || (!Object.ReferenceEquals(left, null) && !Object.ReferenceEquals(right, null) && left.Equals(right));
		}

		public static bool operator !=(BaseType left, BaseType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return GetType().Name.Replace("Type", "");
		}
	}
}
