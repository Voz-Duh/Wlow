using LLVMSharp.Interop;

namespace Wlow.Types;

public partial class BoolMeta : IMetaType
{
    // make constructor private
    private BoolMeta() { }

    public static readonly BoolMeta Get = new();

    public bool IsGeneric() => false;

    public string Name(Scope sc) => "bool";

    public LLVMTypeRef Type(Scope sc) => sc.ctx.Int1Type;

    public void Binary(BinaryWriter writer) => writer.Write((byte)BinaryType.Bool);
}
