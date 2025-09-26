using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct NeverResultNodeTypeResolved(Info Info, INodeTypeResolved Do) : INodeTypeResolved
{
    public TypedValue ValueTypeInfo => new(NeverMetaType.Get);

    public override string ToString() => $"-- never -- ({Do})";
}
