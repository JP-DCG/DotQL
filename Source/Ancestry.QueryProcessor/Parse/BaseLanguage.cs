using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

namespace Ancestry.QueryProcessor.Parse
{
    /*
		Class Hierarchy ->
		
			Statement
				|- Expression
				|	|- UnaryExpression
				|	|- BinaryExpression
				|	|- ValueExpression
				|	|- ParameterExpression
				|	|- IdentifierExpression
				|   |- ListExpression
				|	|- CallExpression
				|	|- IfExpression
				|	|- BetweenExpression
				|	|- CaseExpression
				|	|- CaseItemExpression
				|	|- CaseElseExpression
				|- Block
				|	|- DelimitedBlock
				|- IfStatement
    */
    
    public class LineInfo
    {
		public LineInfo() : this(-1, -1, -1, -1) { }
		public LineInfo(int line, int linePos, int endLine, int endLinePos)
		{
			Line = line;
			LinePos = linePos;
			EndLine = endLine;
			EndLinePos = endLinePos;
		}
		
		public LineInfo(LineInfo lineInfo)
		{
			SetFromLineInfo(lineInfo == null ? StartingOffset : lineInfo);
		}
		
		public int Line;
		public int LinePos;
		public int EndLine;
		public int EndLinePos;
		
		public void SetFromLineInfo(LineInfo lineInfo)
		{
			Line = lineInfo.Line;
			LinePos = lineInfo.LinePos;
			EndLine = lineInfo.EndLine;
			EndLinePos = lineInfo.EndLinePos;
		}

		public static LineInfo Empty = new LineInfo();		
		public static LineInfo StartingOffset = new LineInfo(0, 0, 0, 0);
		public static LineInfo StartingLine = new LineInfo(1, 1, 1, 1);
    }
    
	public abstract class Statement : Object 
	{
		public Statement() : base() {}
		public Statement(Lexer lexer) : base()
		{
			SetPosition(lexer);
		}
		
		// The line and position of the starting token for this element in the syntax tree
		private LineInfo _lineInfo;
		public LineInfo LineInfo { get { return _lineInfo; } }
		
		public int Line
		{
			get { return _lineInfo == null ? -1 : _lineInfo.Line; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.Line = value;
			}
		}
		
