namespace Wlow.Node;

public readonly partial record struct AddValue(Info info, IValue left, IValue right) : IValue
{
    public override string ToString() => $"add({left} + {right})";
}
