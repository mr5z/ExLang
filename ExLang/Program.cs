namespace ExLang;

public record Location(int Line, int Column);

public abstract class Token(string value, Location location)
{
    public Location Location { get; } = location;
    public string Value { get; } = value;
}

public class Keyword(KeywordType type, Location location) : Token(type.ToString(), location)
{
    public KeywordType Type { get; } = type;
}

public enum KeywordType
{
    Def,
    Contract,
    Self,
    For,
    In
}

public class Identifier(string value, Location location) : Token(value, location);

public class Symbol : Token
{
    public SymbolType Type { get; }

    private Symbol(SymbolType type, string value, Location location) 
        : base(value, location)
    {
        Type = type;
    }

    public static Symbol At(Location loc) => new(SymbolType.At, "@", loc);
    public static Symbol Colon(Location loc) => new(SymbolType.Colon, ":", loc);
    public static Symbol Arrow(Location loc) => new(SymbolType.Arrow, "->", loc);
    public static Symbol Equal(Location loc) => new(SymbolType.Equal, "=", loc);
    public static Symbol Comma(Location loc) => new(SymbolType.Comma, ",", loc);
    public static Symbol Dot(Location loc) => new(SymbolType.Dot, ".", loc);
    public static Symbol LParen(Location loc) => new(SymbolType.LParen, "(", loc);
    public static Symbol RParen(Location loc) => new(SymbolType.RParen, ")", loc);
    public static Symbol LBrace(Location loc) => new(SymbolType.LBrace, "{", loc);
    public static Symbol RBrace(Location loc) => new(SymbolType.RBrace, "}", loc);
    public static Symbol LBracket(Location loc) => new(SymbolType.LBracket, "[", loc);
    public static Symbol RBracket(Location loc) => new(SymbolType.RBracket, "]", loc);
}

public enum SymbolType
{
    At,         // @
    Colon,      // :
    Arrow,      // ->
    Equal,      // =
    Comma,      // ,
    Dot,        // .
    LParen,     // (
    RParen,     // )
    LBrace,     // {
    RBrace,     // }
    LBracket,   // [
    RBracket    // ]
}

public class Literal(string value, LiteralType type, Location location) : Token(value, location)
{
    public LiteralType Type { get; } = type;
}

public enum LiteralType
{
    String,
    Number
}

public class Whitespace(string value, Location location) : Token(value, location);

public class Comment(string value, Location location) : Token(value, location);

public class Lexer(string text)
{
    private readonly string _text = text;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    private char Current => _position >= _text.Length ? '\0' : _text[_position];
    private char Peek(int offset = 1) => _position + offset >= _text.Length ? '\0' : _text[_position + offset];

    public Token? NextToken()
    {
        // Handle end of file
        if (Current == '\0')
            return null;

        // Handle whitespace
        if (char.IsWhiteSpace(Current))
            return ReadWhitespace();

        // Handle comments
        if (Current == '/' && Peek() == '/')
            return ReadComment();

        // Handle symbols
        if (IsSymbolStart(Current))
            return ReadSymbol();

        // Handle identifiers and keywords
        if (char.IsLetter(Current) || Current == '_')
            return ReadIdentifierOrKeyword();

        // Handle literals
        if (char.IsDigit(Current))
            return ReadNumberLiteral();
        
        if (Current == '"')
            return ReadStringLiteral();

        throw new Exception($"Unexpected character: {Current} at line {_line}, column {_column}");
    }

    private Location CurrentLocation => new(_line, _column);

    private static bool IsSymbolStart(char c) => "@:=,.<>(){}[]+-*/".Contains(c);

    // Implementation of reading methods...
    private Whitespace ReadWhitespace()
    {
        var start = CurrentLocation;
        var value = "";
        
        while (char.IsWhiteSpace(Current))
        {
            value += Current;
            Advance();
        }
        
        return new Whitespace(value, start);
    }

