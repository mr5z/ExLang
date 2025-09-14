using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Tokens;

internal abstract class Token(string value, Location location)
{
	public Location Location { get; } = location;
	public string Value { get; } = value;

	public override string ToString() => $"{GetType().Name}('{Value}') @ {Location.Line}:{Location.Column}";
}
