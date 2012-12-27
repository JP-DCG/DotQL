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

ModuleMember =
	TypeMember | EnumMember	| ConstMember | VarMember

TypeMember :=
	Name : QualifiedIdentifier ":" "typedef" Type : TypeDeclaration

EnumMember :=
	Name : QualifiedIdentifier ":" "enum" "{" Values : QualifiedIdentifier* "}"

ConstMember :=
	Name : QualifiedIdentifier ":" "const" Expression : Expression

VarMember :=
	Name : QualifiedIdentifier ":" Type: TypeDeclaration

VarDeclaration :=
	"var" Name : QualifiedIdentifier [ ":" Type : TypeDeclaration ] [ ":=" Initializer : Expression )

Assignment :=
	"set" Target : PathExpression ":=" Source : Expression

TypeDeclaration =
	ListType | TupleType | SetType | FunctionType | IntervalType | ScalarType

ListType :=
	"[" Type : TypeDeclaration "]"

SetType :=
	"{" Type : TypeDeclaration "}"

TupleType :=
	"{" Members : [ TupleAttribute | TupleReference | TupleKey ]* "}"

TupleAttribute :=
	Name : QualifiedIdentifier ":" Type : TypeDeclaration

TupleReference :=
	"ref" Name : QualifiedIdentifier "(" SourceColumns : QualifiedIdentifier* ")" 
		Target : QualifiedIdentifier "(" TargetColumns : QualifiedIdentifier* ")"	

TupleKey :=
	"key" "(" Columns : [ QualifiedIdentifier ]* ")"

FunctionType :=
	"function" [ "<" TypeParameters : TypeDeclaration* ">" ] "(" Parameters : ( Name : QualifiedIdentifier ":" Type : TypeDeclaration )* ")" ":" ReturnType : TypeDeclaration

IntervalType :=
	"interval" Type : TypeDeclaration

ScalarType :=
	Name : QualifiedIdentifier

ClausedExpression :=
	ForClauses : [ "for" ForTerm ]*
	LetClauses : [ "let" LetTerm ]*
	WhereClause : [ "where" Expression : Expression ]
	OrderClause : [ "order" Dimensions : OrderDimension* ]
	"return" Expression : [ Expression ]

ForTerm :=
	Name : QualifiedIdentifier "in" Expression : Expression

LetTerm :=
	Name : QualifiedIdentifier ":=" Expression : Expression
	
OrderDimension :=
	Expression : Expression [ Direction : ( "asc" | "desc" ) ]

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
	UnaryExpression : ( Operator : ( "++" | "--" | "+" | "-" | "~" | "not" | "exists" ) Expression : PathExpression )

PathExpression 8 =
	IndexerExpression : ( Expression : PathExpression "[" IndexerExpression : [ Expression ] "]" )

PathExpression 9 =
	InvocationExpression : 
	( 
		Expression : PathExpression [ "<" TypeArguments : TypeDeclaration* ">" ]
			( "(" Arguments : [ Expression ]* ")" ) | ( "=>" Argument : Expression ) 
	)

PathExpression 10 =
	EmbedExpression : ( Expressions : PathExpression )^"#"

PathExpression 11 =
	DereferenceExpression : ( [ IsRooted : "." ] ( Expressions : PathExpression )^"." )

PathExpression =
	Factor :
	(
		(
			"(" Expression ")"
				| ListSelector
				| TupleSelector
				| SetSelector
				| FunctionSelector
				| IntervalSelector
				| IdentifierExpression
				| IntegerLiteral
				| DoubleLiteral
				| StringLiteral
				| BooleanLiteral : ( "true" | "false" )
				| NullLiteral : "null"
				| VoidLiteral : "void"
				| CaseExpression
		) [ "as" AsType : TypeDeclaration ]
	)

ListSelector :=
	"[" Items : [ Expression ]* "]"

TupleSelector :=
	"{" Members : ( TupleAttributeSelector | TupleReference | TupleKey )* "}"

TupleAttributeSelector :=
	Name : QualifiedIdentifier ":" Value : Expression

SetSelector :=
	"{" Items : [ Expression ]* "}"

FunctionSelector :=
	"function" [ "<" TypeParameters : TypeDeclaration* ">" ] "(" Parameters : ( Name : QualifiedIdentifier ":" Type : TypeDeclaration )* ")" ":" ReturnType : TypeDeclaration
		Expression : ClausedExpression

IntervalSelector :=
	Begin : Expression ".." End : Expression

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

