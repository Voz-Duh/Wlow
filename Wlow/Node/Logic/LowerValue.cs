namespace Wlow.Node;

public readonly partial record struct LowerValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"lw({left} < {right})";
}
