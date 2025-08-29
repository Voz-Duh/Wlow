namespace Wlow.Node;

public readonly record struct Else(Info info, IValue then, IValue other) : IValue
{
    public IMetaType Type(Scope sc) => throw new("this structure is temporary and cannot be used");
    public LLVMValue Compile(Scope sc) => throw new("this structure is temporary and cannot be used");

    public override string ToString() => throw new("this structure is temporary and cannot be used");
}
