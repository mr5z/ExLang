using ExLang.Syntax.Infrastructure;
using ExLang.Syntax.Tokens;
using ExLang.Syntax.Tokens.Keywords;
using ExLang.Syntax.Tokens.Literals;
using ExLang.Syntax.Tokens.Symbols;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ExLang.Syntax;

internal class Lexer(string text)
{
	private readonly string _text = text ?? string.Empty;
	private int _position = 0;
	private int _line = 1;
	private int _column = 1;

	private readonly Queue<Token> _buffer = new();

	private static readonly Dictionary<string, SymbolType> SymbolTable = new(StringComparer.Ordinal)
	{
		["@"] = SymbolType.At,
		[":"] = SymbolType.Colon,
		["->"] = SymbolType.Arrow,
		["="] = SymbolType.Equal,
		["=="] = SymbolType.EqualEqual,
		["!="] = SymbolType.NotEqual,
		["+"] = SymbolType.Plus,
		["+="] = SymbolType.PlusEqual,
		["-"] = SymbolType.Minus,
		["-="] = SymbolType.MinusEqual,
		["*"] = SymbolType.Star,
		["*="] = SymbolType.StarEqual,
		["/"] = SymbolType.Slash,
		["/="] = SymbolType.SlashEqual,
		[","] = SymbolType.Comma,
		["."] = SymbolType.Dot,
		["("] = SymbolType.LParen,
		[")"] = SymbolType.RParen,
		["{"] = SymbolType.LBrace,
		["}"] = SymbolType.RBrace,
		["["] = SymbolType.LBracket,
		["]"] = SymbolType.RBracket,
		[";"] = SymbolType.Semicolon,
		["?"] = SymbolType.Question,
		["|"] = SymbolType.Pipe,
		["&"] = SymbolType.Ampersand,
		["<"] = SymbolType.Less,
		[">"] = SymbolType.Greater,
		["<="] = SymbolType.LessEqual,
		[">="] = SymbolType.GreaterEqual,
		["<<"] = SymbolType.LeftShift,
		[">>"] = SymbolType.RightShift,
		[">>>"] = SymbolType.TripleRightShift,
		[".."] = SymbolType.Range,
		["::"] = SymbolType.Scope,
		["=>"] = SymbolType.FatArrow,
	};

	private char Current => _position >= _text.Length ? '\0' : _text[_position];
	private char Peek(int offset = 1) => _position + offset >= _text.Length ? '\0' : _text[_position + offset];

	private Location CurrentLocation => new(_line, _column);

	// Public API: PeekToken with lookahead, NextToken consumes
	public Token PeekToken(int lookahead = 0)
	{
		while (_buffer.Count <= lookahead)
		{
			var t = NextTokenInternal();
			_buffer.Enqueue(t);
		}

		// allow lookahead beyond EOF to always return EOF
		if (lookahead >= _buffer.Count)
			return _buffer.Peek();

		// Access nth element in queue:
		using var e = _buffer.GetEnumerator();
		Token? found = null;
		for (int i = 0; i <= lookahead && e.MoveNext(); i++)
		{
			found = e.Current;
		}

		return found!;
	}

	public Token NextToken()
	{
		if (_buffer.Count > 0)
			return _buffer.Dequeue();

		return NextTokenInternal();
	}

	// Helper for parsers
	public T Expect<T>(Func<Token, bool>? predicate = null) where T : Token
	{
		var token = NextToken();
		if (token is T t && (predicate == null || predicate(token)))
			return t;
		throw new LexerException($"Expected token of type {typeof(T).Name} but got {token.GetType().Name} ('{token.Value}')", token.Location);
	}

	// Internal production of next token (never returns null)
	private Token NextTokenInternal()
	{
		SkipZeroWidth();

		// EOF
		if (Current == '\0')
			return new Eof(CurrentLocation);

		// Whitespace (space, tab, newline) -> produce Whitespace token
		if (char.IsWhiteSpace(Current))
			return ReadWhitespace();

		// Comments: // line, /* block */
		if (Current == '/' && Peek() == '/')
			return ReadLineComment();

		if (Current == '/' && Peek() == '*')
			return ReadBlockComment();

		// Symbols / operators
		if (IsSymbolStart(Current))
			return ReadSymbol();

		// Identifiers or keywords (allow Unicode letters)
		if (char.IsLetter(Current) || Current == '_' || char.GetUnicodeCategory(Current) == UnicodeCategory.LetterNumber)
			return ReadIdentifierOrKeyword();

		// Number literals: 0x, 0b, decimals, floats, exponentials, underscores
		if (char.IsDigit(Current))
			return ReadNumberLiteral();

		// String literal forms: @"..." (verbatim), `...` (backtick), "..." (escapes)
		if (Current == '@' && Peek() == '"')
			return ReadVerbatimStringLiteral();

		if (Current == '`')
			return ReadBacktickStringLiteral();

		if (Current == '"')
			return ReadStringLiteral();

		throw new LexerException($"Unexpected character: '{Current}'", CurrentLocation);
	}

