/* DotQL v1.0 Grammar - G4 syntax + (double quote whitespace elimination, :=, + group suffix for capture) */


Script :=
	Usings : [ UsingClause ]*
	Modules : [ ModuleDeclaration ]*
	VarDeclarations : [ VarDeclaration ]*
	Assignments : [ Assignment ]*
	Expression : [ ClausedExpression ]

UsingClause :=
	"using" [ Alias: QualifiedIdentifier "=" ] Target : QualifiedIdentifier

ModuleDeclaration :=
	"module" Name : QualifiedIdentifier "{" Members : [ ModuleMember ]* "}"

VarDeclaration :=
	"var" Name : QualifiedIdentifier ":" Expression : Expression

Assignment :=
	"set" Target : PathExpression ":=" Source : Expression

ClausedExpression :=
	ForClauses : [ "for" ForTerm ]*
	LetClauses : [ "let" LetTerm ]*
	WhereClause : [ "where" Expression : Expression ]
	OrderClause : [ "order" Dimensions : OrderDimension^"," ]
	"return" Expression	: Expression

ForTerm :=
	Name : QualifiedIdentifier "in" Expression : Expression

LetTerm :=
	Name : QualifiedIdentifier ":=" Expression : Expression
	
OrderDimension :=
	Expression : Expression IsAscending : [ "asc" as '1' | "desc" as '0' ]

Expression =
	ClausedExpression | PathExpression

PathExpression 0 =
    LogicalBinaryExpression : ( Expressions : PathExpression )^( Operators : ( "in" | "or" | "xor" | "like" | "matches" ) )

PathExpression 1 =
	LogicalAndExpression : ( Expressions : PathExpression )^"and"

PathExpression 2 =
	BitwiseBinaryExpression : ( Expressions : PathExpression )^( Operators : ( "^" | "&" | "|" | "<<" | ">>" ) )

PathExpression 3 =
	ComparisonExpression : ( Expressions : PathExpression )^( Operators : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) )

PathExpression 4 =
	AdditiveExpression : ( Expressions : PathExpression )^( Operators : ( "+" | "-" ) )

PathExpression 5 =
	MultiplicativeExpression : ( Expressions : PathExpression )^( Operators : ( "*" | "/" | "%" ) )

PathExpression 6 =
	ExponentExpression : ( Expressions : PathExpression )^"**"

PathExpression 7 =
	UnaryExpression : ( Expressions : PathExpression )^( Operators : ( "+" | "-" | "~" | "not" | "exists" ) )

PathExpression 8 =
	IndexerExpression : Expression : PathExpression "[" IndexerExpression : Expression "]"

PathExpression 9 =
	DereferenceExpression : [ IsRooted : "." ] ( Expressions : PathExpression )^"."

PathExpression 10 =
	"(" Expression ")"
		| ListSelector
		| TupleSelector
		| SetSelector
		| IntervalSelector
	    | FunctionInvocation
		| IdentifierExpression
		| IntegerLiteral
		| DoubleLiteral
		| StringLiteral
		| BooleanLiteral : ( "true" | "false" )
		| NullLiteral : "null"
		| VoidLiteral : "void"
		| CaseExpression

ListSelector :=
	"[" Items : Expression* "]"

TupleSelector :=
	"{" Attributes : ( Name : QualifiedIdentifier ":" Value : Expression )* "}"

SetSelector :=
	"{" Items : Expression* "}"

IntervalSelector :=
	Begin : Expression ".." End : Expression

FunctionInvocation :=
	Name : QualifiedIdentifier ( "(" ArgumentExpressions : Expression* ")" | "=>" ArgumentExpression : Expression )

IdentifierExpression := 
	Name : QualifiedIdentifier

IntegerLiteral :=
	_ (digit* | '0x' { '0'..'9', 'a'..'f', 'A'..'F' }*)+ _

DoubleLiteral :=
	_ (digit* '.' digit* [( 'e' | 'E' ) [ '+' | '-' ] digit*])+ _

StringLiteral :=
	_ '''' ( [ '''''' as '''' | {?} &! '''' ]* )+ '''' _

CaseExpression :=
    "case" [ TestExpression : Expression ]
        Items : ( "when" WhenExpression : Expression "then" ThenExpression : Expression )*
        else ElseExpression : Expression
    end

QualifiedIdentifier =
	Identifier [ '\' Identifier ]*

Identifier =
	_ ( letter | '_' [ letter | digit | '_' ]* )+ _

// Whitespace
_ =
	[{ ' ', #9..#13 } | LineComment | BlockComment ]*
		
BlockComment =
	'/*' [ BlockComment | {?} &! '*/' ]* '*/'
		
LineComment =
	'//' [ {! #10, #13 } ]*

letter =
	{ 'a'..'z', 'A'..'Z' }
		
digit =
	'0'..'9'

