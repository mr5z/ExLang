using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record ExpressionNode(
    string Value,
    Location Location
) : AstNode(Location);