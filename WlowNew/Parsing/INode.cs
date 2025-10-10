
using Wlow.Shared;

namespace Wlow.Parsing;

public interface INode
{
    Info Info { get; }
    INodeTypeResolved TypeResolve(Scope scope);
}
