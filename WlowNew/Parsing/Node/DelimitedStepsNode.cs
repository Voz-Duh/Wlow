
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct DelimitedStepsNode(ImmutableArray<INode> Steps, INode Final) : INode
{
    public Info Info => Final.Info;

    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var resolvedSteps = new INodeTypeResolved[Steps.Length];
        for (int i = 0; i < Steps.Length; i++)
            resolvedSteps[i] = Steps[i].TypeResolve(scope);

        var resolvedFinal = Final.TypeResolve(scope);

        return new DelimitedStepsNodeTypeResolved([.. resolvedSteps], resolvedFinal);
    }

    public override string ToString() => $"({string.Join("; ", Steps)}; {Final})";
}

public readonly record struct DelimitedStepsNodeTypeResolved(ImmutableArray<INodeTypeResolved> Steps, INodeTypeResolved Final) : INodeTypeResolved
{
    public TypedValue ValueTypeInfo => Final.ValueTypeInfo;
    public INodeTypeResolved TypeFixation()
        => new DelimitedStepsNodeTypeResolved([.. Steps.Select(s => s.TypeFixation())], Final.TypeFixation());

    public override string ToString() => $"({string.Join("; ", Steps)}; {Final})";
}