	// Add this new method for backtick string literals
	private Literal ReadBacktickStringLiteral()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// consume opening backtick
		Advance();

		while (Current != '\0' && Current != '`')
		{
			sb.Append(Current);
			Advance();
		}

		if (Current != '`')
			throw new LexerException("Unterminated backtick string literal", start);

		// consume closing backtick
		Advance();

		return new Literal(sb.ToString(), LiteralType.String, start);
	}

	private void SkipZeroWidth()
	{
		// reserved for BOM or other zero width checks; currently no-op
	}

	private static bool IsSymbolStart(char c)
	{
		// include common operator starting chars
		return "@:=,.(){}[]+-*/!<>&|;:?".IndexOf(c) >= 0;
	}

	private Whitespace ReadWhitespace()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		while (char.IsWhiteSpace(Current))
		{
			sb.Append(Current);
			Advance();
		}

		return new Whitespace(sb.ToString(), start);
	}

	private Comment ReadLineComment()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// consume "//"
		sb.Append(Current);
		Advance();
		sb.Append(Current);
		Advance();

		while (Current != '\0' && Current != '\n')
		{
			sb.Append(Current);
			Advance();
		}

		return new Comment(sb.ToString(), start);
	}

	private Comment ReadBlockComment()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// consume "/*"
		sb.Append(Current);
		Advance();
		sb.Append(Current);
		Advance();

		while (Current != '\0')
		{
			if (Current == '*' && Peek() == '/')
			{
				sb.Append(Current);
				Advance();
				sb.Append(Current);
				Advance();
				break;
			}

			sb.Append(Current);
			Advance();
		}

		if (Current == '\0' && !sb.ToString().EndsWith("*/"))
			throw new LexerException("Unterminated block comment", start);

		return new Comment(sb.ToString(), start);
	}

	private Symbol ReadSymbol()
	{
		var start = CurrentLocation;
		// Try up to three-char symbols (e.g., >>>) or two-char symbols
		// Build candidate strings progressively
		var maxLook = 3;
		var candidate = new StringBuilder();

		for (var i = 0; i < maxLook && _position + i < _text.Length; i++)
		{
			candidate.Append(_text[_position + i]);
			var s = candidate.ToString();

			if (SymbolTable.TryGetValue(s, out SymbolType value))
			{
				// If longer possible, prefer longer match; so we continue unless next can't form valid symbol.
				// We'll check greedily: try to grow until no longer symbol exists.
				// Look ahead one more char to see if a longer symbol would match.
				var nextIndex = _position + i + 1;
				var canGrow = nextIndex < _text.Length;
				if (canGrow)
				{
					var nextCandidate = new StringBuilder(s).Append(_text[nextIndex]).ToString();
					if (SymbolTable.ContainsKey(nextCandidate))
						continue; // consume more
				}

				// We have the longest match for this position
				// Advance by length of s
				for (int j = 0; j < s.Length; j++)
					Advance();

				return new Symbol(value, s, start);
			}
		}

		// If not recognized, consume single char and return unknown symbol error
		var unknown = Current.ToString();
		Advance();
		throw new LexerException($"Unexpected symbol '{unknown}'", start);
	}

	private static KeywordType? GetKeyword(string value)
	{
		var spellingAttributes = Enum.GetValues<KeywordType>()
			.Select(keyword => new
			{
				Attribute = keyword.GetType()
					.GetField(keyword.ToString())
					?.GetCustomAttribute<KeywordSpellingAttribute>(),
				Keyword = keyword
			});

		return spellingAttributes.FirstOrDefault(sa => sa.Attribute?.Spelling == value)?.Keyword;
	}

	private Token ReadIdentifierOrKeyword()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// Accept letters, digits, underscore, and many Unicode identifier categories
		while (Current != '\0' && (char.IsLetterOrDigit(Current) || Current == '_' ||
			   CharUnicodeCategoryIsLetterOrNumber(Current)))
		{
			sb.Append(Current);
			Advance();
		}

		var stringValue = sb.ToString();
		var value = GetKeyword(stringValue);

		// keyword lookup (case-sensitive)
		if (value.HasValue)
			return new Keyword(value.Value, start);

		// fallback identifier
		return new Identifier(stringValue, start);
	}

	private static bool CharUnicodeCategoryIsLetterOrNumber(char c)
	{
		var cat = char.GetUnicodeCategory(c);
		return cat == UnicodeCategory.LetterNumber ||
			   cat == UnicodeCategory.LowercaseLetter ||
			   cat == UnicodeCategory.UppercaseLetter ||
			   cat == UnicodeCategory.OtherLetter ||
			   cat == UnicodeCategory.TitlecaseLetter ||
			   cat == UnicodeCategory.ModifierLetter;
	}

	private Literal ReadNumberLiteral()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// Support 0x (hex), 0b (binary), decimal, underscores, exponents
		if (Current == '0' && (Peek() == 'x' || Peek() == 'X'))
		{
			sb.Append(Current); Advance(); // 0
			sb.Append(Current); Advance(); // x
			while (IsHexDigit(Current) || Current == '_')
			{
				if (Current != '_') sb.Append(Current);
				Advance();
			}
			return new Literal(sb.ToString(), LiteralType.Number, start);
		}

		if (Current == '0' && (Peek() == 'b' || Peek() == 'B'))
		{
			sb.Append(Current); Advance(); // 0
			sb.Append(Current); Advance(); // b
			while (Current == '0' || Current == '1' || Current == '_')
			{
				if (Current != '_') sb.Append(Current);
				Advance();
			}
			return new Literal(sb.ToString(), LiteralType.Number, start);
		}

		// Decimal/integral/fraction/exponent
		bool sawDot = false;
		while (char.IsDigit(Current) || Current == '_' || !sawDot && Current == '.')
		{
			if (Current == '.')
			{
				// If '.' followed by digit -> decimal point; if followed by '.' => range operator, stop here
				if (Peek() == '.')
					break;
				sawDot = true;
				sb.Append('.');
				Advance();
				continue;
			}

			if (Current != '_') sb.Append(Current);
			Advance();
		}

		// Exponent part
		if (Current == 'e' || Current == 'E')
		{
			sb.Append(Current);
			Advance();
			if (Current == '+' || Current == '-')
			{
				sb.Append(Current);
				Advance();
			}
			if (!char.IsDigit(Current))
				throw new LexerException("Invalid exponent in number literal", CurrentLocation);

			while (char.IsDigit(Current) || Current == '_')
			{
				if (Current != '_') sb.Append(Current);
				Advance();
			}
		}

		return new Literal(sb.ToString(), LiteralType.Number, start);
	}

	private static bool IsHexDigit(char c)
	{
		return c >= '0' && c <= '9' ||
			   c >= 'a' && c <= 'f' ||
			   c >= 'A' && c <= 'F';
	}

	private Literal ReadStringLiteral()
	{
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// consume opening quote
		Advance();

		while (Current != '\0' && Current != '"')
		{
			if (Current == '\\')
			{
				Advance();
				if (Current == '\0')
					throw new LexerException("Unterminated escape sequence", CurrentLocation);

				sb.Append(Current switch
				{
					'n' => '\n',
					't' => '\t',
					'r' => '\r',
					'"' => '"',
					'\\' => '\\',
					'0' => '\0',
					_ => throw new LexerException($"Invalid escape sequence \\{Current}", CurrentLocation)
				});
				Advance();
				continue;
			}

			sb.Append(Current);
			Advance();
		}

		if (Current != '"')
			throw new LexerException("Unterminated string literal", start);

		// consume closing quote
		Advance();

		return new Literal(sb.ToString(), LiteralType.String, start);
	}

	private Literal ReadVerbatimStringLiteral()
	{
		// starts with @" ... " where "" is escaped quote sequence inside verbatim
		var start = CurrentLocation;
		var sb = new StringBuilder();

		// consume @ and opening "
		Advance(); // '@'
		Advance(); // '"'

		while (Current != '\0')
		{
			if (Current == '"' && Peek() == '"')
			{
				// a double double-quote inside verbatim means a single quote in value
				sb.Append('"');
				Advance(); Advance();
				continue;
			}

			if (Current == '"')
			{
				Advance(); // closing quote
				return new Literal(sb.ToString(), LiteralType.String, start);
			}

			sb.Append(Current);
			Advance();
		}

		throw new LexerException("Unterminated verbatim string literal", start);
	}

	private void Advance()
	{
		if (Current == '\n')
		{
			_line++;
			_column = 1;
		}
		else if (Current != '\0')
		{
			_column++;
		}
		_position++;
	}
}
