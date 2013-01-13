/* DotQL Grammar v1.0 */

grammar DotQL
	sensitive : true
	prefix : _
	suffix : _

Script :=
	Usings : [ UsingClause ]*
	Modules : [ ModuleDeclaration ]*
	Vars : [ VarDeclaration ]*
	Assignments : [ ClausedAssignment ]*
	Expression : [ ClausedExpression ]

UsingClause :=
	"using" [ Alias: QualifiedIdentifier ":=" ] Target : QualifiedIdentifier

ModuleDeclaration :=
	"module" Name : QualifiedIdentifier "{" Members : [ moduleMember ]* "}"

moduleMember =
	TypeMember | EnumMember	| ConstMember | VarMember

TypeMember :=
	Name : QualifiedIdentifier ":" "typedef" Type : typeDeclaration

EnumMember :=
	Name : QualifiedIdentifier ":" "enum" "{" Values : QualifiedIdentifier* "}"

ConstMember :=
	Name : QualifiedIdentifier ":" "const" Expression : expression

VarMember :=
	Name : QualifiedIdentifier ":" Type: typeDeclaration

VarDeclaration :=
	"var" Name : QualifiedIdentifier [ ":" Type : typeDeclaration ] [ ":=" Initializer : expression )

ClausedAssignment :=
	ForClauses : [ ForClause ]*
	LetClauses : [ LetClause ]*
	[ "where" WhereClause : expression ]
	Assignments : ("set" Target : expression ":=" Source : expression)*

typeDeclaration =
	ListType | TupleType | SetType | FunctionType | IntervalType | NamedType | TypeOf

ListType :=
	"[" Type : [ typeDeclaration ] "]" [ IsOptional : ( "?" | "!" ) ]

SetType :=
	"{" Type : [ typeDeclaration ] "}" [ IsOptional : ( "?" | "!" ) ]

TupleType :=
	"{" ":" | Members : ( TupleAttribute | TupleReference | TupleKey )* "}" [ IsOptional : ( "?" | "!" ) ]

TupleAttribute :=
	Name : QualifiedIdentifier ":" Type : typeDeclaration

TupleReference :=
	"ref" Name : QualifiedIdentifier "(" SourceAttributeNames : QualifiedIdentifier* ")" 
		Target : QualifiedIdentifier "(" TargetAttributeNames : QualifiedIdentifier* ")"	

TupleKey :=
	"key" "(" AttributeNames : [ QualifiedIdentifier ]* ")"

FunctionType :=
	functionParameters "=>" [ "<" TypeParameters : typeDeclaration* ">" ] ReturnType : typeDeclaration [ IsOptional : ( "?" | "!" ) ]

functionParameters =
	"(" Parameters : [ FunctionParameter ]* ")"

FunctionParameter :=
	Name : QualifiedIdentifier ":" Type : typeDeclaration

IntervalType :=
	"interval" Type : typeDeclaration [ IsOptional : ( "?" | "!" ) ]

NamedType :=
	Target : QualifiedIdentifier [ IsOptional : ( "?" | "!" ) ]

TypeOf :=
	"typeof" Expression : expression [ IsOptional : ( "?" | "!" ) ]

expression 1 =
	OfExpression : ( Expression : expression "of" Type : typeDeclaration )

expression 2 =
    LogicalBinaryExpression : ( Expressions : expression )^( Operators : ( "in" | "or" | "xor" | "like" | "matches" | "??" ) )

expression 3 =
	LogicalAndExpression : ( Expressions : expression )^"and"

expression 4 =
	BitwiseBinaryExpression : ( Expressions : expression )^( Operators : ( "^" | "&" | "|" | "<<" | ">>" ) )

expression 5 =
	ComparisonExpression : ( Expressions : expression )^( Operators : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) )

expression 6 =
	AdditiveExpression : ( Expressions : expression )^( Operators : ( "+" | "-" ) )

