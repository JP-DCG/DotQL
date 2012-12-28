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

		private List<Assignment> _assignments = new List<Assignment>();
		public List<Assignment> Assignments { get { return _assignments; } }

		public ClausedExpression Expression { get; set; }
	}

	public struct QualifiedIdentifier
	{
		public bool IsRooted;
		public string[] Components;
	}


	public class Using : Statement
	{
		public QualifiedIdentifier Alias { get; set; }
		
		public QualifiedIdentifier Target { get; set; }
	}

	public class ModuleDeclaration : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		private List<ModuleMember> _members = new List<ModuleMember>();
		public List<ModuleMember> Members { get { return _members; } }
	}

	public abstract class ModuleMember : Statement
	{
		public QualifiedIdentifier Name { get; set; }
	}

	public class TypeMember : ModuleMember
	{
		public TypeDeclaration Type { get; set; }
	}

	public class EnumMember : ModuleMember
	{
		private List<QualifiedIdentifier> _values = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> Values { get { return _values; } }
	}

	public class ConstMember : ModuleMember
	{
		public Expression Expression { get; set; }
	}

	public class VarMember : ModuleMember
	{
		public TypeDeclaration Type { get; set; }
	}

	public class VarDeclaration : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }
		
		public Expression Initializer { get; set; }		
	}

	public class Assignment : Statement
	{
		public PathExpression Target { get; set; }

		public Expression Source { get; set; }
	}

	public abstract class TypeDeclaration : Statement { }

	public class ListType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }
	}

	public class SetType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }
	}

	public class TupleType : TypeDeclaration
	{
		private List<TupleAttribute> _attributes = new List<TupleAttribute>();
		public List<TupleAttribute> Attributes { get { return _attributes; } }

		private List<TupleReference> _references = new List<TupleReference>();
		public List<TupleReference> References { get { return _references; } }

		private List<TupleKey> _keys = new List<TupleKey>();
		public List<TupleKey> Keys { get { return _keys; } }
	}

	public class TupleAttribute : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }
	}

	public class TupleReference : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		private List<QualifiedIdentifier> _sourceAttributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> SourceAttributeNames { get { return _sourceAttributeNames; } }

		public QualifiedIdentifier Target { get; set; }

		private List<QualifiedIdentifier> _targetAttributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> TargetAttributeNames { get { return _targetAttributeNames; } }
	}

	public class TupleKey : Statement
	{
		private List<QualifiedIdentifier> _attributeNames = new List<QualifiedIdentifier>();
		public List<QualifiedIdentifier> AttributeNames { get { return _attributeNames; } }
	}

	public class FunctionType : TypeDeclaration
	{
		private List<TypeDeclaration> _typeParameters = new List<TypeDeclaration>();
		public List<TypeDeclaration> TypeParameters { get { return _typeParameters; } }

		private List<FunctionParameter> _parameters = new List<FunctionParameter>();
		public List<FunctionParameter> Parameters { get { return _parameters; } }

		public TypeDeclaration ReturnType { get; set; }
	}

	public class FunctionParameter : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public TypeDeclaration Type { get; set; }
	}

	public class IntervalType : TypeDeclaration
	{
		public TypeDeclaration Type { get; set; }
	}

	public class NamedType : TypeDeclaration
	{
		public QualifiedIdentifier Name { get; set; }
	}

	public abstract class Expression : Statement { }

    public class ClausedExpression : Expression
    {        
		private List<ForClause> _forClauses = new List<ForClause>();
        public List<ForClause> ForClauses { get { return _forClauses; } }
        
		private List<LetClause> _letClauses = new List<LetClause>();
        public List<LetClause> LetClauses { get { return _letClauses; } }
        
        public Expression WhereClause { get; set; }

        public List<OrderDimension> OrderDimensions { get; set; }

        public Expression Expression { get; set; }
    }

	public class ForClause : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Expression { get; set; }
	}

	public class LetClause : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Expression { get; set; }
	}

	public class OrderDimension : Statement
	{
		public Expression Expression { get; set; }

		public bool Ascending { get; set; }
	}

	public abstract class PathExpression : Expression { }

	public class OfExpression : PathExpression
	{
		public Expression Expression { get; set; }

		public TypeDeclaration Type { get; set; }
	}

	public class BinaryExpression : PathExpression
	{
		private List<Expression> _expressions = new List<Expression>();
		public List<Expression> Expressions { get { return _expressions; } }

		private List<Operator> _operators = new List<Operator>();
		public List<Operator> Operators { get { return _operators; } }
	}

	public class UnaryExpression : PathExpression
	{
		public Expression Expression { get; set; }

		public Operator Operator { get; set; }
	}

	public class IndexerExpression : PathExpression
	{
		public Expression Expression { get; set; }

		public Expression Indexer { get; set; }
	}

	public class CallExpression : PathExpression
	{
		public Expression Expression { get; set; }

		private List<TypeDeclaration> _typeArguments = new List<TypeDeclaration>();
		public List<TypeDeclaration> TypeArguments { get { return _typeArguments; } }

		private List<Expression> _arguments = new List<Expression>();
		public List<Expression> Arguments { get { return _arguments; } }

		/// <summary> Single tuple argument (mutex with arguments). </summary>
		public Expression Argument { get; set; }
	}

	public class ListSelector : PathExpression
	{
		private List<Expression> _items = new List<Expression>();
		public List<Expression> Items { get { return _items; } }
	}

	public class TupleSelector : PathExpression
	{
		private List<AttributeSelector> _attributes = new List<AttributeSelector>();
		public List<AttributeSelector> Attributes { get { return _attributes; } }

		private List<TupleReference> _references = new List<TupleReference>();
		public List<TupleReference> References { get { return _references; } }

		private List<TupleKey> _keys = new List<TupleKey>();
		public List<TupleKey> Keys { get { return _keys; } }
	}

	public class AttributeSelector : Statement
	{
		public QualifiedIdentifier Name { get; set; }

		public Expression Value { get; set; }
	}

	public class SetSelector : PathExpression
	{
		private List<Expression> _items = new List<Expression>();
		public List<Expression> Items { get { return _items; } }
	}

	public class FunctionSelector : PathExpression
	{
		public FunctionType Type { get; set; }

		public ClausedExpression Expression { get; set; }
	}

	public class IntervalSelector : PathExpression
	{
		public Expression Begin { get; set; }

		public Expression End { get; set; }
	}

	public class IdentifierExpression : PathExpression
	{
		public QualifiedIdentifier Name { get; set; }
	}

	public class LiteralExpression : PathExpression
	{
		public string Value { get; set; }

		public TokenType TokenType { get; set; }
	}

	public class CaseExpression : PathExpression
	{
		public Expression TestExpression { get; set; }

		private List<CaseItem> _items = new List<CaseItem>();
		public List<CaseItem> Items { get { return _items; } }

		public Expression ElseExpression { get; set; }
	}

	public class CaseItem : Statement
	{
		public Expression WhenExpression { get; set; }

		public Expression ThenExpression { get; set; }
	}

	public class IfExpression : PathExpression
	{
		public Expression Expression { get; set; }

		public Expression ThenExpression { get; set; }

		public Expression ElseExpression { get; set; }
	}

	public class TryCatchExpression : PathExpression
	{
		public Expression TryExpression { get; set; }

		public Expression CatchExpression { get; set; }
	}
}

