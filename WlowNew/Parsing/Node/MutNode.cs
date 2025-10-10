using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct MutNode(Info Info, string Name, IMetaType Type, INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope.Isolated);
        var type = value.ValueTypeInfo.Type.ImplicitCast(scope, Info, Type);

        if (type.Mutability(scope) == TypeMutability.Const)
            throw CompilationException.Create(Info, "immutable type cannot be placed to mutable variable");

        var varInfo = scope.CreateVariable(Info, TypeMutability.Mutate, Name, type);

        return new MutNodeTypeResolved(Info, varInfo, Name, value);
    }
}


public readonly record struct MutNodeTypeResolved(Info Info, TypedValue ValueTypeInfo, string Name, INodeTypeResolved Value) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new MutNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Name, Value.TypeFixation());
    public override string ToString() => $"mut {Name} {ValueTypeInfo} = {Value}";
}
