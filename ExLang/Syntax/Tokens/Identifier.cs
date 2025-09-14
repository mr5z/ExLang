using ExLang.Syntax.Infrastructure;
using ExLang.Syntax.Tokens;

internal sealed class Identifier(string value, Location location) : Token(value, location);
