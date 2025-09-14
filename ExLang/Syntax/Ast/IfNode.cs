using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record IfNode(
    AstNode Condition,
    AstNode ThenBlock,
    AstNode? ElseBlock,
    Location Location
) : AstNode(Location);