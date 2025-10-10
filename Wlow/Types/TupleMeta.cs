using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial struct TupleMeta(IMetaType[] elements, bool packed = false) : IMetaType
{
    public readonly bool packed = packed;
    public readonly IMetaType[] elements = elements;

    public int IndexedFieldsCount => elements.Length;
    public bool HasIndexedFields => true;
    public LLVMValue? IndexedFieldGet(Scope sc, Info info, LLVMValueRef val, int index, bool as_pointer, bool type_only = false)
    {
        // cannot be out of range
        if (index < 0 || index >= elements.Length)
            return null;

        var type = elements[index];

        if (type_only) return new(type);

        if (as_pointer)
            return new(type, link: sc.bi.BuildStructGEP2(Type(sc), val, (uint)index));

        return new(type, val: sc.bi.BuildExtractValue(val, (uint)index));
    }

    public bool IsGeneric() => false;

    public string Name(Scope sc)
        => $"({string.Join(", ", elements.Select(v => v.Name(sc)))})";

    public LLVMTypeRef Type(Scope sc)
        => LLVMTypeRef.CreateStruct(
            [.. elements.Select(v => v.Type(sc))],
            Packed: packed
        );

    public void Binary(BinaryWriter writer)
    {
        writer.Write((byte)BinaryType.TupleStart);
        writer.Write((byte)(packed ? 1 : 0));
        foreach (var e in elements)
        {
            e.Binary(writer);
        }
        writer.Write((byte)BinaryType.TupleEnd);
    }
}
