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
	OptionalType | requiredTypes

OptionalType :=
	Type : requiredTypes IsRequired : ( "?" | "!" )

requiredTypes =
	ListType | TupleType | SetType | FunctionType | IntervalType | NamedType | TypeOf

ListType :=
	"[" Type : typeDeclaration "]"

SetType :=
	"{" Type : typeDeclaration "}"

TupleType :=
	"{" ":" | Members : ( TupleAttribute | TupleReference | TupleKey )* "}"

TupleAttribute :=
	Name : QualifiedIdentifier ":" Type : typeDeclaration

TupleReference :=
	"ref" Name : QualifiedIdentifier "(" SourceAttributeNames : QualifiedIdentifier* ")" 
		Target : QualifiedIdentifier "(" TargetAttributeNames : QualifiedIdentifier* ")"	

TupleKey :=
	"key" "{" AttributeNames : [ QualifiedIdentifier ]* "}"

FunctionType :=
	functionParameters "=>" [ "<" TypeParameters : typeDeclaration* ">" ] ReturnType : typeDeclaration

functionParameters =
	"(" Parameters : [ FunctionParameter ]* ")"

FunctionParameter :=
	Name : QualifiedIdentifier ":" Type : typeDeclaration

IntervalType :=
	"interval" Type : typeDeclaration

NamedType :=
	Target : QualifiedIdentifier

TypeOf :=
	"typeof" Expression : expression 

expression 1 =
	OfExpression : ( Expression : expression "of" Type : typeDeclaration )

expression 2 =
    LogicalBinaryExpression : ( Left : expression Operator : ( "in" | "or" | "xor" | "like" | "matches" | "??" ) Right : expression )

expression 3 =
	LogicalAndExpression : ( Left : expression "and" Right : expression )

expression 4 =
	BitwiseBinaryExpression : ( Left : expression Operator : ( "^" | "&" | "|" | "<<" | ">>" ) Right : expression )

expression 5 =
	ComparisonExpression : ( Left : expression Operator : ( "=" | "<>" | "<" | ">" | "<=" | ">=" | "?=" ) Right : expression )

expression 6 =
	AdditiveExpression : ( Left : expression Operator : ( "+" | "-" ) Right : expression )

expression 7 =
	MultiplicativeExpression : ( Left : expression Operator : ( "*" | "/" | "%" ) Right : expression )

expression 8 :=
	IntervalSelector : ( Begin : expression ".." End : expression )

expression 9 R =
	ExponentExpression : ( Left : expression "**" Right : expression )

expression 10 =
	CallExpression : 
	( 
		Argument : expression 
		(
			( "->" Expression : expression [ "<" TypeArguments : typeDeclaration* ">" ] "(" Arguments : [ expression ]* ")" )
				| ( "=>" Expression : expression )
		)
	)

expression 11 =
	UnaryExpression : 
	(
		( Operator : ( "-" | "~" | "not" | "exists" ) Expression : expression )
			| ( Expression : expression Operator : ( "@@" | "++" | "--" ) )
	)

expression 12 =
	DereferenceExpression : ( Left : expression Operator : ( "." | "@" | "," ) Right : expression )

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
		| NameLiteral
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
	[ Name : QualifiedIdentifier ] ":" Value : expression

SetSelector :=
	"{" Items : [ expression ]* "}"

FunctionSelector :=
	functionParameters "=>" Expression : ClausedExpression

IdentifierExpression := 
	Target : QualifiedIdentifier

IntegerLiteral :=
	_ (digit* | '0x' { '0'..'9', 'a'..'f', 'A'..'F' }*)+ _

DoubleLiteral :=
	_ (digit* '.' digit* [( 'e' | 'E' ) [ '+' | '-' ] digit*])+ _

CharacterLiteral :=
	_ pascalString+ 'c' _

NameLiteral :=
	_ pascalString+ 'n' _

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
	_ IsRooted : [ '\' ] Items : identifier^'\' _

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

