using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct SetNode(
    Info Info,
    INode Acceptor,
    INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var acceptor = Acceptor.TypeResolve(scope.Isolated);
        var value = Value.TypeResolve(scope.Isolated);
        
        if (!acceptor.ValueTypeInfo.Type.Convention(scope) << TypeConvention.Set)
        {
            throw CompilationException.Create(Acceptor.Info, $"type {acceptor.ValueTypeInfo.Type} of left side is not suitable to be assigned");
        }

        return new SetNodeTypeResolved(Info, acceptor.ValueTypeInfo, acceptor, value);
    }

    public override string ToString() => $"{Acceptor} = {Value}";
}

public readonly record struct SetNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Acceptor,
    INodeTypeResolved Value) : INodeTypeResolved
{
    public override string ToString() => $"{Acceptor} = {Value}";
    public INodeTypeResolved TypeFixation()
        => new SetNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Acceptor.TypeFixation(), Value.TypeFixation());
}
