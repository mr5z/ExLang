namespace ExLang.Syntax.Tokens.Symbols;

internal enum SymbolType
{
	At,         // @
	Colon,      // :
	Arrow,      // ->
	Equal,      // =
	EqualEqual, // ==
	NotEqual,   // !=
	Plus,       // +
	PlusEqual,  // +=
	Minus,      // -
	MinusEqual, // -=
	Star,       // *
	StarEqual,  // *=
	Slash,      // /
	SlashEqual, // /=
	Comma,      // ,
	Dot,        // .
	LParen,     // (
	RParen,     // )
	LBrace,     // {
	RBrace,     // }
	LBracket,   // [
	RBracket,   // ]
	Semicolon,  // ;
	Question,   // ?
	Pipe,       // |
	Ampersand,  // &
	Less,       // <
	Greater,    // >
	LessEqual,  // <=
	GreaterEqual, // >=
	LeftShift,  // <<
	RightShift, // >>
	TripleRightShift, // >>>
	Range,      // ..
	Scope,      // ::
	FatArrow,   // =>
	ArrowLambda, // => (alias)
	Unknown
}
