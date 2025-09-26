namespace Wlow.Node;

public readonly partial record struct BitOrValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"bitor({left} | {right})";
}