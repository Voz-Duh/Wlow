using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct TupleNode(Info Info, ImmutableArray<INode> Elements) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope) {
        var elements = Elements.TypeResolve(scope);
        var types = elements.Types();
        var type = TupleMetaType.Create(Info, scope, types);
        return new TupleNodeTypeResolved(
            Info, TypedValue.From(type),
            elements
        );
    }

    public override string ToString() => $"--: tuple :-- ({string.Join(", ", Elements)})";
}

public readonly record struct TupleNodeTypeResolved(Info Info, TypedValue ValueTypeInfo, ImmutableArray<INodeTypeResolved> Elements) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new TupleNodeTypeResolved(
            Info, ValueTypeInfo.Fixate(),
            [.. Elements.Select(v => v.TypeFixation())]
        );

    public override string ToString() => $"--: tuple :-- ({string.Join(", ", Elements)})";
}