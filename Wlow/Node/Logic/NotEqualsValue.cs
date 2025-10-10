namespace Wlow.Node;

public readonly partial record struct NotEqualsValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"noteq({left} != {right})";
}
