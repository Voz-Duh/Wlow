namespace Wlow.Node;

public readonly partial record struct LowerEqualsValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"lw({left} <= {right})";
}
