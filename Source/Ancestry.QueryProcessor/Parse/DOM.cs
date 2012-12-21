using System;
using System.Linq;
using System.Collections.Generic;

namespace Ancestry.QueryProcessor.Parse
{
    /*
		DOM Hierarchy ->
		
			Language.Statement
				|- Language.Expression
				|	|- Language.CallExpression
				|	|	|- AggregateCallExpression
				|	|- FieldExpression
				|	|	|- InsertFieldExpression
				|	|	|- UpdateFieldExpression
				|	|	|- OrderFieldExpression
				|	|	|- QualifiedFieldExpression
				|	|- ColumnExpression
				|	|- CastExpression
				|	|- TableExpression
				|	|- TableSpecifier
				|	|- SelectExpression
				|	|- TableOperatorExpression
				|	|- QueryExpression
				|	|- ValuesExpression
				|	|- ConstraintValueExpression
				|	|- ConstraintRecordValueExpression
				|	|- QueryParameterExpression
				|- Clause
				|	|- SelectClause
				|	|- JoinClause
				|	|- FromClause
				|	|	|- AlgebraicFromClause
				|	|	|- CalculusFromClause
				|	|- FilterClause
				|	|	|- WhereClause
				|	|	|- HavingClause
				|	|- GroupClause
				|	|- OrderClause
				|	|- UpdateClause
				|	|- DeleteClause
				|	|- InsertClause
				|- SelectStatement
				|- InsertStatement				
				|- UpdateStatement
				|- DeleteStatement
				|- CreateTableStatement
				|- ColumnDefinition
				|- AlterTableStatement
				|- DropTableStatement
				|- CreateIndexStatement
				|- OrderColumnDefinition
				|- AlterIndexStatement
				|- DropIndexStatement
	*/

	public class Using
	{
		public Using() {}
		public Using(string name, string alias = null)
		{
			Name = name;
			Alias = alias;
		}

		public string Alias { get; set; }
		
		public string Name { get; set; }
	}

    public enum JoinType {Cross, Inner, Left, Right, Full};
    public enum AggregationType {None, Count, Sum, Min, Max, Avg};
    
    public class AggregateCallExpression : CallExpression
    {
		public AggregateCallExpression() : base(){}
		
		public bool IsDistinct { get; set; }
		
		public bool IsRowLevel { get; set; }
    }
    
    public class UserExpression : Expression
    {
		public UserExpression() : base() {}
		public UserExpression(string translationString, Expression[] arguments) : base()
		{
			_translationString = translationString;
			_expressions.AddRange(arguments);
		}
		
		private string _translationString = String.Empty;
		public string TranslationString
		{
			get { return _translationString; }
			set { _translationString = value == null ? String.Empty : value; }
		}
        
        protected List<Expression> _expressions = new List<Expression>();
        public List<Expression> Expressions { get { return _expressions; } }
	}
    
    public class QueryParameterExpression : Expression
    {
		public QueryParameterExpression() : base(){}
		public QueryParameterExpression(string parameterName) : base()
		{
			ParameterName = parameterName;
		}
		
        protected string _parameterName = String.Empty;
        public string ParameterName
        {
            get { return _parameterName; }
            set { _parameterName = value; }
        }
    }
    
    public abstract class FieldExpression : Expression
    {
		public FieldExpression() : base(){}
		public FieldExpression(string fieldName) : base()
		{
			FieldName = fieldName;
		}
		
