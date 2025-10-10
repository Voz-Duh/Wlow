using LLVMSharp.Interop;

namespace Wlow.Types;

public partial class VoidMeta : IMetaType
{
    // make constructor private
    private VoidMeta() { }

    public bool IsGeneric() => false;

    public static readonly VoidMeta Get = new();

    public string Name(Scope sc) => "void";

    public LLVMTypeRef Type(Scope sc) => sc.ctx.VoidType;

    public void Binary(BinaryWriter writer) => throw new(Errors.Temp);
}
