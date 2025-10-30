using Wlow.Parsing;

namespace Wlow.TypeResolving;

public readonly record struct FunctionDefinition(INodeTypeResolved BumpNode, FunctionMetaType Type) : IFunctionDefinition;
