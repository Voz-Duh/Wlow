
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public interface INode
{
    Info Info { get; }
    INodeTypeResolved TypeResolve(Scope scope);
}

public interface INodeTypeResolved
{
    TypedValue ValueTypeInfo { get; }
}
