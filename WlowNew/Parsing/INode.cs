
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public interface INode
{
    Info Info { get; }
    INodeTypeResolved TypeResolve(Scope scope);
}

public static class INodeHelper
{
#pragma warning disable IDE0305 // Simplify collection initialization
    public static ImmutableArray<INodeTypeResolved> TypeResolve<T>(this T values, Scope scope)
        where T : IEnumerable<INode>
    {
        var len = values.Count();
        var res = new INodeTypeResolved[len];
        var i = 0;
        foreach (var value in values)
            res[i++] = value.TypeResolve(scope);
        return res.ToImmutableArray();
    }
#pragma warning restore IDE0305 // Simplify collection initialization
}
