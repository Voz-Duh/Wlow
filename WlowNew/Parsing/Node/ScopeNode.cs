
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct ScopeNode(Info Info, INode Value) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        return new ScopeNodeTypeResolved(Value.TypeResolve(scope.New));
    }

    public override string ToString() => $"(--: new scope :-- {Value})";
}

public readonly record struct ScopeNodeTypeResolved(INodeTypeResolved Value) : INodeTypeResolved
{
    public TypedValue ValueTypeInfo => Value.ValueTypeInfo;
    public INodeTypeResolved TypeFixation()
        => new ScopeNodeTypeResolved(Value.TypeFixation());

    public override string ToString() => $"(--: new scope :-- {Value})";
}