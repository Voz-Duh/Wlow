namespace Wlow.Node;

public readonly partial record struct EqualsValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"eq({left} == {right})";
}
