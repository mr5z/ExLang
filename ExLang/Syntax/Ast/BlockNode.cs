using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record BlockNode(
    List<AstNode> Statements,
    Location Location
) : AstNode(Location);