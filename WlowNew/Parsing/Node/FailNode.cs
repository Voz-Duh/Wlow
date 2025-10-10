using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct FailNode(Info Info, INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);

        var toType = scope.HandleError();

        // value type must not be casted to toType,
        // toType used only to generate new error of correct type! 
        return new FailNodeTypeResolved(Info, TypedValue.From(scope, NeverMetaType.Get), value, toType);
    }

    public override string ToString() => $"fail ({Value})";
}

public readonly record struct FailNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Value,
    IMetaType ErrorToType) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new FailNodeTypeResolved(Info, ValueTypeInfo, Value.TypeFixation(), ErrorToType);
    public override string ToString() => $"fail ({Value})";
}
