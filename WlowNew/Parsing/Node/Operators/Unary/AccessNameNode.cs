using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct AccessNameNode(Info Info, INode Value, string Name) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);

        value.ValueTypeInfo.Type.AccessName(Info, Name);

        return new AccessNameNodeTypeResolved(Info, value.ValueTypeInfo, value, Name);
    }

    public override string ToString() => $"({Value}).{Name}";
}

public readonly record struct AccessNameNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Value,
    string Name) : INodeTypeResolved
{
    public override string ToString() => $"({Value}).{Name}";
}
