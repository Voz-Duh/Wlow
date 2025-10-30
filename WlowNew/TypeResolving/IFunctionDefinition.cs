using Wlow.Parsing;

namespace Wlow.TypeResolving;

public interface IFunctionDefinition
{
    FunctionMetaType Type { get; }
    INodeTypeResolved BumpNode { get; }
}
