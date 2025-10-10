namespace Wlow.Node;

public readonly record struct Cast(Info info, IValue value, IMetaType to) : IValue
{
    public IMetaType Type(Scope sc)
        => value.Type(sc).ExplicitCast(sc, info, to);

    public LLVMValue Compile(Scope sc)
    {
        var val = value.Compile(sc);
        return new(val.type, val: val.type.ExplicitCast(sc, info, val.Get(value.info, sc), to));
    }

    public override string ToString() => $"cast({value} -> {to.Name(new())})";
}
