namespace Wlow.Node;

public readonly partial record struct GreaterEqualsValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"gt({left} >= {right})";
}
