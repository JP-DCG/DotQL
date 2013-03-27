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
				||
				(
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType()
						&& left.Native == right.Native
				);
		}

		public static bool operator !=(ScalarType left, ScalarType right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return Native.Name;
		}

		// Restriction
		public override ExpressionContext CompileExtractExpression(Compiler compiler, Frame frame, ExpressionContext left, Parse.ExtractExpression expression, BaseType typeHint)
		{
			var local = compiler.AddFrame(frame, expression);
			var alreadyOptional = left.Type is OptionalType;
			var memberNative = left.Type.GetNative(compiler.Emitter);
			var resultType = alreadyOptional ? left.Type : new OptionalType(left.Type);
			var resultNative = resultType.GetNative(compiler.Emitter);

			// Register value symbol
			LocalBuilder valueLocal = null;
			var localSymbol = new Object();
			local.Add(expression.Condition, Name.FromComponents(Parse.ReservedWords.Value), localSymbol);
			compiler.ContextsBySymbol.Add
			(
				localSymbol, 
				new ExpressionContext
				(
					null,
					left.Type,
					left.Characteristics,
					m => { m.IL.Emit(OpCodes.Ldloc, valueLocal); }
				)
			);

			var condition = compiler.CompileExpression(local, expression.Condition, SystemTypes.Boolean);
			if (!(condition.Type is BooleanType))
				throw new CompilerException(expression.Condition, CompilerException.Codes.IncorrectType, condition.Type, "Boolean");

			return
				new ExpressionContext
				(
					expression,
					resultType,
					Compiler.MergeCharacteristics(left.Characteristics, condition.Characteristics),
					m =>
					{
						var nullLabel = m.IL.DefineLabel();
						var endLabel = m.IL.DefineLabel();

						// Register value argument
						valueLocal = m.DeclareLocal(expression, memberNative, Parse.ReservedWords.Value);
						left.EmitGet(m);
						m.IL.Emit(OpCodes.Stloc, valueLocal);

						condition.EmitGet(m);
						m.IL.Emit(OpCodes.Brfalse, nullLabel);

						// Passed condition
						if (!alreadyOptional && memberNative.IsValueType)
						{
							var optionalLocal = m.DeclareLocal(expression, resultNative, Parse.ReservedWords.Value);
							m.IL.Emit(OpCodes.Ldloca, optionalLocal);
							m.IL.Emit(OpCodes.Ldloc, valueLocal);
							m.IL.Emit(OpCodes.Call, resultNative.GetConstructor(new System.Type[] { left.Type.GetNative(compiler.Emitter) }));
							m.IL.Emit(OpCodes.Ldloc, optionalLocal);
						}
						else
							m.IL.Emit(OpCodes.Ldloc, valueLocal);
						m.IL.Emit(OpCodes.Br, endLabel);

						// Failed condition
						m.IL.MarkLabel(nullLabel);
						if (!alreadyOptional && memberNative.IsValueType)
						{
							var optionalLocal = m.DeclareLocal(expression, resultNative, Parse.ReservedWords.Value);
							m.IL.Emit(OpCodes.Ldloca, optionalLocal);
							m.IL.Emit(OpCodes.Initobj, resultNative);
							m.IL.Emit(OpCodes.Ldloc, optionalLocal);
						}
						else if (alreadyOptional && memberNative.IsValueType)
						{
							m.IL.Emit(OpCodes.Ldloca, valueLocal);
							m.IL.Emit(OpCodes.Initobj, resultNative);
							m.IL.Emit(OpCodes.Ldloc, valueLocal);
						}
						else
							m.IL.Emit(OpCodes.Ldnull);

						m.IL.MarkLabel(endLabel);
					}
				);
		}


		public override Parse.Expression BuildDefault()
		{
			return new Parse.LiteralExpression { Value = ReflectionUtility.GetDefaultValue(Native) };
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return new Parse.NamedType { Target = Parse.ID.FromComponents("System", Native.Name) };
		}
	}
}
