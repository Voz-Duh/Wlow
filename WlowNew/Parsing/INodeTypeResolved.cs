using System.Collections.Immutable;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public interface INodeTypeResolved
{
    TypedValue ValueTypeInfo { get; }
    INodeTypeResolved TypeFixation();
}

public static class INodeTypeResolvedHelper
{
#pragma warning disable IDE0305 // Simplify collection initialization
    public static ImmutableArray<INodeTypeResolved> TypeFixation<T>(this T values)
        where T : IEnumerable<INodeTypeResolved>
    {
        var len = values.Count();
        var res = new INodeTypeResolved[len];
        var i = 0;
        foreach (var value in values)
            res[i++] = value.TypeFixation();
        return res.ToImmutableArray();
    }

    public static ImmutableArray<IMetaType> Types<T>(this T values)
        where T : IEnumerable<INodeTypeResolved>
    {
        var len = values.Count();
        var res = new IMetaType[len];
        var i = 0;
        foreach (var value in values)
            res[i++] = value.ValueTypeInfo.Type;
        return res.ToImmutableArray();
    }
#pragma warning restore IDE0305 // Simplify collection initialization
}
