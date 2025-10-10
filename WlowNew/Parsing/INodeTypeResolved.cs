using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public interface INodeTypeResolved
{
    TypedValue ValueTypeInfo { get; }
    INodeTypeResolved TypeFixation();
}
