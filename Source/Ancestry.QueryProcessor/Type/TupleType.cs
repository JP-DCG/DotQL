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
					!Object.ReferenceEquals(right, null)
						&& !Object.ReferenceEquals(left, null)
						&& left.GetType() == right.GetType()
						&& left.Attributes.Equivalent(right.Attributes)
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

		public override ExpressionContext CompileBinaryExpression(Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Dereference: 
					return CompileDereference(compiler, frame, left, expression, typeHint);

				default: 
					return base.CompileBinaryExpression(compiler, frame, left, expression, typeHint);
			}
		}

		protected override void EmitBinaryOperator(MethodContext method, Compiler compiler, ExpressionContext left, ExpressionContext right, Parse.BinaryExpression expression)
		{
			switch (expression.Operator)
			{
				case Parse.Operator.Equal:
				case Parse.Operator.NotEqual:
					base.EmitBinaryOperator(method, compiler, left, right, expression);
					break;

				//// TODO: Tuple union
				//case Parse.Operator.BitwiseOr:

				default: throw NotSupported(expression);
			}
		}

		private ExpressionContext CompileDereference(Compiler compiler, Frame frame, ExpressionContext left, Parse.BinaryExpression expression, Type.BaseType typeHint)
		{
			var local = compiler.AddFrame(frame, expression);
			var native = left.ActualNative(compiler.Emitter);

			LocalBuilder valueVariable = null;

			// Create symbol for each tuple member
			foreach (var a in ((TupleType)left.Type).Attributes)
			{
				var field = native.GetField(a.Key.ToString(), BindingFlags.Public | BindingFlags.Instance);
				local.Add(expression, a.Key, field);
				compiler.ContextsBySymbol.Add
				(
					field, 
					new ExpressionContext
					(
						null,
						a.Value,
						Characteristic.Default,
						m => 
						{ 
							m.IL.Emit(OpCodes.Ldloc, valueVariable);
							m.IL.Emit(OpCodes.Ldfld, field); 
						}
					)
				);
			}

			var right = compiler.CompileExpression(local, expression.Right, typeHint);
			
			return
				new ExpressionContext
				(
					expression,
					right.Type,
					Compiler.MergeCharacteristics(left.Characteristics, right.Characteristics),
					m =>
					{
						m.IL.BeginScope();
						
						left.EmitGet(m);
						valueVariable = m.DeclareLocal(expression.Right, native, "value");
						m.IL.Emit(OpCodes.Stloc, valueVariable);
						
						right.EmitGet(m);

						m.IL.EndScope();
					}
				);
		}

		public override System.Type GetNative(Emitter emitter)
		{
			return emitter.FindOrCreateNativeFromTupleType(this);
		}

		public override Parse.Expression BuildDefault()
		{
			return
				new Parse.TupleSelector
				{
					Attributes =
					(
						from a in Attributes 
						select new Parse.AttributeSelector { Name = a.Key.ToID(), Value = a.Value.BuildDefault() }
					).ToList(),
					Keys = 
					(
						from k in Keys
						select new Parse.TupleKey { AttributeNames = (from n in k.AttributeNames select n.ToID()).ToList() }
					).ToList(),
					References =
					(
						from r in References
						select new Parse.TupleReference 
						{ 
							Name = r.Key.ToID(), 
							SourceAttributeNames = (from n in r.Value.SourceAttributeNames select n.ToID()).ToList(),
							Target = r.Value.Target.ToID(),
							TargetAttributeNames = (from n in r.Value.TargetAttributeNames select n.ToID()).ToList()
						}
					).ToList()
				};
		}

		public override Parse.TypeDeclaration BuildDOM()
		{
			return
				new Parse.TupleType
				{
					Attributes =
					(
						from a in Attributes
						select new Parse.TupleAttribute { Name = a.Key.ToID(), Type = a.Value.BuildDOM() }
					).ToList(),
					Keys =
					(
						from k in Keys
						select new Parse.TupleKey { AttributeNames = (from n in k.AttributeNames select n.ToID()).ToList() }
					).ToList(),
					References =
					(
						from r in References
						select new Parse.TupleReference
						{
							Name = r.Key.ToID(),
							SourceAttributeNames = (from n in r.Value.SourceAttributeNames select n.ToID()).ToList(),
							Target = r.Value.Target.ToID(),
							TargetAttributeNames = (from n in r.Value.TargetAttributeNames select n.ToID()).ToList()
						}
					).ToList()
				};
		}
	}
}
