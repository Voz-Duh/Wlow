namespace Wlow.Node;

public readonly partial record struct GreaterValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"gt({left} > {right})";
}
