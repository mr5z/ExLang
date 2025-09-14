namespace ExLang.Syntax.Tokens.Keywords;

[AttributeUsage(AttributeTargets.Field)]
internal class KeywordSpellingAttribute(string spelling) : Attribute
{
	public string Spelling { get; } = spelling;
}
