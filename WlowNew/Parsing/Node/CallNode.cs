
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct CallNode(
    Info Info,
    INode Value,
    ImmutableArray<INode> Arguments) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var value = Value.TypeResolve(scope.Isolated);
        var arguments = Arguments.Select(v => (v.Info, Node: v.TypeResolve(scope.Isolated)));
        var definition = value.ValueTypeInfo.Type.Unwrap().Call(scope, Info, [.. from v in arguments select (v.Info, v.Node.ValueTypeInfo)]);

        return new CallNodeTypeResolved(Info, new(TypeMutability.Const, definition.Type.Result), definition, value, [.. from v in arguments select v.Node]);
    }

    public override string ToString() => $"({Value}' {string.Join(", ", Arguments)})";
}


public readonly record struct CallNodeTypeResolved(
    Info Info,
    TypedValue ValueTypeInfo,
    FunctionDefinition Definition,
    INodeTypeResolved Value,
    ImmutableArray<INodeTypeResolved> Arguments) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new CallNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Definition, Value.TypeFixation(), [.. from v in Arguments select v.TypeFixation()]);
    public override string ToString() => $"--: {ValueTypeInfo.Type.Name} -- {Definition.Type.Name} = {Definition.Node} :-- ({Value}' {string.Join(", ", Arguments)})";
}
