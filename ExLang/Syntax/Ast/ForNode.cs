using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record ForNode(
    string Iterator,
    AstNode Range,
    AstNode Body,
    Location Location
) : AstNode(Location);