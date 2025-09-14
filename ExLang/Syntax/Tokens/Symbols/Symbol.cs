using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Tokens.Symbols;

internal sealed class Symbol(SymbolType type, string value, Location location) : Token(value, location)
{
	public SymbolType Type { get; } = type;
}
