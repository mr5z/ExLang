using ExLang.Syntax.Infrastructure;
using ExLang.Syntax.Tokens;

internal sealed class Whitespace(string value, Location location) : Token(value, location);
