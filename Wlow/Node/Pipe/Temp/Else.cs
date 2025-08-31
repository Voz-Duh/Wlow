namespace Wlow.Node;

public readonly record struct Else(Info info, IValue then, IValue other) : IValue
{
    public IMetaType Type(Scope sc) => throw new(Errors.Temp);
    public LLVMValue Compile(Scope sc) => throw new(Errors.Temp);

    public override string ToString() => throw new(Errors.Temp);
}
