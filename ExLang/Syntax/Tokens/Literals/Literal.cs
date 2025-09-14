using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Tokens.Literals;

internal sealed class Literal(string value, LiteralType type, Location location) : Token(value, location)
{
	public LiteralType Type { get; } = type;
}
