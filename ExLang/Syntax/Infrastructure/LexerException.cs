namespace ExLang.Syntax.Infrastructure;

internal class LexerException(string message, Location location) : Exception($"{message} at {location.Line}:{location.Column}")
{
	public Location Location { get; } = location;
}
