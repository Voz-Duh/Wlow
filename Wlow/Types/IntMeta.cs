using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial record struct IntMeta(uint bytes, BinaryType bin) : IMetaType
{
    public bool IsGeneric() => false;
    
    public string Name(Scope sc) => $"i{bytes}";

    public LLVMTypeRef Type(Scope sc) => sc.ctx.GetIntType(bytes);

    public void Binary(BinaryWriter writer) => writer.Write((byte)bin);
}
