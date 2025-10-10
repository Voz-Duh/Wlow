using LLVMSharp.Interop;

namespace Wlow.Types;

public partial class GenericLinkMeta : IMetaType
{   
    public IMetaType CurrentType = GenericMeta.Get;

    public int IndexedFieldsCount => CurrentType.IndexedFieldsCount;
    public bool HasIndexedFields => CurrentType.HasIndexedFields;
    public LLVMValue? IndexedFieldGet(Scope sc, Info info, LLVMValueRef val, int index, bool as_pointer, bool type_only = false) => CurrentType.IndexedFieldGet(sc, info, val, index, as_pointer, type_only);

    public bool HasFields => CurrentType.HasFields;
    public LLVMValue? FieldGet(Scope sc, Info info, LLVMValueRef val, string name, bool as_pointer, bool type_only = false) => CurrentType.FieldGet(sc, info, val, name, as_pointer, type_only);

    public bool IsGeneric() => CurrentType.IsGeneric();

    public string Name(Scope sc) => CurrentType.Name(sc);

    public LLVMTypeRef Type(Scope sc) => CurrentType.Type(sc);

    public void Binary(BinaryWriter writer) => CurrentType.Binary(writer);

    public LLVMValueRef ImplicitCast(Scope sc, Info info, LLVMValueRef val, IMetaType to, bool generic_frendly=true)
        => CurrentType.ImplicitCast(sc, info, val, to, generic_frendly);

    public IMetaType ImplicitCast(Scope sc, Info info, IMetaType to)
    {
        CurrentType = CurrentType.ImplicitCast(sc, info, to);
        return this;
    }

    public LLVMValueRef ExplicitCast(Scope sc, Info info, LLVMValueRef val, IMetaType to)
        => CurrentType.ExplicitCast(sc, info, val, to);

    public IMetaType ExplicitCast(Scope sc, Info info, IMetaType to)
    {
        CurrentType = CurrentType.ExplicitCast(sc, info, to);
        return this;
    }
}
