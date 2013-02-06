using System;
using System.Linq;
using System.Collections.Generic;

namespace Ancestry.QueryProcessor.Parse
{
    public class Script : Statement
    {
		private List<Using> _usings = new List<Using>();
		public List<Using> Usings { get { return _usings; } }

		private List<ModuleDeclaration> _modules = new List<ModuleDeclaration>();
		public List<ModuleDeclaration> Modules { get { return _modules; } }

		private List<VarDeclaration> _vars = new List<VarDeclaration>();
		public List<VarDeclaration> Vars { get { return _vars; } }

		private List<ClausedAssignment> _assignments = new List<ClausedAssignment>();
		public List<ClausedAssignment> Assignments { get { return _assignments; } }

		public ClausedExpression Expression { get; set; }

		public override string ToString()
		{
			return 
				String.Join
				(
					"\r\n",
					(from u in Usings select u.ToString())
						.Union(from m in Modules select m.ToString())
						.Union(from v in Vars select v.ToString())
						.Union(from a in Assignments select a.ToString())
				)
					+ "\r\n" + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var u in Usings)
				yield return u;
			foreach (var m in Modules)
				yield return m;
			foreach (var v in Vars)
				yield return v;
			foreach (var a in Assignments)
				yield return a;
			if (Expression != null)
				yield return Expression;
		}
	}

	public class Using : Statement
	{
		public QualifiedIdentifier Alias { get; set; }
		
		public QualifiedIdentifier Target { get; set; }

		public Version Version { get; set; }

		public override string ToString()
		{
			return "using " + (Alias == null ? "" : Alias + " := ") + Target + " " + Version;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Alias;
			yield return Target;
		}
	}

	public class ModuleDeclaration : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public Version Version { get; set; }

		private List<ModuleMember> _members = new List<ModuleMember>();
		public List<ModuleMember> Members { get { return _members; } }

		public override string ToString()
		{
			return "module " + Name + " " + Version + "{\r\n\t" 
				+ String.Join("\r\n\t", (from m in Members select m.ToString()).ToArray()) + "\r\n}";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			foreach (var m in Members)
				yield return m;
		}
	}

	public interface ISymbol
	{
		QualifiedIdentifier Name { get; set; }
	}

	public abstract class ModuleMember : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
		}
	}

	public class TypeMember : ModuleMember
	{
		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return Name + " : " + Type.ToString();
		}

		public override IEnumerable<Statement> GetChildren()
		{
			base.GetChildren();
			yield return Type;
		}
	}

	public class EnumMember : ModuleMember
	{
		private List<QualifiedIdentifier> _values = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> Values { get { return _values; } }

		public override string ToString()
		{
			return Name + " : " + String.Join(" ", from v in Values select v.ToString());
		}

		public override IEnumerable<Statement> GetChildren()
		{
			base.GetChildren();
			foreach (var v in Values)
				yield return v;
		}
	}

	public class ConstMember : ModuleMember
	{
		public Expression Expression { get; set; }

		public override string ToString()
		{
			return Name + " : " + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			base.GetChildren();
			yield return Expression;
		}
	}

	public class VarMember : ModuleMember
	{
		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return Name + " : " + Type;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			base.GetChildren();
			yield return Type;
		}
	}

	public class VarDeclaration : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }
		
		public Expression Initializer { get; set; }

		public override string ToString()
		{
			return "var " + Name + (Type != null ? " : " + Type : "") + (Initializer != null ? " := " + Initializer : "");
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Type;
			yield return Initializer;
		}
	}

	public class ClausedAssignment : Statement
	{
		private List<ForClause> _forClauses = new List<ForClause>();
		public List<ForClause> ForClauses { get { return _forClauses; } }

		private List<LetClause> _letClauses = new List<LetClause>();
		public List<LetClause> LetClauses { get { return _letClauses; } }

		public Expression WhereClause { get; set; }

		private List<Assignment> _assignments = new List<Assignment>();
		public List<Assignment> Assignments { get { return _assignments; } }

		public override string ToString()
		{
			return
				String.Join
				(
					"\r\n",
					(from f in ForClauses select f.ToString())
						.Union(from l in LetClauses select l.ToString())
						.Union(WhereClause != null ? new[] { "where" + WhereClause.ToString() } : new string[] { })
						.Union(from a in Assignments select a.ToString())
				);
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var f in ForClauses)
				yield return f;
			foreach (var l in LetClauses)
				yield return l;
			if (WhereClause != null)
				yield return WhereClause;
			foreach (var a in Assignments)
				yield return a;
		}
	}

	public class Assignment : Statement
	{
		public Expression Target { get; set; }

		public Expression Source { get; set; }

		public override string ToString()
		{
			return "set " + Target + " := " + Source;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Target;
			yield return Source;
		}
	}

	public abstract class TypeDeclaration : Statement { }

	public class OptionalType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }

		public bool IsRequired { get; set; }

		public override string ToString()
		{
			return Type.ToString() + (IsRequired ? "!" : "?");
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Type;
		}
	}

	public class NaryType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Type;
		}
	}

	public class ListType : NaryType
	{
		public override string ToString()
		{
			return "[" + Type + "]";
		}
	}

	public class SetType : NaryType
	{
		public override string ToString()
		{
			return "{ " + Type + " }";
		}
	}

	public class TupleType : TypeDeclaration
	{
		private List<TupleAttribute> _attributes = new List<TupleAttribute>();
		public List<TupleAttribute> Attributes { get { return _attributes; } }

		private List<TupleReference> _references = new List<TupleReference>();
		public List<TupleReference> References { get { return _references; } }

		private List<TupleKey> _keys = new List<TupleKey>();
		public List<TupleKey> Keys { get { return _keys; } }

		public override string ToString()
		{
			return "{ " 
				+ 
				(
					Attributes.Count == 0
						? ":"
						: String.Join
						(
							"  ", 
							(from a in Attributes select a.ToString())
							.Union(from r in References select r.ToString())
							.Union(from k in Keys select k.ToString())
						)
				) + " }";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var a in Attributes)
				yield return a;
			foreach (var r in References)
				yield return r;
			foreach (var k in Keys)
				yield return k;
		}
	}

	public class TupleAttribute : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return Name + " : " + Type;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Type;
		}
	}

	public class TupleReference : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		private List<QualifiedIdentifier> _sourceAttributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> SourceAttributeNames { get { return _sourceAttributeNames; } }

		public QualifiedIdentifier Target { get; set; }

		private List<QualifiedIdentifier> _targetAttributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> TargetAttributeNames { get { return _targetAttributeNames; } }

		public override string ToString()
		{
			return "ref " + Name + "(" + String.Join(" ", from n in SourceAttributeNames select n.ToString()) 
				+ ") " + Target + "(" + String.Join(" ", from n in TargetAttributeNames select n.ToString()) + ")";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			foreach (var sa in SourceAttributeNames)
				yield return sa;
			yield return Target;
			foreach (var ta in TargetAttributeNames)
				yield return ta;
		}
	}

	public class TupleKey : Statement
	{
		private List<QualifiedIdentifier> _attributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> AttributeNames { get { return _attributeNames; } }

		public override string ToString()
		{
			return "key (" + String.Join(" ", from a in AttributeNames select a.ToString()) + ")";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var an in AttributeNames)
				yield return an;
		}
	}

	public class FunctionType : TypeDeclaration
	{
		private List<TypeDeclaration> _typeParameters = new List<TypeDeclaration>();
		public List<TypeDeclaration> TypeParameters { get { return _typeParameters; } }

		private List<FunctionParameter> _parameters = new List<FunctionParameter>();
		public List<FunctionParameter> Parameters { get { return _parameters; } }

		public TypeDeclaration ReturnType { get; set; }

		public override string ToString()
		{
			return "(" + String.Join(" ", from p in Parameters select p.ToString()) 
				+ ") => "
				+ (TypeParameters.Count > 0 ? "<" + String.Join(" ", from tp in TypeParameters select tp.ToString()) + "> " : "")
				+ ReturnType.ToString();
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var tp in TypeParameters)
				yield return tp;
			foreach (var p in Parameters)
				yield return p;
			yield return ReturnType;
		}
	}

	public class FunctionParameter : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return Name + " : " + Type;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Type;
		}
	}

	public class IntervalType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return "interval " + Type;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Type;
		}
	}

	public class NamedType : TypeDeclaration
	{
		public QualifiedIdentifier Target { get; set; }

		public override string ToString()
		{
			return Target.ToString();
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Target;
		}
	}

	public class TypeOf : TypeDeclaration
	{
		public Expression Expression { get; set; }

		public override string ToString()
		{
			return "typeof " + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
		}
	}


	public abstract class Expression : Statement { }

    public class ClausedExpression : Expression
    {        
		private List<ForClause> _forClauses = new List<ForClause>();
        public List<ForClause> ForClauses { get { return _forClauses; } }
        
		private List<LetClause> _letClauses = new List<LetClause>();
        public List<LetClause> LetClauses { get { return _letClauses; } }
        
        public Expression WhereClause { get; set; }

		private List<OrderDimension> _orderDimensions = new List<OrderDimension>();
        public List<OrderDimension> OrderDimensions { get { return _orderDimensions; } }

        public Expression Expression { get; set; }

		public override string ToString()
		{
			return 
				String.Join
				(
					"\r\n",
					(from f in ForClauses select f.ToString())
						.Union(from l in LetClauses select l.ToString())
						.Union(WhereClause != null ? new[] { "where" + WhereClause.ToString() } : new string[] {})
						.Union(OrderDimensions.Count > 0 ? new[] { "order (" + String.Join(" ", from o in OrderDimensions select o.ToString()) + ")" } : new string[] {})
						.Union(Expression != null ? new[] { "return " + Expression } : new string[] {})
				);
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var f in ForClauses)
				yield return f;
			foreach (var l in LetClauses)
				yield return l;
			if (WhereClause != null)
				yield return WhereClause;
			foreach (var od in OrderDimensions)
				yield return od;
			yield return Expression;
		}
    }

	public class ForClause : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Expression { get; set; }

		public override string ToString()
		{
			return "for " + Name + " in " + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Expression;
		}
	}

	public class LetClause : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Expression { get; set; }

		public override string ToString()
		{
			return "let " + Name + " := " + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Expression;
		}
	}

	public class OrderDimension : Statement
	{
		public Expression Expression { get; set; }

		public bool Ascending { get; set; }

		public override string ToString()
		{
			return Expression + (Ascending ? "" : " desc");
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
		}
	}

	public class OfExpression : Expression
	{
		public Expression Expression { get; set; }

		public TypeDeclaration Type { get; set; }

		public override string ToString()
		{
			return Expression + " of " + Type;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
			yield return Type;
		}
	}

	public class BinaryExpression : Expression
	{
		public Expression Left { get; set; }

		public Expression Right { get; set; } 

		public Operator Operator { get; set; }

		public override string ToString()
		{
			return Left + " " + Operator.ToString() + " " + Right;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Left;
			yield return Right;
		}
	}

	public class UnaryExpression : Expression
	{
		public Expression Expression { get; set; }

		public Operator Operator { get; set; }

		public override string ToString()
		{
			return Operator.ToString() + " " + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
		}
	}

	public class IndexerExpression : Expression
	{
		public Expression Expression { get; set; }

		public Expression Indexer { get; set; }

		public override string ToString()
		{
			return Expression.ToString() + "[" + Indexer + "]";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
			yield return Indexer;
		}
	}

	public class CallExpression : Expression
	{
		public Expression Expression { get; set; }

		private List<TypeDeclaration> _typeArguments = new List<TypeDeclaration>();
		public List<TypeDeclaration> TypeArguments { get { return _typeArguments; } }

		private List<Expression> _arguments = new List<Expression>();
		public List<Expression> Arguments { get { return _arguments; } }

		/// <summary> Single tuple argument (mutex with arguments). </summary>
		public Expression Argument { get; set; }

		public override string ToString()
		{
			return 
				(
					Argument != null
						? Argument.ToString()
						: (Arguments.Count > 0 ? Arguments[0].ToString() : "{ : }")
				)
					+ (Argument != null ? "=>" : "->")
					+ Expression
					+ (TypeArguments.Count > 0 ? "<" + String.Join(" ", from ta in TypeArguments select ta.ToString()) + ">" : "")
					+ (Argument == null ? "(" + String.Join(" ", (from a in Arguments select a.ToString()).Skip(1)) + ")" : "");
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
			foreach (var ta in TypeArguments)
				yield return ta;
			foreach (var a in Arguments)
				yield return a;
			if (Argument != null)
				yield return Argument;
		}
	}

	public class RestrictExpression : Expression
	{
		public Expression Expression { get; set; }

		public Expression Condition { get; set; }

		public override string ToString()
		{
			return Expression.ToString() + "?(" + Condition + ")";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Expression;
			yield return Condition;
		}
	}

	public class ListSelector : Expression
	{
		private List<Expression> _items = new List<Expression>();
		public List<Expression> Items { get { return _items; } }

		public override string ToString()
		{
			return "[" + String.Join(" ", from i in Items select i.ToString()) + "]";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var i in Items)
				yield return i;
		}
	}

	public class TupleSelector : Expression
	{
		private List<AttributeSelector> _attributes = new List<AttributeSelector>();
		public List<AttributeSelector> Attributes { get { return _attributes; } }

		private List<TupleReference> _references = new List<TupleReference>();
		public List<TupleReference> References { get { return _references; } }

		private List<TupleKey> _keys = new List<TupleKey>();
		public List<TupleKey> Keys { get { return _keys; } }

		public override string ToString()
		{
			return "{ " 
				+ 
				(
					Attributes.Count == 0
						? ":"
						: String.Join
						(
							"  ", 
							(from a in Attributes select a.ToString())
							.Union(from r in References select r.ToString())
							.Union(from k in Keys select k.ToString())
						)
				) + " }";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var a in Attributes)
				yield return a;
			foreach (var r in References)
				yield return r;
			foreach (var k in Keys)
				yield return k;
		}
	}

	public class AttributeSelector : Statement, ISymbol
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Value { get; set; }

		public override string ToString()
		{
 			 return Name.ToString() + " : " + Value;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Name;
			yield return Value;
		}
	}

	public class SetSelector : Expression
	{
		private List<Expression> _items = new List<Expression>();
		public List<Expression> Items { get { return _items; } }

		public override string ToString()
		{
			return "{ " + String.Join(" ", from i in Items select i.ToString()) + " }";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var i in Items)
				yield return i;
		}
	}

	public class FunctionSelector : Expression
	{
		private List<FunctionParameter> _parameters = new List<FunctionParameter>();
		public List<FunctionParameter> Parameters { get { return _parameters; } }

		public ClausedExpression Expression { get; set; }

		public override string ToString()
		{
			return "(" + String.Join(" ", from p in Parameters select p.ToString()) + ")"
				+ " =>\r\n\t" + Expression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			foreach (var p in Parameters)
				yield return p;
			yield return Expression;
		}
	}

	public class IntervalSelector : Expression
	{
		public Expression Begin { get; set; }

		public Expression End { get; set; }

		public override string ToString()
		{
			return Begin.ToString() + ".." + End.ToString();
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Begin;
			yield return End;
		}
	}

	public class IdentifierExpression : Expression
	{
		public QualifiedIdentifier Target { get; set; }

		public override string ToString()
		{
			return Target.ToString();
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return Target;
		}
	}

	public class LiteralExpression : Expression
	{
		public object Value { get; set; }

		public override string ToString()
		{
			switch (Value.GetType().Name)
			{
				case "String": return "'" + ((String)Value).Replace("'", "''") + "'";
				case "Char": return "'" + ((Char)Value).ToString().Replace("'", "''") + "'c";
				// TODO: Complete this
				default: return Value.ToString();
			}
		}
	}

	public class CaseExpression : Expression
	{
		public Expression TestExpression { get; set; }

		public bool IsStrict { get; set; }

		private List<CaseItem> _items = new List<CaseItem>();
		public List<CaseItem> Items { get { return _items; } }

		public Expression ElseExpression { get; set; }

		public override string ToString()
		{
			return "case" + (TestExpression != null ? " " + TestExpression : "")
				+ String.Concat(from i in Items select "\r\n\t" + i.ToString())
				+ "\r\n\t" + ElseExpression
				+ "\r\nend";
		}

		public override IEnumerable<Statement> GetChildren()
		{
			if (TestExpression != null)
				yield return TestExpression;
			foreach (var i in Items)
				yield return i;
			yield return ElseExpression;
		}
	}

	public class CaseItem : Statement
	{
		public Expression WhenExpression { get; set; }

		public Expression ThenExpression { get; set; }

		public override string ToString()
		{
			return "when " + WhenExpression + " then " + ThenExpression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return WhenExpression;
			yield return ThenExpression;
		}
	}

	public class IfExpression : Expression
	{
		public Expression TestExpression { get; set; }

		public Expression ThenExpression { get; set; }

		public Expression ElseExpression { get; set; }

		public override string ToString()
		{
			return "if " + TestExpression + " then " + ThenExpression + " else " + ElseExpression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return TestExpression;
			yield return ThenExpression;
			yield return ElseExpression;
		}
	}

	public class TryCatchExpression : Expression
	{
		public Expression TryExpression { get; set; }

		public Expression CatchExpression { get; set; }

		public override string ToString()
		{
			return "try " + TryExpression + " catch " + CatchExpression;
		}

		public override IEnumerable<Statement> GetChildren()
		{
			yield return TryExpression;
			yield return CatchExpression;
		}
	}

	public class QualifiedIdentifier : Statement
	{
		public bool IsRooted { get; set; }

		public string[] Components { get; set; }

		public override string ToString()
		{
			return (IsRooted ? "\\" : "") + String.Join("\\", Components);
		}

		public static QualifiedIdentifier FromComponents(params string[] components)
		{
			return new QualifiedIdentifier { Components = components };
		}
	}
}

