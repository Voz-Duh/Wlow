using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct IdentNode(Info Info, string Name) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var variable = scope.GetVariable(Info, Name);

        return new IdentNodeTypeResolved(Info, variable, Name);
    }

    public override string ToString() => Name;
}

public readonly record struct IdentNodeTypeResolved(Info Info, TypedValue ValueTypeInfo, string Name) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation() => new IdentNodeTypeResolved(Info, ValueTypeInfo.Fixate(), Name);
    public override string ToString() => Name;
}
