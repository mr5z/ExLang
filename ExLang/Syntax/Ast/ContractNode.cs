using ExLang.Syntax.Infrastructure;

namespace ExLang.Syntax.Ast;

internal record ContractNode(
    string Name,
    List<FunctionNode> Functions,
    Location Location
) : AstNode(Location);