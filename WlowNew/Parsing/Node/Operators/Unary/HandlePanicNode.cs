using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct HandlePanicNode(Info Info, INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);
        if (value.ValueTypeInfo.Type.Unwrap() is not NotMetaType not)
            return value;

        return new HandlePanicNodeTypeResolved(Info, new(value.ValueTypeInfo.Mutability, not.To), value);
    }

    public override string ToString() => $"({Value})!";
}

public readonly record struct HandlePanicNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Value) : INodeTypeResolved
{
    public override string ToString() => $"({Value})!";
}
