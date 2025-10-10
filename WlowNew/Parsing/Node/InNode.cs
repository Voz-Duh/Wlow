using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct InNode(Info Info, string Name) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        scope.ValidateLabel(Info, Name);

        return new InNodeTypeResolved(Info, TypedValue.From(scope, NeverMetaType.Get), Name);
    }

    public override string ToString() => $"in {Name}";
}

public readonly record struct InNodeTypeResolved(Info Info, TypedValue ValueTypeInfo, string Name) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation() => this;
    public override string ToString() => $"in {Name}";
}
