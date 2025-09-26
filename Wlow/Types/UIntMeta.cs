using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial record struct UIntMeta(uint bits, BinaryType bin) : IMetaType
{
    public bool IsGeneric() => false;
    
    public string Name(Scope sc) => $"i{bits}";

    public LLVMTypeRef Type(Scope sc) => sc.ctx.GetIntType(bits);

    public void Binary(BinaryWriter writer) => writer.Write((byte)bin);
}
