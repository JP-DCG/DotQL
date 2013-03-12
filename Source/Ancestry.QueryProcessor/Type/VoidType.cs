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
		public override ExpressionContext CompileBinaryExpression(Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, BaseType typeHint)
		{
			throw NotSupported(expression);
		}

		public override ExpressionContext CompileUnaryExpression(Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, BaseType typeHint)
		{
			throw NotSupported(expression);
		}

		public override int GetHashCode()
		{
			return 3931;	// arbitrary prime
		}

		public override bool Equals(object obj)
		{
			if (obj is VoidType)
				return (VoidType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(VoidType left, VoidType right)
		{
			return Object.ReferenceEquals(left, right)
				||
				(
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType()
				);
		}

		public static bool operator !=(VoidType left, VoidType right)
		{
			return !(left == right);
		}

		public override System.Type GetNative(Compile.Emitter emitter)
		{
			return typeof(Runtime.Void);
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", "Void") };
		}

		public override Parse.Expression BuildDefault()
		{
			throw new Exception("Internal Error: void type cannot be emitted as a default.");
		}
	}
}
