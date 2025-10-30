using System.Numerics;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct ImplicitCastNode(INode Value, IMetaType Type) : INode
{
    public Info Info => Value.Info;

    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);
        var type = value.ValueTypeInfo.Type.ImplicitCast(scope, Info, Type);
        return new ImplicitCastNodeTypeResolved(
            TypedValue.From(type),
            value,
            type
        );
    }

    public override string ToString() => $"({Value}) --: implicit :- -> {Type}";
}

public readonly record struct ImplicitCastNodeTypeResolved(TypedValue ValueTypeInfo, INodeTypeResolved Value, IMetaType Type) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation() => this;

    public override string ToString() => $"({Value}) --: implicit :- -> {Type}";
}
