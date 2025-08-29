using LLVMSharp.Interop;

namespace Wlow.Types;

public partial class GenericLinkMeta : IMetaType
{
    public IMetaType CurrentType = GenericMeta.Get;

    public bool IsGeneric() => CurrentType.IsGeneric();
    
    public string Name(Scope sc) => CurrentType.Name(sc);

    public LLVMTypeRef Type(Scope sc) => CurrentType.Type(sc);

    public void Binary(BinaryWriter writer) => CurrentType.Binary(writer);
}
