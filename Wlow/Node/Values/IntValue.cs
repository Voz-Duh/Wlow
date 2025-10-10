using System.Numerics;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct IntValue(Info info, BigInteger value) : IValue
{
    public IMetaType Type(Scope sc)
        => new IntMeta(32, BinaryType.Int32);

    public LLVMValue Compile(Scope sc)
    {
        var ty = Type(sc);
        return new(ty, LLVMConst.CreateBigIntConstant(ty.Type(sc), value));
    }

    public override string ToString() => $"num({value})";
}