expression 7 =
	MultiplicativeExpression : ( Expressions : expression )^( Operators : ( "*" | "/" | "%" ) )

expression 8 :=
	IntervalSelector : ( Begin : expression ".." End : expression )

expression 9 =
	ExponentExpression : ( Expressions : expression )^"**"

expression 10 =
	CallExpression : 
	( 
		Expression : expression 
		(
			( "->" [ "<" TypeArguments : typeDeclaration* ">" ] "(" Arguments : [ expression ]* ")" )
				| ( "=>" Argument : expression )
		)
	)

expression 11 =
	UnaryExpression : 
	(
		( Operator : ( "-" | "~" | "not" | "exists" ) Expression : expression )
			| ( Expression : expression Operator : ( "@@" | "++" | "--" ) )
	)

expression 12 =
	DereferenceExpression : ( Expression : expression Operator : ( "." | "@" | "," ) Member : QualifiedIdentifier )

expression =
	"(" expression ")"
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
	"[" Items : [ expression ]* "]"

TupleSelector :=
	"{" ":" | Members : ( TupleAttributeSelector | TupleReference | TupleKey )* "}"

TupleAttributeSelector :=
	Name : QualifiedIdentifier ":" Value : expression

SetSelector :=
	"{" Items : [ expression ]* "}"

FunctionSelector :=
	( functionParameters | TypeName : QualifiedIdentifier ) "=>" Expression : ClausedExpression

IdentifierExpression := 
	Target : QualifiedIdentifier

IntegerLiteral :=
	_ (digit* | '0x' { '0'..'9', 'a'..'f', 'A'..'F' }*)+ _

DoubleLiteral :=
	_ (digit* '.' digit* [( 'e' | 'E' ) [ '+' | '-' ] digit*])+ _

CharacterLiteral :=
	_ pascalString+ 'c' _

StringLiteral :=
	_ ( PascalSegments : ( Leadings : [ '#' Digits : digit* ]* [ Strings : pascalString ]^( BetweenChars: ( '#' Digits : digit* )* ) EndingChars : [ '#' Digits : digit* ]* ) 
		| ( '"' ( [ '\\' as '\' | '\"' as '"' | '\r' as #13 | '\n' as #10 | '\t' as #9 | {?} &! '"' ]* )+ '"' ) _

CaseExpression :=
    "case" [ [ IsStrict : "strict" ] TestExpression : expression ]
        Items : ( "when" WhenExpression : expression "then" ThenExpression : expression )*
        "else" ElseExpression : expression
    "end"

IfExpression :=
	"if" TestExpression : expression
        "then" ThenExpression : expression
        "else" ElseExpression : expression 

TryExpression :=
	"try" TryExpression : expression "catch" CatchExpression : expression

ClausedExpression :=
	ForClauses : [ ForClause ]*
	LetClauses : [ LetClause ]*
	[ "where" WhereClause : expression ]
	[ "order" "(" OrderDimensions : OrderDimension* ")" ]
	"return" Expression : expression

ForClause :=
	"for" Name : QualifiedIdentifier "in" Expression : expression

LetClause :=
	"let" Name : QualifiedIdentifier ":=" Expression : expression
	
OrderDimension :=
	Expression : expression [ Direction : ( "asc" | "desc" ) ]

pascalString =
	'''' ( [ '''''' as '''' | {?} &! '''' ]* )+ ''''

QualifiedIdentifier :=
	IsRooted : [ '\' ] Items : identifier^'\'

identifier =
	_ ( letter | '_' [ letter | digit | '_' ]* )+ _

_ =	// Whitespace
	[{ ' ', #9..#13 } | lineComment | blockComment ]*
		
blockComment =
	'/*' [ blockComment | {?} &! '*/' ]* '*/'
		
lineComment =
	'//' [ {! #10, #13 } ]*

letter =
	{ 'a'..'z', 'A'..'Z' }
		
digit =
	'0'..'9'

