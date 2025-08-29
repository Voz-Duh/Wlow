using LLVMSharp.Interop;

namespace Wlow.Types;

public partial class GenericMeta : IMetaType
{
    // make constructor private
    private GenericMeta() { }

    public bool IsGeneric() => true;
    
    public static readonly GenericMeta Get = new();

    public string Name(Scope sc) => "generic";

    public LLVMTypeRef Type(Scope sc) => throw new(Errors.Temp);

    public void Binary(BinaryWriter writer) => writer.Write((byte)BinaryType.Generic);
}
