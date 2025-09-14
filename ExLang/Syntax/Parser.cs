using ExLang.Syntax.Ast;
using ExLang.Syntax.Tokens;
using ExLang.Syntax.Tokens.Keywords;
using ExLang.Syntax.Tokens.Symbols;

namespace ExLang.Syntax;

internal class Parser(Lexer lexer)
{
    private readonly Lexer _lexer = lexer;
    private Token _current = lexer.NextToken();

    private void Next() => _current = _lexer.NextToken();

    private bool Match(string value)
    {
        if (_current.Value == value)
        {
            Next();
            return true;
        }
        return false;
    }

    private void Expect(string value)
    {
        if (_current.Value != value)
            throw new Exception($"Expected '{value}' but got '{_current.Value}' at line {_current.Location.Line}, col {_current.Location.Column}");
        Next();
    }

    public ContractNode ParseContract()
    {
        Expect("Contract");
        var name = _current.Value;
        Next();

        Expect("{");

        var functions = new List<FunctionNode>();
        while (_current is Keyword k && k.Type == KeywordType.Def || _current is Symbol s && s.Type == SymbolType.At)
        {
            var attributes = ParseAttributes();
            functions.Add(ParseFunction(attributes));
        }

        Expect("}");

        return new ContractNode(name, functions, _current.Location);
    }

    private List<string> ParseAttributes()
    {
        var attributes = new List<string>();
        while (_current is Symbol s && s.Type == SymbolType.At)
        {
            Next(); // '@'
            var attrName = _current.Value;
            Next();
            if (Match("("))
            {
                var args = new List<string>();
                while (_current.Value != ")")
                {
                    args.Add(_current.Value);
                    Next();
                    Match(","); // skip comma
                }
                Next(); // ')'
                attributes.Add($"{attrName}({string.Join(",", args)})");
            }
            else
            {
                attributes.Add(attrName);
            }
        }
        return attributes;
    }

    private FunctionNode ParseFunction(List<string> attributes)
    {
        Expect("Def");
        var name = _current.Value;
        Next();

        Expect("(");
        var parameters = new List<ParameterNode>();
        if (_current.Value != ")")
        {
            do
            {
                parameters.Add(ParseParameter());
            } while (Match(","));
        }
        Expect(")");

        Expect(":");
        var returnType = _current.Value;
        Next();

        AstNode? body = null;
        if (Match("="))
        {
            body = ParseExpression();
        }
        else if (Match("{"))
        {
            body = ParseBlock();
        }

        return new FunctionNode(name, parameters, returnType, body, _current.Location)
        {
            Attributes = attributes
        };
    }

    private ParameterNode ParseParameter()
    {
        var name = _current.Value;
        Next();
        Expect(":");
        var type = _current.Value;
        Next();
        return new ParameterNode(name, type, _current.Location);
    }

    private AstNode ParseBlock()
    {
        var statements = new List<AstNode>();
        while (_current.Value != "}")
        {
            if (_current is Keyword k)
            {
                switch (k.Type)
                {
                    case KeywordType.If:
                        statements.Add(ParseIf());
                        break;
                    case KeywordType.For:
                        statements.Add(ParseFor());
                        break;
                    case KeywordType.Return:
                        statements.Add(ParseReturn());
                        break;
                    case KeywordType.Self:
                    case KeywordType.Contract:
                    case KeywordType.Def:
                    case KeywordType.In:
                    case KeywordType.Else:
                        // Handle other keywords as needed
                        break;
                    default:
                        statements.Add(ParseExpression());
                        break;
                }
            }
            else if (_current.Value == "var")
            {
                statements.Add(ParseVarDeclaration());
            }
            else
            {
                statements.Add(ParseExpression());
            }
        }
        Next(); // consume '}'
        return new BlockNode(statements, _current.Location);
    }

    private AstNode ParseIf()
    {
        Expect("If");
        var condition = ParseExpression();
        Expect("{");
        var thenBlock = ParseBlock();
        AstNode? elseBlock = null;
        if (_current is Keyword k && k.Type == KeywordType.Else)
        {
            Next();
            Expect("{");
            elseBlock = ParseBlock();
        }
        return new IfNode(condition, thenBlock, elseBlock, _current.Location);
    }

    private AstNode ParseFor()
    {
        Expect("For");
        var iterator = _current.Value;
        Next();
        Expect("In");
        var range = ParseExpression();
        Expect("{");
        var body = ParseBlock();
        return new ForNode(iterator, range, body, _current.Location);
    }

    private AstNode ParseReturn()
    {
        Expect("Return");
        var expr = ParseExpression();
        return new ReturnNode(expr, _current.Location);
    }

    private AstNode ParseVarDeclaration()
    {
        Expect("var");
        var name = _current.Value;
        Next();
        Expect("=");
        var expr = ParseExpression();
        return new VarDeclNode(name, expr, _current.Location);
    }

    private AstNode ParseExpression()
    {
        // This is a placeholder for a real expression parser.
        // For now, just consume the next token and wrap it.
        var token = _current;
        Next();
        return new ExpressionNode(token.Value, token.Location);
    }
}