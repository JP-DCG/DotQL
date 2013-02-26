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
			left = compiler.MaterializeRepository(method, left);

			switch (expression.Operator)
			{
				case Parse.Operator.Addition:
				case Parse.Operator.Subtract:
				case Parse.Operator.Multiply:
				case Parse.Operator.Modulo:
				case Parse.Operator.Divide:
				case Parse.Operator.Power:

				case Parse.Operator.BitwiseAnd:
				case Parse.Operator.BitwiseOr:
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor:
				case Parse.Operator.ShiftLeft:
				case Parse.Operator.ShiftRight:
					{
						var right = compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right, typeHint));
						return DefaultBinaryOperator(method, compiler, left, right, expression);
					}

				case Parse.Operator.And:
				case Parse.Operator.Or:
					{
						var right = compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right, typeHint));
						return DefaultShortCircuit(method, compiler, frame, left, expression, typeHint);
					}

				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
				case Parse.Operator.InclusiveGreater:
				case Parse.Operator.InclusiveLess:
				case Parse.Operator.Greater:
				case Parse.Operator.Less:
					{
						var right = compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right));	// (no type hint)
						return DefaultBinaryOperator(method, compiler, left, right, expression);
					}

				default: throw NotSupported(expression);
			}
		}

		public virtual ExpressionContext CompileUnaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, Type.BaseType typeHint)
		{
			inner = compiler.MaterializeRepository(method, inner);
			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
				case Parse.Operator.IsNull:
				case Parse.Operator.Negate:
				case Parse.Operator.Not:
				case Parse.Operator.BitwiseNot:
				case Parse.Operator.Successor:
				case Parse.Operator.Predicessor:
					return DefaultUnaryOperator(method, compiler, inner, expression);

				default: throw NotSupported(expression);
			}
		}

		public virtual ExpressionContext CompileRestrictExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.RestrictExpression expression, BaseType typeHint)
		{
			throw new CompilerException(expression, CompilerException.Codes.OperatorNotSupported, Parse.Keywords.Restrict, GetType());
		}

		protected Exception NotSupported(Parse.BinaryExpression expression)
		{
			return new CompilerException(expression, CompilerException.Codes.OperatorNotSupported, expression.Operator, GetType());
		}

		protected Exception NotSupported(Parse.UnaryExpression expression)
		{
			return new CompilerException(expression, CompilerException.Codes.OperatorNotSupported, expression.Operator, GetType());
		}

		/// <summary> Overridden to determine what operators a type supports and to change how they are implemented. </summary>
		/// <remarks> Override this rather than CompileBinaryOperator when nothing special is necessary when compiling the right-hand expression. </remarks>
		protected virtual ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			var leftType = left.Type.GetNative(compiler.Emitter);
			var rightType = right.Type.GetNative(compiler.Emitter);
			switch (expression.Operator)
			{
				case Parse.Operator.Addition:
					if (!CallClassOp(method, "op_Addition", leftType, rightType))
						method.IL.Emit(OpCodes.Add);
					break;
				case Parse.Operator.Subtract:
					if (!CallClassOp(method, "op_Subtraction", leftType, rightType))
						method.IL.Emit(OpCodes.Sub);
					break;
				case Parse.Operator.Multiply:
					if (!CallClassOp(method, "op_Multiply", leftType, rightType))
						method.IL.Emit(OpCodes.Mul);
					break;
				case Parse.Operator.Modulo:
					if (!CallClassOp(method, "op_Modulus", leftType, rightType))
						method.IL.Emit(OpCodes.Rem);
					break;
				case Parse.Operator.Divide:
					if (!CallClassOp(method, "op_Division", leftType, rightType))
						method.IL.Emit(OpCodes.Div);
					break;
				case Parse.Operator.Power:
					var mathPower = typeof(System.Math).GetMethod("Pow", new[] { left.Type.GetNative(compiler.Emitter), right.Type.GetNative(compiler.Emitter) });
					if (mathPower == null)
						throw new NotSupportedException();
					method.IL.EmitCall(OpCodes.Call, mathPower, null);
					break;
				case Parse.Operator.BitwiseAnd:
					if (!CallClassOp(method, "op_BitwiseOr", leftType, rightType))
						method.IL.Emit(OpCodes.And);
					break;
				case Parse.Operator.BitwiseOr:
					if (!CallClassOp(method, "op_Addition", leftType, rightType))
						method.IL.Emit(OpCodes.Or);
					break;
				case Parse.Operator.BitwiseXor:
				case Parse.Operator.Xor:
					if (!CallClassOp(method, "op_ExclusiveOr", leftType, rightType))
						method.IL.Emit(OpCodes.Xor);
					break;
				case Parse.Operator.ShiftLeft:
					if (!CallClassOp(method, "op_LeftShift", leftType, rightType))
						method.IL.Emit(OpCodes.Shl);
					break;
				case Parse.Operator.ShiftRight:
					if (!CallClassOp(method, "op_RightShift", leftType, rightType))
						method.IL.Emit(OpCodes.Shr);
					break;

				case Parse.Operator.Equal:
					if (!CallClassOp(method, "op_Equality", leftType, rightType))
						method.IL.Emit(OpCodes.Ceq);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.NotEqual:
					if (!CallClassOp(method, "op_Inequality", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Ceq);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.InclusiveGreater:
					if (!CallClassOp(method, "op_GreaterThanOrEqual", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Clt);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.InclusiveLess:
					if (!CallClassOp(method, "op_LessThanOrEqual", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Cgt);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.Greater:
					if (!CallClassOp(method, "op_GreaterThan", leftType, rightType))
						method.IL.Emit(OpCodes.Cgt);
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.Less:
					if (!CallClassOp(method, "op_LessThan", leftType, rightType))
						method.IL.Emit(OpCodes.Clt);
					return new ExpressionContext(SystemTypes.Boolean);

				default: throw NotSupported(expression);
			}
			return left;
		}

		/// <summary> Overridden to determine what operators a type supports and to change how they are implemented. </summary>
		/// <remarks> Override this rather than CompileBinaryOperator when nothing special is necessary when compiling the right-hand expression. </remarks>
		protected virtual ExpressionContext DefaultShortCircuit(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			var leftType = left.Type.GetNative(compiler.Emitter);
			switch (expression.Operator)
			{
				case Parse.Operator.And:
					if (!CallClassOp(method, "op_LogicalAnd", leftType, leftType))
					{
						var label = method.IL.DefineLabel();
						method.IL.Emit(OpCodes.Dup);
						method.IL.Emit(OpCodes.Brfalse_S, label);
						compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right, typeHint));
						method.IL.Emit(OpCodes.And);
						method.IL.MarkLabel(label);
					}
					break;

				case Parse.Operator.Or:
					if (!CallClassOp(method, "op_LogicalOr", leftType, leftType))
					{
						var label = method.IL.DefineLabel();
						method.IL.Emit(OpCodes.Dup);
						method.IL.Emit(OpCodes.Brtrue_S, label);
						compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right, typeHint));
						method.IL.Emit(OpCodes.Or);
						method.IL.MarkLabel(label);
					}
					break;

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
			return left;
		}

		/// <summary> Overridden to determine what operators a type supports and to change how they are implemented. </summary>
		protected virtual ExpressionContext DefaultUnaryOperator(MethodContext method, Compiler compiler, ExpressionContext inner, Parse.UnaryExpression expression)
		{
			var innerType = inner.Type.GetNative(compiler.Emitter);

			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
					method.IL.Emit(OpCodes.Pop);
					method.IL.Emit(OpCodes.Ldc_I4_1);	// true
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.IsNull:
					method.IL.Emit(OpCodes.Pop);
					method.IL.Emit(OpCodes.Ldc_I4_0);	// false 
					return new ExpressionContext(SystemTypes.Boolean);
				case Parse.Operator.Negate:
					if (!CallClassOp(method, "op_UnaryNegation", innerType))
						method.IL.Emit(OpCodes.Neg);
					break;
				case Parse.Operator.Not:
					if (!CallClassOp(method, "op_Negation", innerType))
						method.IL.Emit(OpCodes.Not);
					break;
				case Parse.Operator.BitwiseNot:
					if (!CallClassOp(method, "op_OnesComplement", innerType))
						method.IL.Emit(OpCodes.Not);
					break;
				case Parse.Operator.Successor:
					method.IL.Emit(OpCodes.Ldc_I4_1);
					method.IL.Emit(OpCodes.Add);
					break;
				case Parse.Operator.Predicessor:
					method.IL.Emit(OpCodes.Ldc_I4_1);
					method.IL.Emit(OpCodes.Sub);
					break;

				default: throw NotSupported(expression);
			}

			return inner;
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
