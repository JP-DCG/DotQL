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
		public ScalarType(System.Type native)
		{
			Native = native;
		}

		public System.Type Native { get; set; }

		public override System.Type GetNative(Emitter emitter)
		{
			return Native;
		}

		public override int GetHashCode()
		{
			return Native.GetHashCode();
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
				|| (left.Native == right.Native);
		}

		public static bool operator !=(ScalarType left, ScalarType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Native.Name;
		}

		public override ExpressionContext CompileRestrictExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.RestrictExpression expression, BaseType typeHint)
		{
			left = compiler.MaterializeRepository(method, left);
			var local = compiler.AddFrame(frame, expression);
			var alreadyOptional = left.Type is OptionalType;
			var memberNative = left.Type.GetNative(compiler.Emitter);
			var resultType = alreadyOptional ? left.Type : new OptionalType(left.Type);
			var resultNative = resultType.GetNative(compiler.Emitter);

			var nullLabel = method.IL.DefineLabel();
			var endLabel = method.IL.DefineLabel();

			// Register value argument
			var valueLocal = method.DeclareLocal(expression, memberNative, Parse.ReservedWords.Value);
			local.Add(expression.Condition, Name.FromComponents(Parse.ReservedWords.Value), valueLocal);
			compiler.WritersBySymbol.Add(valueLocal, m => { m.IL.Emit(OpCodes.Ldloc, valueLocal); return new ExpressionContext(left.Type); });
			method.IL.Emit(OpCodes.Stloc, valueLocal);

			var condition = compiler.CompileExpression(method, local, expression.Condition, SystemTypes.Boolean);

			method.IL.Emit(OpCodes.Brfalse, nullLabel);

			// Passed condition
			if (!alreadyOptional)
			{
				var optionalLocal = method.DeclareLocal(expression, resultNative, Parse.ReservedWords.Value);
				method.IL.Emit(OpCodes.Ldloca, optionalLocal);
				method.IL.Emit(OpCodes.Ldloc, valueLocal);
				method.IL.Emit(OpCodes.Call, resultNative.GetConstructor(new System.Type[] { left.Type.GetNative(compiler.Emitter) }));
				method.IL.Emit(OpCodes.Ldloc, optionalLocal);
			}
			else
				method.IL.Emit(OpCodes.Ldloc, valueLocal);
			method.IL.Emit(OpCodes.Br, endLabel);

			// Failed condition
			method.IL.MarkLabel(nullLabel);
			if (!alreadyOptional)
			{
				var optionalLocal = method.DeclareLocal(expression, resultNative, Parse.ReservedWords.Value);
				method.IL.Emit(OpCodes.Ldloca, optionalLocal);
				method.IL.Emit(OpCodes.Initobj, resultNative);
				method.IL.Emit(OpCodes.Ldloc, optionalLocal);
			}
			else
			{
				method.IL.Emit(OpCodes.Ldloca, valueLocal);
				method.IL.Emit(OpCodes.Initobj, resultNative);
				method.IL.Emit(OpCodes.Ldloc, valueLocal);
			}

			method.IL.MarkLabel(endLabel);

			return new ExpressionContext(resultType);
		}
	}
}
