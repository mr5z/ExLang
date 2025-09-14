using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record FunctionNode(
    string Name,
    List<ParameterNode> Parameters,
    string ReturnType,
    AstNode? Body,
    Location Location,
    List<string>? Attributes = null
) : AstNode(Location);