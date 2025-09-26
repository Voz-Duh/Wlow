using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct ErrorToNodeTypeResolved(Info Info, INodeTypeResolved Value) : INodeTypeResolved
{
    public TypedValue ValueTypeInfo => Value.ValueTypeInfo;

    public override string ToString() => $"({Value})?";
}