    private void Advance()
    {
        if (Current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        _position++;
    }

    private Comment ReadComment()
    {
        var start = CurrentLocation;
        var value = "";
        
        // Consume both forward slashes
        while (Current == '/')
        {
            value += Current;
            Advance();
        }
        
        // Read until end of line or end of file
        while (Current != '\0' && Current != '\n')
        {
            value += Current;
            Advance();
        }
        
        return new Comment(value, start);
    }

    private Symbol ReadSymbol()
    {
        var loc = CurrentLocation;
        var first = Current;
        Advance();

        // Check for two-character symbols
        if (first == '-' && Current == '>')
        {
            Advance();
            return Symbol.Arrow(loc);
        }

        // Single-character symbols
        return first switch
        {
            '@' => Symbol.At(loc),
            ':' => Symbol.Colon(loc),
            '=' => Symbol.Equal(loc),
            ',' => Symbol.Comma(loc),
            '.' => Symbol.Dot(loc),
            '(' => Symbol.LParen(loc),
            ')' => Symbol.RParen(loc),
            '{' => Symbol.LBrace(loc),
            '}' => Symbol.RBrace(loc),
            '[' => Symbol.LBracket(loc),
            ']' => Symbol.RBracket(loc),
            _ => throw new Exception($"Unexpected symbol: {first} at line {loc.Line}, column {loc.Column}")
        };
    }

    private Token ReadIdentifierOrKeyword()
    {
        var start = CurrentLocation;
        var value = "";

        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            value += Current;
            Advance();
        }

        // Check if it's a keyword
        if (Enum.TryParse<KeywordType>(value, true, out var keywordType))
        {
            return new Keyword(keywordType, start);
        }

        return new Identifier(value, start);
    }

    private Literal ReadNumberLiteral()
    {
        var start = CurrentLocation;
        var value = "";

        // Read integer part
        while (char.IsDigit(Current))
        {
            value += Current;
            Advance();
        }

        // Read decimal part if present
        if (Current == '.' && char.IsDigit(Peek()))
        {
            value += Current;
            Advance();

            while (char.IsDigit(Current))
            {
                value += Current;
                Advance();
            }
        }

        return new Literal(value, LiteralType.Number, start);
    }

    private Literal ReadStringLiteral()
    {
        var start = CurrentLocation;
        var value = "";
        
        // Skip the opening quote
        Advance();

        while (Current != '\0' && Current != '"')
        {
            // Handle escape sequences
            if (Current == '\\')
            {
                Advance();
                value += Current switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '"' => '"',
                    '\\' => '\\',
                    _ => throw new Exception($"Invalid escape sequence: \\{Current} at line {_line}, column {_column}")
                };
            }
            else
            {
                value += Current;
            }
            Advance();
        }

        if (Current != '"')
        {
            throw new Exception($"Unterminated string literal at line {start.Line}, column {start.Column}");
        }

        // Skip the closing quote
        Advance();

        return new Literal(value, LiteralType.String, start);
    }
}

internal class Program
{
    /*
    contract
        attribute specifier
            open parenthesis
                string literal
            close parenthesis
        definition start
            function name
                open parenthesis
                    parameter name
                    colon
                    parameter type
                close parenthesis
            colon
            return type
    */
    static void Main(string[] args)
    {
        var source = @"
            contract Numeric { self ->
                @Alias(""+"")
                def plus(other: Self): Self
            }
        ";

        var lexer = new Lexer(source);
        var indent = 0;
        Token? token;
        
        while ((token = lexer.NextToken()) != null)
        {
            // Skip whitespace and comments in the output
            if (token is Whitespace or Comment)
                continue;

            // Adjust indentation based on braces
            if (token is Symbol { Type: SymbolType.LBrace })
                indent++;
            else if (token is Symbol { Type: SymbolType.RBrace })
                indent--;

            // Print the token with proper indentation
            var indentation = new string(' ', indent * 2);
            var tokenType = token switch
            {
                Keyword k => k.Type.ToString(),
                Symbol s => s.Type.ToString(),
                Identifier => "Identifier",
                Literal l => $"Literal({l.Type})",
                _ => token.GetType().Name
            };

            Console.WriteLine($"{indentation}{tokenType}: {token.Value}");
        }
    }
} 