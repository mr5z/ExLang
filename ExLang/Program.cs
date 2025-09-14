using ExLang.Syntax;
using ExLang.Syntax.Tokens.Keywords;
using ExLang.Syntax.Tokens.Literals;
using ExLang.Syntax.Tokens.Symbols;

namespace ExLang;

internal class Program
{
	static void Main(string[] args)
	{
		var source = @"
contract MathUtils {
    @Alias(""+"")
    def add(x: Number, y: Number): Number = x + y

    @Alias(""-"")
    def subtract(x: Number, y: Number): Number = x - y

    def multiply(x: Number, y: Number): Number {
        // Multiplies two numbers
        return x * y
    }

    def divide(x: Number, y: Number): Number {
        /* Handle division, check for zero */
        if y == 0 {
            return 0
        } else {
            return x / y
        }
    }

    def rangeSum(start: Number, end: Number): Number {
        var sum = 0
        for i in start..end {
            sum += i
        }
        return sum
    }

    def formatResult(value: Number): String = @""Result: {value}""

    def escapeTest(): String = ""Line1\nLine2\tTabbed\\Backslash\""Quote\""""

    def rawTest(): String = `Raw string with no escapes: \n \t \\ "" '`

    def hexTest(): Number = 0xDEAD_BEEF

    def binTest(): Number = 0b1010_1100

    def floatTest(): Number = 3.14159e+00

    def complexOp(a: Number, b: Number): Number = (a << 2) + (b >> 1) - a * b / (a != b ? 1 : 2)
}
";

		var lexer = new Lexer(source);

		Console.WriteLine("========================================");
		Console.WriteLine(source);
		Console.WriteLine("========================================");
		Console.WriteLine("Tokens (skipping whitespace/comments):");
		while (true)
		{
			var token = lexer.NextToken();
			if (token is Eof) break;

			// optionally skip whitespace/comments in display
			if (token is Whitespace || token is Comment)
				continue;

			string kind = token switch
			{
				Keyword k => $"Keyword({k.Type})",
				Symbol s => $"Symbol({s.Type})",
				Identifier => "Identifier",
				Literal l => $"Literal({l.Type})",
				_ => token.GetType().Name
			};

			Console.WriteLine($"{token.Location.Line,3}:{token.Location.Column,3} {kind,-20} {token.Value}");
		}
	}
}
