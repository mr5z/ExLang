using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record VarDeclNode(
    string Name,
    AstNode Expression,
    Location Location
) : AstNode(Location);