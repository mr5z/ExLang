using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Tokens.Keywords;

internal sealed class Keyword(KeywordType type, Location location) : Token(type.ToString(), location)
{
	public KeywordType Type { get; } = type;
}
