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
	"set" Target : Expression ":=" Source : Expression

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

Expression 1 =
	OfExpression : ( Expression : Expression "of" Type : TypeDeclaration )

Expression 2 =
    LogicalBinaryExpression : ( Expressions : Expression )^( Operators : ( "in" | "or" | "xor" | "like" | "matches" | "?" ) )

Expression 3 =
	LogicalAndExpression : ( Expressions : Expression )^"and"

Expression 4 =
	BitwiseBinaryExpression : ( Expressions : Expression )^( Operators : ( "^" | "&" | "|" | "<<" | ">>" ) )

Expression 5 =
	ComparisonExpression : ( Expressions : Expression )^( Operators : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) )

Expression 6 =
	AdditiveExpression : ( Expressions : Expression )^( Operators : ( "+" | "-" ) )

Expression 7 =
	MultiplicativeExpression : ( Expressions : Expression )^( Operators : ( "*" | "/" | "%" | ".." ) )

Expression 8 :=
	IntervalSelector : ( Begin : Expression ".." End : Expression )

Expression 9 =
	ExponentExpression : ( Expressions : Expression )^"**"

Expression 10 =
	UnaryExpression : ( Operator : ( "++" | "--" | "-" | "~" | "not" | "exists" | "??" ) Expression : Expression )

Expression 11 =
	DereferenceExpression : ( Expressions : Expression )^"."

Expression 12 =
	IndexerExpression : ( Expression : Expression "[" Indexer : [ Expression ] "]" )

Expression 13 =
	CallExpression : 
	( 
		Expression : Expression [ "<" TypeArguments : TypeDeclaration* ">" ]
			( "(" Arguments : [ Expression ]* ")" ) | ( "=>" Argument : Expression ) 
	)

Expression =
	"(" Expression ")"
		| ListSelector
		| TupleSelector
		| SetSelector
		| FunctionSelector
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
		| TryExpression
		| ClausedExpression

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

TryExpression :=
	"try" TryExpression : Expression "catch" CatchExpression : Expression

ClausedExpression :=
	ForClauses : [ ForClause ]*
	LetClauses : [ LetClause ]*
	[ "where" WhereClause : Expression ]
	[ "order" "(" OrderDimensions : OrderDimension* ")" ]
	"return" Expression : Expression

ForClause :=
	"for" Name : QualifiedIdentifier "in" Expression : Expression

LetClause :=
	"let" Name : QualifiedIdentifier ":=" Expression : Expression
	
OrderDimension :=
	Expression : Expression [ Direction : ( "asc" | "desc" ) ]

PascalString =
	'''' ( [ '''''' as '''' | {?} &! '''' ]* )+ ''''

QualifiedIdentifier =
	IsRooted : [ '\' ] Items : Identifier^'\'

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

