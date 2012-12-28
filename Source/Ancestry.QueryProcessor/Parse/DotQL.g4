/* DotQL v1.0 Grammar - G4 syntax + (double quote whitespace elimination, :=, + group suffix for capture) */


Script :=
	Usings : [ UsingClause ]*
	Modules : [ ModuleDeclaration ]*
	Vars : [ VarDeclaration ]*
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
	ListType | TupleType | SetType | FunctionType | IntervalType | NamedType

ListType :=
	"[" Type : [ TypeDeclaration ] "]"

SetType :=
	"{" Type : [ TypeDeclaration ] "}"

TupleType :=
	"{" ":" | Members : ( TupleAttribute | TupleReference | TupleKey )* "}"

TupleAttribute :=
	Name : QualifiedIdentifier ":" Type : TypeDeclaration

TupleReference :=
	"ref" Name : QualifiedIdentifier "(" SourceAttributeNames : QualifiedIdentifier* ")" 
		Target : QualifiedIdentifier "(" TargetAttributeNames : QualifiedIdentifier* ")"	

TupleKey :=
	"key" "(" AttributeNames : [ QualifiedIdentifier ]* ")"

FunctionType :=
	"function" [ "<" TypeParameters : TypeDeclaration* ">" ] "(" Parameters : [ FunctionParameter ]* ")" ":" ReturnType : TypeDeclaration

FunctionParameter :=
	Name : QualifiedIdentifier ":" Type : TypeDeclaration

IntervalType :=
	"interval" Type : TypeDeclaration

NamedType :=
	Name : QualifiedIdentifier

ClausedExpression :=
	[ "for" ForClauses : ForClause ]*
	[ "let" LetClauses : LetClause ]*
	[ "where" WhereClause : Expression ]
	[ "order" OrderClause : OrderDimension* ]
	"return" Expression : Expression

ForClause :=
	Name : QualifiedIdentifier "in" Expression : Expression

LetClause :=
	Name : QualifiedIdentifier ":=" Expression : Expression
	
OrderDimension :=
	Expression : Expression [ Direction : ( "asc" | "desc" ) ]

Expression =
	ClausedExpression | PathExpression

PathExpression 10 =
	OfExpression : ( Expression : PathExpression "of" Type : TypeDeclaration ) 

PathExpression 20 =
    LogicalBinaryExpression : ( Expressions : PathExpression )^( Operators : ( "in" | "or" | "xor" | "like" | "matches" ) )

PathExpression 30 =
	LogicalAndExpression : ( Expressions : PathExpression )^"and"

PathExpression 40 =
	BitwiseBinaryExpression : ( Expressions : PathExpression )^( Operators : ( "^" | "&" | "|" | "<<" | ">>" ) )

PathExpression 50 =
	ComparisonExpression : ( Expressions : PathExpression )^( Operators : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) )

PathExpression 60 =
	AdditiveExpression : ( Expressions : PathExpression )^( Operators : ( "+" | "-" ) )

PathExpression 70 =
	MultiplicativeExpression : ( Expressions : PathExpression )^( Operators : ( "*" | "/" | "%" ) )

PathExpression 80 =
	ExponentExpression : ( Expressions : PathExpression )^"**"

PathExpression 90 =
	UnaryExpression : ( Operator : ( "++" | "--" | "+" | "-" | "~" | "not" | "exists" ) Expression : PathExpression )

PathExpression 100 =
	DereferenceExpression : ( Expressions : PathExpression )^"."

PathExpression 110 =
	IndexerExpression : ( Expression : PathExpression "[" Indexer : [ Expression ] "]" )

PathExpression 120 =
	InvocationExpression : 
	( 
		Expression : PathExpression [ "<" TypeArguments : TypeDeclaration* ">" ]
			( "(" Arguments : [ Expression ]* ")" ) | ( "=>" Argument : Expression ) 
	)

PathExpression =
	"(" Expression ")"
		| ListSelector
		| TupleSelector
		| SetSelector
		| FunctionSelector
		| IntervalSelector
		| IdentifierExpression
		| IntegerLiteral
		| DoubleLiteral
		| CharacterLiteral
		| StringLiteral
		| BooleanLiteral : ( "true" | "false" )
		| NullLiteral : "null"
		| VoidLiteral : "void"
		| CaseExpression
		| IfExpression

ListSelector :=
	"[" Items : [ Expression ]* "]"

TupleSelector :=
	"{" ":" | Members : ( TupleAttributeSelector | TupleReference | TupleKey )* "}"

TupleAttributeSelector :=
	Name : QualifiedIdentifier ":" Value : Expression

SetSelector :=
	"{" Items : [ Expression ]* "}"

FunctionSelector :=
	Type : FunctionType Expression : ClausedExpression

IntervalSelector :=
	Begin : Expression ".." End : Expression

IdentifierExpression := 
	Name : QualifiedIdentifier

IntegerLiteral :=
	_ (digit* | '0x' { '0'..'9', 'a'..'f', 'A'..'F' }*)+ _

DoubleLiteral :=
	_ (digit* '.' digit* [( 'e' | 'E' ) [ '+' | '-' ] digit*])+ _

CharacterLiteral :=
	_ PascalString+ 'c' _

StringLiteral :=
	_ ( PascalSegments : ( [ '#' LeadingChars : digit* ]* PascalString [ '#' TrailingChars : digit* ]* )* ) 
		| ( '"' ( [ '\\' as '\' | '\"' as '"' | '\r' as #13 | '\n' as #10 | '\t' as #9 | {?} &! '"' ]* )+ '"' ) _

CaseExpression :=
    "case" [ TestExpression : Expression ]
        Items : ( "when" WhenExpression : Expression "then" ThenExpression : Expression )*
        "else" ElseExpression : Expression
    "end"

IfExpression :=
	"if" TestExpression : Expression
        "then" ThenExpression : Expression
        "else" ElseExpression : Expression 

PascalString =
	'''' ( [ '''''' as '''' | {?} &! '''' ]* )+ ''''

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