		public int LinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.LinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.LinePos = value;
			}
		}
		
		public int EndLine
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLine; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLine = value;
			}
		}
		
		public int EndLinePos
		{
			get { return _lineInfo == null ? -1 : _lineInfo.EndLinePos; }
			set
			{
				if (_lineInfo == null)
					_lineInfo = new LineInfo();
				_lineInfo.EndLinePos = value;
			}
		}
		
		public void SetPosition(Lexer lexer)
		{
			Line = lexer[0, false].Line;
			LinePos = lexer[0, false].LinePos;
		}
		
		public void SetEndPosition(Lexer lexer)
		{
			EndLine = lexer[0, false].Line;
			LexerToken token = lexer[0, false];
			EndLinePos = token.LinePos + (token.Token == null ? 0 : token.Token.Length);
		}
		
		public void SetLineInfo(LineInfo lineInfo)
		{
			if (lineInfo != null)
			{
				Line = lineInfo.Line;
				LinePos = lineInfo.LinePos;
				EndLine = lineInfo.EndLine;
				EndLinePos = lineInfo.EndLinePos;
			}
		}
	}
	
	public class EmptyStatement : Statement {}
    
    public abstract class Expression : Statement {}

	public enum Modifier : byte { In, Var, Out, Const }
	
	public class ParameterExpression : Expression
	{
		public ParameterExpression() : base(){}
		public ParameterExpression(Modifier modifier, Expression expression)
		{
			Modifier = modifier;
			Expression = expression;
		}
		
		public Modifier Modifier { get; set; }
		
		public Expression Expression { get; set; }
	}
	
    public class UnaryExpression : Expression
    {
        public UnaryExpression() : base(){}
        public UnaryExpression(Operator op, Expression expression) : base()
        {
            Operator = op;
            Expression = expression;
        }
        
        public Operator Operator { get; set; }
        
        public Expression Expression { get; set; }
    }
    
    public class BinaryExpression : Expression
    {
        public BinaryExpression() : base(){}
        public BinaryExpression(Expression leftExpression, Operator op, Expression rightExpression) : base()
        {
            LeftExpression = leftExpression;
			Operator = op;
            RightExpression = rightExpression;
        }
    
        public Operator Operator { get; set; }
        
        public Expression LeftExpression { get; set; }

        public Expression RightExpression { get; set; }
    }
    
    public class QualifierExpression : Expression
    {
        public QualifierExpression() : base(){}
        public QualifierExpression(Expression leftExpression, Expression rightExpression) : base()
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }
    
        public Expression LeftExpression { get; set; }

        public Expression RightExpression { get; set; }
    }
    
    public class ValueExpression : Expression
    {
        public ValueExpression() : base(){}
        public ValueExpression(object tempValue) : base()
        {
            Value = tempValue;
            if (tempValue is decimal)
				Token = TokenType.Decimal;
			else if (tempValue is long)
				Token = TokenType.Integer;
			else if (tempValue is int)
			{
				Value = Convert.ToInt64(tempValue);
				Token = TokenType.Integer;
			}
			else if (tempValue is double)
				Token = TokenType.Float;
			else if (tempValue is bool)
				Token = TokenType.Boolean;
			else if (tempValue is string)
				Token = TokenType.String;
        }
        
        public ValueExpression(object tempValue, TokenType token) : base()
        {
			Value = tempValue;
			Token = token;
        }
        
        public object Value { get; set; }
        
        public TokenType Token { get; set; }
    }
    
    public class IdentifierExpression : Expression
    {
        public IdentifierExpression() : base(){}
        public IdentifierExpression(string identifier) : base()
        {
            Identifier = identifier;
        }
        
        protected string _identifier = String.Empty;
        public string Identifier
        {
            get { return _identifier; }
            set { _identifier = value; }
        }
    }
    
    public class ListExpression : Expression
    {
        public ListExpression() : base()
        {
        }
        
        public ListExpression(Expression[] expressions) : base()
        {
			_expressions.AddRange(expressions);
        }
        
        protected List<Expression> _expressions = new List<Expression>();
        public List<Expression> Expressions { get { return _expressions; } }
    }
    
    public class IndexerExpression : Expression
    {
        public Expression Expression { get; set; }
        
        public Expression Indexer
        {
			get { return _expressions[0]; }
			set 
			{
				if (_expressions.Count == 0)
					_expressions.Add(value);
				else
					_expressions[0] = value;
			}
        }
        
        protected List<Expression> _expressions = new List<Expression>();
        public List<Expression> Expressions { get { return _expressions; } }
    }
   
    public class CallExpression : Expression
    {
        public CallExpression() : base()
        {
        }
        
        public CallExpression(string identifier, Expression[] arguments) : base()
        {
            _expressions.AddRange(arguments);
            Identifier = identifier;
        }
        
        protected string _identifier = String.Empty;
        public string Identifier
        {
            get { return _identifier; }
            set { _identifier = value; }
        }
        
        protected List<Expression> _expressions = new List<Expression>();
        public List<Expression> Expressions { get { return _expressions; } }
    }
   
    public class IfExpression : Expression
    {
        public IfExpression() : base(){}
        public IfExpression(Expression expression, Expression trueExpression, Expression falseExpression) : base()
        {
            Expression = expression;
            TrueExpression = trueExpression;
            FalseExpression = falseExpression;
        }
        
        public Expression Expression { get; set; }

        public Expression TrueExpression { get; set; }
        
        public Expression FalseExpression { get; set; }
    }
    
    public class CaseExpression : Expression
    {		
        public CaseExpression() : base(){}
        public CaseExpression(CaseItemExpression[] items, Expression elseExpression) : base()
        {
			CaseItems.AddRange(items);
			ElseExpression = elseExpression;
        }
        
        public CaseExpression(Expression expression, CaseItemExpression[] items, Expression elseExpression)
        {
			Expression = expression;
			CaseItems.AddRange(items);
			ElseExpression = elseExpression;
        }
        
        public Expression Expression { get; set; }
        
        protected List<CaseItemExpression> _caseItems = new List<CaseItemExpression>();
        public List<CaseItemExpression> CaseItems { get { return _caseItems; } }
        
        public Expression ElseExpression { get; set; }
    }
    
    public class CaseItemExpression : Expression
    {
        public CaseItemExpression() : base(){}
        public CaseItemExpression(Expression whenExpression, Expression thenExpression) : base()
        {
            WhenExpression = whenExpression;
            ThenExpression = thenExpression;
        }
    
        public Expression WhenExpression { get; set; }

        public Expression ThenExpression { get; set; }
    }
    
    public class CaseElseExpression : Expression
    {
		public CaseElseExpression() : base(){}
		public CaseElseExpression(Expression expression) : base()
		{
			Expression = expression;
		}
		
		public Expression Expression { get; set; }
    }
    
    public class BetweenExpression : Expression
    {
		public BetweenExpression() : base(){}
		public BetweenExpression(Expression expression, Expression lowerExpression, Expression upperExpression) : base()
		{
			Expression = expression;
			LowerExpression = lowerExpression;
			UpperExpression = upperExpression;
		}
		
		public Expression Expression { get; set; }
		
		public Expression LowerExpression { get; set; }
		
		public Expression UpperExpression { get; set; }
    }
    
	public class IfStatement : Statement
    {
        public IfStatement() : base(){}
        public IfStatement(Expression expression, Statement trueStatement, Statement falseStatement)
        {
            Expression = expression;
            TrueStatement = trueStatement;
            FalseStatement = falseStatement;
        }

        public Expression Expression { get; set; }
        
        public Statement TrueStatement { get; set; }

        public Statement FalseStatement { get; set; }
    }
    
    public class CaseStatement : Statement
    {		
        public CaseStatement() : base(){}
        public CaseStatement(CaseItemStatement[] items, Statement elseStatement) : base()
        {
			CaseItems.AddRange(items);												
			ElseStatement = elseStatement;
        }
        
        public CaseStatement(Expression expression, CaseItemStatement[] items, Statement elseStatement)
        {
			Expression = expression;
			CaseItems.AddRange(items);
			ElseStatement = elseStatement;
        }
        
        public Expression Expression { get; set; }
        
        protected List<CaseItemStatement> _caseItems = new List<CaseItemStatement>();
        public List<CaseItemStatement> CaseItems { get { return _caseItems; } }
        
        public Statement ElseStatement { get; set; }
    }
    
    public class CaseItemStatement : Statement
    {
        public CaseItemStatement() : base(){}
        public CaseItemStatement(Expression whenExpression, Statement thenStatement) : base()
        {
            WhenExpression = whenExpression;
            ThenStatement = thenStatement;
        }
    
        public Expression WhenExpression { get; set; }

        public Statement ThenStatement { get; set; }
    }
}
