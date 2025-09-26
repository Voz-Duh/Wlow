using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct LetNode(Info Info, string Name, IMetaType Type, INode? Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        if (Value is null)
        {
            scope.CreateLabel(Info, Name);

            return new LabelNodeTypeResolved();
        }

        var value = Value.TypeResolve(scope.Isolated);
        var type = value.ValueTypeInfo.Type.ImplicitCast(scope, Info, Type);

        if (type.Mutability == Mutability.Mutate)
            throw CompilationException.Create(Info, "mutable type cannot be placed to immutable variable");

        var varInfo = scope.CreateVariable(Info, Mutability.Const, Name, type);

        return new LetNodeTypeResolved(Info, varInfo, Name, value);
    }

    public override string ToString() => Value is null ? $"let {Name} {Type.Name}" : $"let {Name} {Type.Name} = {Value}";
}

public readonly record struct LabelNodeTypeResolved(Info Info, string Name) : INodeTypeResolved
{
    // TODO void type here
    public TypedValue ValueTypeInfo => new();

    public override string ToString() => $"let {Name} {ValueTypeInfo}";
}


public readonly record struct LetNodeTypeResolved(Info Info, TypedValue ValueTypeInfo, string Name, INodeTypeResolved Value) : INodeTypeResolved
{
    public override string ToString() => $"let {Name} {ValueTypeInfo} = {Value}";
}
