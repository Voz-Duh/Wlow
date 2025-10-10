using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct HandleHigherNode(Info Info, INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope);
        if (value.ValueTypeInfo.Type.Unwrap() is not NotMetaType not)
            return value;

        var toType = scope.HandleError();

        // value type must not be casted to toType,
        // toType used only to generate new error of correct type! 
        return new HandleHigherNodeTypeResolved(Info, new(value.ValueTypeInfo.Mutability, not.To), value, toType);
    }

    public override string ToString() => $"({Value})?";
}

public readonly record struct HandleHigherNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Value,
    IMetaType ErrorAtType) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new HandleHigherNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Value.TypeFixation(), ErrorAtType.Fixate());

    public override string ToString() => $"({Value})?";
}
