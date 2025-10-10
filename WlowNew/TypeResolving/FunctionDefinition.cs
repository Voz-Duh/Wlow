using Wlow.Parsing;

namespace Wlow.TypeResolving;

public readonly record struct FunctionDefinition(INodeTypeResolved Node, FunctionMetaType Type);

