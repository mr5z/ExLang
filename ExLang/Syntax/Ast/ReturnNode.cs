using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record ReturnNode(
    AstNode Expression,
    Location Location
) : AstNode(Location);