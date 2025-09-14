namespace ExLang.Syntax.Tokens.Keywords;

internal enum KeywordType
{
	[KeywordSpelling("def")]
	Def,
	[KeywordSpelling("contract")]
	Contract,
	[KeywordSpelling("self")]
	Self,
	[KeywordSpelling("for")]
	For,
	[KeywordSpelling("in")]
	In,
	[KeywordSpelling("if")]
	If,
	[KeywordSpelling("else")]
	Else,
	[KeywordSpelling("return")]
	Return,
}