        protected string _fieldName = String.Empty;
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }
    }
    
    public class InsertFieldExpression : FieldExpression
    {	
		public InsertFieldExpression(string fieldName) : base(fieldName){}
    }
    
    public class UpdateFieldExpression : FieldExpression
    {
        public Expression Expression { get; set; }
    }
    
    public class OrderFieldExpression : QualifiedFieldExpression
    {
        public OrderFieldExpression() : base()
        {
            Ascending = true;
        }
        
        public OrderFieldExpression(string fieldName, string tableAlias, bool ascending) : base(fieldName, tableAlias)
        {
			Ascending = ascending;
        }
        
        public bool Ascending { get; set; }
    }

    public class QualifiedFieldExpression : FieldExpression
    {
		public QualifiedFieldExpression() : base(){}
		public QualifiedFieldExpression(string fieldName) : base(fieldName){}
		public QualifiedFieldExpression(string fieldName, string tableAlias) : base(fieldName)
		{
			TableAlias = tableAlias;
		}
		
        protected string _tableAlias = String.Empty;
        public string TableAlias
        {
            get { return _tableAlias; }
            set { _tableAlias = value; }
        }
    }
    
    public class ColumnExpression : Expression
    {
		public ColumnExpression() : base(){}
		public ColumnExpression(Expression expression) : base()
		{
			Expression = expression;
		}
		
		public ColumnExpression(Expression expression, string columnAlias) : base()
		{
			Expression = expression;
			ColumnAlias = columnAlias;
		}

        protected string _columnAlias = String.Empty;
        public string ColumnAlias
        {
            get { return _columnAlias; }
            set { _columnAlias = value; }
        }
        
        public Expression Expression { get; set; }
    }
    
    public class CastExpression : Expression
    {
		public CastExpression() : base(){}
		public CastExpression(Expression expression, string domainName) : base()
		{
			Expression = expression;
			_domainName = domainName;
		}

        public Expression Expression { get; set; }
		
		protected string _domainName = String.Empty;
		public string DomainName
		{
			get { return _domainName; }
			set { _domainName = value; }
		}
    }
    
    public abstract class Clause : Statement{}
    
    public class ColumnExpressions : List<ColumnExpression>
    {
        public ColumnExpressions() : base(){}
        
        public ColumnExpression this[string columnAlias]
        {
			get { return this[IndexOf(columnAlias)]; }
			set { base[IndexOf(columnAlias)] = value; }
		}
		
		public int IndexOf(string columnAlias)
		{
			return this.FindIndex(e => String.Equals(e.ColumnAlias, columnAlias));
		}
		
		public bool Contains(string columnAlias)
		{
			return IndexOf(columnAlias) >= 0;
		}
    }
    
    public class SelectClause : Clause
    {
        public SelectClause() : base()
        {
            _columns = new ColumnExpressions();
        }

        public bool Distinct { get; set; }
        
        public bool NonProject { get; set; }
        
        protected ColumnExpressions _columns;
        public ColumnExpressions Columns { get { return _columns; } }
    }
    
    public class TableExpression : Expression
    {
		public TableExpression() : base(){}
		public TableExpression(string tableName) : base()
		{
			TableName = tableName;
		}

		public TableExpression(string tableSchema, string tableName) : base()
		{
			TableSchema = tableSchema;
			TableName = tableName;
		}

		protected string _tableSchema = String.Empty;
		public virtual string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}

        protected string _tableName = String.Empty;
        public virtual string TableName
        {
            get { return _tableName; }
            set { _tableName = value == null ? String.Empty : value; }
        }
    }
    
    public class JoinClause : Clause
    {
        public JoinClause() : base()
        {
            JoinType = JoinType.Inner;
        }
        
        public JoinClause(TableSpecifier tableSpecifier, JoinType joinType, Expression joinExpression)
        {
			TableSpecifier = tableSpecifier;
			JoinType = joinType;
			JoinExpression = joinExpression;
        }

        public TableSpecifier TableSpecifier { get; set; }
 
        public JoinType JoinType { get; set; }
        
        public Expression JoinExpression { get; set; }
    }
    
    public abstract class FromClause : Clause 
    {
		public abstract bool HasJoins();
    }
    
    public class TableSpecifier : Expression
    {
		public TableSpecifier() : base(){}
		public TableSpecifier(Expression expression)
		{
			TableExpression = expression;
		}
		
		public TableSpecifier(Expression expression, string alias)
		{
			TableExpression = expression;
			TableAlias = alias;
		}
		
        public Expression TableExpression { get; set; }
        
        public string TableAlias { get; set; }

		public string FindTableAlias(string tableName)
        {
            if 
                (
                    (TableExpression is TableExpression) && 
                    String.Equals(((TableExpression)TableExpression).TableName, tableName, StringComparison.OrdinalIgnoreCase)
                )
            {
                return TableAlias;
            }
			return string.Empty;
		}
    }
    
    public class CalculusFromClause : FromClause
    {
		public CalculusFromClause() : base(){}
		public CalculusFromClause(TableSpecifier tableSpecifier) : base()
		{
			_tableSpecifiers.Add(tableSpecifier);
		}
		
		public CalculusFromClause(TableSpecifier[] tableSpecifiers) : base()
		{
			_tableSpecifiers.AddRange(tableSpecifiers);
		}
		
		protected List<TableSpecifier> _tableSpecifiers = new List<TableSpecifier>();
		public List<TableSpecifier> TableSpecifiers { get { return _tableSpecifiers; } }
		
		public override bool HasJoins()
		{
			return _tableSpecifiers.Count > 1;
		}
    }
    
    public class AlgebraicFromClause : FromClause
    {    
        public AlgebraicFromClause() : base(){}
        public AlgebraicFromClause(TableSpecifier specifier) : base()
        {
			TableSpecifier = specifier;
        }
        
        public TableSpecifier TableSpecifier { get; set; }

        protected internal JoinClause _parentJoin;
        public JoinClause ParentJoin { get { return _parentJoin; } }

        protected List<JoinClause> _joins = new List<JoinClause>();
        public List<JoinClause> Joins { get { return _joins; } }
        
		public override bool HasJoins()
		{
			return _joins.Count > 0;
		}
        
        public string FindTableAlias(string tableName)
        {
            string result = TableSpecifier.FindTableAlias(tableName);
			if (result != string.Empty)
				return result;
            foreach (JoinClause join in _joins)
            {
                result = join.TableSpecifier.FindTableAlias(tableName);
                if (result != string.Empty)
                    return result;
            }
            return string.Empty;
        }
    }
    
    public class FilterClause : Clause
    {
		public FilterClause() : base(){}
		public FilterClause(Expression expression) : base()
		{
			Expression = expression;
		}
		
        public Expression Expression { get; set; }
    }
    
    public class WhereClause : FilterClause
    {
		public WhereClause() : base(){}
		public WhereClause(Expression expression) : base(expression){}
    }
    
    public class HavingClause : FilterClause
    {
		public HavingClause() : base(){}
		public HavingClause(Expression expression) : base(expression){}
    }
    
    public class GroupClause : Clause
    {
        public GroupClause() : base()
        {
            _columns = new List<Expression>();
        }
        
        protected List<Expression> _columns;
        public List<Expression> Columns { get { return _columns; } }
    }
    
    public class OrderClause : Clause
    {
		public OrderClause() : base()
        {
            _columns = new List<OrderFieldExpression>();
        }

        protected List<OrderFieldExpression> _columns;
        public List<OrderFieldExpression> Columns { get { return _columns; } }
    }
    
    public class SelectExpression : Expression
    {        
        public SelectClause SelectClause { get; set; }
        
        public FromClause FromClause { get; set; }
        
        public WhereClause WhereClause { get; set; }

        public GroupClause GroupClause { get; set; }

        public HavingClause HavingClause { get; set; }
    }
    
    public enum TableOperator { Union, Intersect, Difference }
    
    public class TableOperatorExpression : Expression
    {
		public TableOperatorExpression() : base(){}
		public TableOperatorExpression(TableOperator operatorValue, SelectExpression selectExpression) : base()
		{
			TableOperator = operatorValue;
			SelectExpression = selectExpression;
		}
		
		public TableOperatorExpression(TableOperator operatorValue, bool distinct, SelectExpression selectExpression) : base()
		{
			TableOperator = operatorValue;
			Distinct = distinct;
			SelectExpression = selectExpression;
		}
		
		public TableOperator TableOperator { get; set; }

        public bool Distinct { get; set; }

        public SelectExpression SelectExpression { get; set; }
    }
    
    public class QueryExpression : Expression
    {        
        public QueryExpression() : base()
        {
            _tableOperators = new List<TableOperatorExpression>();
        }
        
        public SelectExpression SelectExpression { get; set; }
        
        protected List<TableOperatorExpression> _tableOperators;
        public List<TableOperatorExpression> TableOperators { get { return _tableOperators; } }

		// Indicates whether the given query expression could safely be extended with another table operator expression of the given table operator        
        public bool IsCompatibleWith(TableOperator tableOperator)
        {
			foreach (var tableOperatorExpression in _tableOperators)
				if (tableOperatorExpression.TableOperator != tableOperator)
					return false;
			return true;
        }
    }

	public class TableValuedExpression : Expression
	{
		public QueryExpression QueryExpression { get; set; }
	}

    public class SelectStatement : Statement
    {        
        public QueryExpression QueryExpression { get; set; }

        public OrderClause OrderClause { get; set; }
    }
    
    public class InsertClause : Clause
    {        
        public InsertClause() : base()
        {
            _columns = new List<InsertFieldExpression>();
        }
        
        protected List<InsertFieldExpression> _columns;
        public List<InsertFieldExpression> Columns { get { return _columns; } }

        public TableExpression TableExpression { get; set; }
    }
    
    public class ValuesExpression : Expression
    {
        public ValuesExpression() : base()
        {
            _expressions = new List<Expression>();
        }
 
        protected List<Expression> _expressions;
        public List<Expression> Expressions { get { return _expressions; } }
    }
    
    public class InsertStatement : Statement
    {        
        public InsertClause InsertClause { get; set; }

        public Expression Values { get; set; }
    }

    public class UpdateClause : Clause
    {        
        public UpdateClause() : base()
        {
            _columns = new List<UpdateFieldExpression>();
        }
        
        protected List<UpdateFieldExpression> _columns;
        public List<UpdateFieldExpression> Columns { get { return _columns; } }
        
        protected internal string _tableAlias = String.Empty;
        public string TableAlias { get { return _tableAlias; } }

        public TableExpression TableExpression { get; set; }
    }
    
    public class UpdateStatement : Statement
    {
        public UpdateClause UpdateClause { get; set; }

        public FromClause FromClause { get; set; }

        public WhereClause WhereClause { get; set; }
    }
    
    public class DeleteClause : Clause
    {        
        protected internal string _tableAlias = String.Empty;
        public string TableAlias { get { return _tableAlias; } }

        public TableExpression TableExpression { get; set; }
    }

    public class DeleteStatement : Statement
    {
        public DeleteClause DeleteClause { get; set; }
        
        public FromClause FromClause { get; set; }
        
        public WhereClause WhereClause { get; set; }
    }
    
    public class ConstraintValueExpression : Expression
	{}
    
    public class ConstraintRecordValueExpression : Expression
    {
        protected string _columnName = String.Empty;
        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }
    }
    
    public class CreateTableStatement : Statement
    {
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
		
		protected List<ColumnDefinition> _columns = new List<ColumnDefinition>();
		public List<ColumnDefinition> Columns { get { return _columns; } }
    }
    
    public class AlterTableStatement : Statement
    {
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
		
		protected List<ColumnDefinition> _addColumns = new List<ColumnDefinition>();
		public List<ColumnDefinition> AddColumns { get { return _addColumns; } }
		
		protected List<AlterColumnDefinition> _alterColumns = new List<AlterColumnDefinition>();
		public List<AlterColumnDefinition> AlterColumns { get { return _alterColumns; } }
		
		protected List<DropColumnDefinition> _dropColumns = new List<DropColumnDefinition>();
		public List<DropColumnDefinition> DropColumns { get { return _dropColumns; } }
    }
    
    public class DropTableStatement : Statement
    {
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}
		
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
    }
    
    public class CreateIndexStatement : Statement
    {
		protected string _indexSchema = String.Empty;
		public string IndexSchema
		{
			get { return _indexSchema; }
			set { _indexSchema = value == null ? String.Empty : value; }
		}
		
		protected string _indexName = String.Empty;
		public string IndexName
		{
			get { return _indexName; }
			set { _indexName = value == null ? String.Empty : value; }
		}
		
		public bool IsUnique { get; set; }

		public bool IsClustered { get; set; }

		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}

		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}

		protected List<OrderColumnDefinition> _columns = new List<OrderColumnDefinition>();
		public List<OrderColumnDefinition> Columns { get { return _columns; } }
    }
    
    public class DropIndexStatement : Statement
    {
		protected string _indexSchema = String.Empty;
		public string IndexSchema
		{
			get { return _indexSchema; }
			set { _indexSchema = value == null ? String.Empty : value; }
		}
		
		protected string _indexName = String.Empty;
		public string IndexName
		{
			get { return _indexName; }
			set { _indexName = value == null ? String.Empty : value; }
		}
    }
    
    public class ColumnDefinition : Statement
    {
		public ColumnDefinition() : base(){}
		public ColumnDefinition(string columnName, string domainName) : base()
		{
			ColumnName = columnName;
			DomainName = domainName;
		}
		
		public ColumnDefinition(string columnName, string domainName, bool isNullable) : base()
		{
			ColumnName = columnName;
			DomainName = domainName;
			IsNullable = isNullable;
		}
		
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		// DomainName
		protected string _domainName = String.Empty;
		public string DomainName
		{
			get { return _domainName; }
			set { _domainName = value == null ? String.Empty : value; }
		}

		public bool IsNullable { get; set; }
    }

    public class AlterColumnDefinition : Statement
    {
		public AlterColumnDefinition() : base() {}
		public AlterColumnDefinition(string columnName) : base()
		{
			_columnName = columnName;
		}
		
		public AlterColumnDefinition(string columnName, bool isNullable)
		{
			_columnName = columnName;
			AlterNullable = true;
			IsNullable = isNullable;
		}

		public AlterColumnDefinition(string columnName, string domainName)
		{
			_columnName = columnName;
			DomainName = domainName;
		}
		
		public AlterColumnDefinition(string columnName, string domainName, bool isNullable)
		{
			_columnName = columnName;
			DomainName = domainName;
			AlterNullable = true;
			IsNullable = isNullable;
		}

		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		/// <summary>Null domain name indicates no change to the domain of the column</summary>
		public string DomainName { get; set; }
		
		public bool AlterNullable { get; set; }

		public bool IsNullable { get; set; }
    }
    
    public class DropColumnDefinition : Statement
    {
		public DropColumnDefinition() : base(){}
		public DropColumnDefinition(string columnName) : base()
		{
			ColumnName = columnName;
		}
		
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}
    }

    public class OrderColumnDefinition : Statement
    {
		protected string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		public bool Ascending { get; set; }
    }

    public class Batch : Statement
    {
		private List<Statement> _statements = new List<Statement>();
		public List<Statement> Statements { get { return _statements; } }
    }
}

