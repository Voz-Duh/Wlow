namespace Wlow.Node;

public readonly partial record struct BitXorValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"bitxor({left} ~ {right})";
}