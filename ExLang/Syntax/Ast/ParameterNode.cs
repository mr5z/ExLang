using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record ParameterNode(
    string Name,
    string Type,
    Location Location
) : AstNode(Location);