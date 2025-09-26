using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial struct PointerMeta : IMetaType
{
    private readonly bool mut;
    private readonly IMetaType to;

    // make constructor private
    private PointerMeta(IMetaType to, bool mut)
    {
        this.to = to;
        this.mut = mut;
    }

    public bool IsGeneric() => true;
    
    public static PointerMeta To(IMetaType to, bool mut) => new(to, mut);

    public string Name(Scope sc) => $"*{to.Name(sc)}";

    public LLVMTypeRef Type(Scope sc) => to.Type(sc).Ptr(0);

    public void Binary(BinaryWriter writer)
    {
        writer.Write((byte)BinaryType.PtrStart);
        to.Binary(writer);
        writer.Write((byte)BinaryType.PtrEnd);
    }
}
