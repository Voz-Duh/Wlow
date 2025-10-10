namespace Wlow.Node;

public readonly partial record struct SubValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"sub({left} - {right})";
}
