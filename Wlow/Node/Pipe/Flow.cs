namespace Wlow.Node;

public readonly record struct Flow(Info info, IValue body, IValue next) : IValue
{
    public IMetaType Type(Scope sc)
    {
        body.Type(sc);
        return next.Type(sc);
    }
    public LLVMValue Compile(Scope sc)
    {
        body.Compile(sc);
        return next.Compile(sc);
    }

    public override string ToString() => $"first({body}) |> second({next})";
}
