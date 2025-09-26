namespace Wlow.Node;

public readonly partial record struct BitAndValue(Info info, IValue left, IValue right) : IValue
{
    void T(Scope sc) => sc.bi.BuildOr(left.Compile(sc).Get(left.info, sc), right.Compile(sc).Get(right.info, sc));
    public override string ToString() => $"bitand({left} & {right})";
}