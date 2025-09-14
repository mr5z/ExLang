using ExLang.Syntax.Infrastructure;
using ExLang.Syntax.Tokens;

internal sealed class Eof(Location location) : Token(string.Empty, location);
