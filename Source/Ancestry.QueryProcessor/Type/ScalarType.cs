using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class ScalarType : BaseType
	{
		public System.Type Type { get; set; }

		public override System.Type GetNative(Emitter emitter)
		{
			return Type;
		}

		public override int GetHashCode()
		{
			return IsRepository.GetHashCode() * 83 + Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is ScalarType)
				return (ScalarType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(ScalarType left, ScalarType right)
		{
			return Object.ReferenceEquals(left, right)
				|| (left.IsRepository == right.IsRepository && left.Type == right.Type);
		}

		public static bool operator !=(ScalarType left, ScalarType right)
		{
			return !(left == right);
		}

		public override BaseType Clone()
		{
			return new ScalarType { IsRepository = this.IsRepository, Type = this.Type };
		}

		public override ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
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
						return CompileOperator(method, compiler, left, right, expression.Operator);
					}

				case Parse.Operator.And:
				case Parse.Operator.Or:
					{
						var right = compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right, typeHint));
						return CompileShortCircuit(method, compiler, frame, left, expression, typeHint);
					}


				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
				case Parse.Operator.InclusiveGreater:
				case Parse.Operator.InclusiveLess:
				case Parse.Operator.Greater:
				case Parse.Operator.Less:
					{
						var right = compiler.MaterializeRepository(method, compiler.CompileExpression(method, frame, expression.Right));	// (no type hint)
						return CompileOperator(method, compiler, left, right, expression.Operator);
					}

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}
		}

		public virtual ExpressionContext CompileOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.Operator op)
		{
			var leftType = left.Type.GetNative(compiler.Emitter);
			var rightType = right.Type.GetNative(compiler.Emitter);
			switch (op)
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
					break;
				case Parse.Operator.NotEqual: 
					if (!CallClassOp(method, "op_Inequality", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Ceq);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					break;
				case Parse.Operator.InclusiveGreater:
					if (!CallClassOp(method, "op_GreaterThanOrEqual", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Clt);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					break;
				case Parse.Operator.InclusiveLess:
					if (!CallClassOp(method, "op_LessThanOrEqual", leftType, rightType))
					{
						method.IL.Emit(OpCodes.Cgt);
						method.IL.Emit(OpCodes.Ldc_I4_0);
						method.IL.Emit(OpCodes.Ceq);
					}
					break;
				case Parse.Operator.Greater:
					if (!CallClassOp(method, "op_GreaterThan", leftType, rightType))
						method.IL.Emit(OpCodes.Cgt); 
					break;
				case Parse.Operator.Less:
					if (!CallClassOp(method, "op_LessThan", leftType, rightType))
						method.IL.Emit(OpCodes.Clt); 
					break;

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", op));
			}
			return left;
		}

		public virtual ExpressionContext CompileShortCircuit(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
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

		public override ExpressionContext CompileUnaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext inner, Parse.UnaryExpression expression, Type.BaseType typeHint)
		{
			inner = compiler.MaterializeRepository(method, inner);
			var innerType = inner.Type.GetNative(compiler.Emitter);

			switch (expression.Operator)
			{
				case Parse.Operator.Exists:
					method.IL.Emit(OpCodes.Pop); 
					method.IL.Emit(OpCodes.Ldc_I4_1);	// true
					break;	
				case Parse.Operator.IsNull: 
					method.IL.Emit(OpCodes.Pop);
					method.IL.Emit(OpCodes.Ldc_I4_0);	// false 
					break;
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
				//case Parse.Operator.Successor: 
				//case Parse.Operator.Predicessor:

				default: throw new NotSupportedException(String.Format("Operator {0} is not supported.", expression.Operator));
			}

			return inner;
		}
	}
}
