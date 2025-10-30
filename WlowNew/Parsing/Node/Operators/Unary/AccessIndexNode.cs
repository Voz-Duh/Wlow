using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct AccessIndexNode(Info Info, INode Value, int Index) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);

        var type = value.ValueTypeInfo.Type.AccessIndex(scope, Info, Index);

        return new AccessIndexNodeTypeResolved(Info, TypedValue.From(type), value, Index);
    }

    public override string ToString() => $"({Value}).{Index}";
}

public readonly record struct AccessIndexNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Value,
    int Index) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new AccessIndexNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Value.TypeFixation(), Index);

    public override string ToString() => $"({Value}).{Index}";
}
