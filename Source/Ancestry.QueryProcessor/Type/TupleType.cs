using Ancestry.QueryProcessor.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ancestry.QueryProcessor.Type
{
	public class TupleType : BaseType
	{
		private Dictionary<Name, BaseType> _attributes = new Dictionary<Name, BaseType>();
		public Dictionary<Name, BaseType> Attributes { get { return _attributes; } }

		private Dictionary<Name, TupleReference> _references = new Dictionary<Name,TupleReference>();
		public Dictionary<Name, TupleReference> References { get { return _references; } }

		private Runtime.Set<TupleKey> _keys = new Runtime.Set<TupleKey>();
		public Runtime.Set<TupleKey> Keys { get { return _keys; } }

		public override int GetHashCode()
		{
			var running = 0;
			foreach (var a in _attributes)
				running ^= a.Key.GetHashCode() * 83 + a.Value.GetHashCode();
			foreach (var r in _references)
				running ^= r.Key.GetHashCode() * 83 + r.Value.GetHashCode();
			foreach (var k in _keys)
				running ^= k.GetHashCode();
			return running;
		}

		public override bool Equals(object obj)
		{
			if (obj is TupleType)
				return (TupleType)obj == this;
			else
				return base.Equals(obj);
		}

		public static bool operator ==(TupleType left, TupleType right)
		{
			return Object.ReferenceEquals(left, right)
				|| 
				(
					left.Attributes.Equivalent(right.Attributes)
						&& left.References.Equivalent(right.References)
						&& left.Keys == right.Keys
				);
		}

		public static bool operator !=(TupleType left, TupleType right)
		{
			return !(left == right);
		}

		public IEnumerable<Name> GetKeyAttributes()
		{
			if (Keys.Count == 0)
			{
				foreach (var a in Attributes)
					yield return a.Key;
			}
			else
			{
				// Return distinct set of all attributes from all keys
				var attributes = new HashSet<Name>();
				foreach (var k in Keys)
					foreach (var an in k.AttributeNames)
					{
						if (attributes.Add(an))
							yield return an;
					}
			}
		}

		public override ExpressionContext CompileBinaryExpression(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Dereference: 
					return CompileDereference(method, compiler, frame, left, expression, typeHint);

				default: 
					return base.CompileBinaryExpression(method, compiler, frame, left, expression, typeHint);
			}
		}

		protected override ExpressionContext DefaultBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
					return base.DefaultBinaryOperator(method, compiler, left, right, expression);

				//// TODO: Tuple union
				//case Parse.Operator.BitwiseOr:

				default: throw NotSupported(expression);
			}
		}

		private ExpressionContext CompileDereference(MethodContext method, Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			var local = compiler.AddFrame(frame, expression);

			left = compiler.MaterializeRepository(method, left);
			var valueVariable = method.DeclareLocal(expression.Right, left.Type.GetNative(compiler.Emitter), "value");
			method.IL.Emit(OpCodes.Stloc, valueVariable);

			var native = left.Type.GetNative(compiler.Emitter);
			foreach (var a in ((TupleType)left.Type).Attributes)
			{
				var field = native.GetField(a.Key.ToString(), BindingFlags.Public | BindingFlags.Instance);
				local.Add(expression, a.Key, field);
				compiler.WritersBySymbol.Add(field, m => { m.IL.Emit(OpCodes.Ldfld, valueVariable); return new ExpressionContext(a.Value); });
			}
			return compiler.CompileExpression(method, local, expression.Right, typeHint);
		}

		public override System.Type GetNative(Emitter emitter)
		{
			return emitter.FindOrCreateNativeFromTupleType(this);
		}
	}
}
