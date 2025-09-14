using ExLang.Syntax.Infrastructure;
using ExLang.Syntax.Tokens;

internal sealed class Comment(string value, Location location) : Token(value, location);
