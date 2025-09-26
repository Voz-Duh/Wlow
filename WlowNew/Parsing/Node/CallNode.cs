
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
        // TODO calling of fixed functions (there's no fixed functions right now)
        if (value.ValueTypeInfo.Type.Unwrap() is not FunctionMetaType function)
            throw CompilationException.Create(Info, $"trying to call type {value.ValueTypeInfo.Type.Name} which is not callable");

        var arguments = Arguments.Select(v => (v.Info, Node: v.TypeResolve(scope.Isolated))).ToImmutableArray();
        var definition = function.Declaration.ResolveCall(scope, Info, [.. from v in arguments select (v.Info, v.Node.ValueTypeInfo)]);

        return new CallNodeTypeResolved(Info, new(Mutability.Const, definition.Type.Result), definition, value, [.. from v in arguments select v.Node]);
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
    public override string ToString() => $"--: {ValueTypeInfo.Type.Name} -- {Definition.Type.Name} = {Definition.Node} :-- ({Value}' {string.Join(", ", Arguments)})";
}
