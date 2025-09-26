using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial struct StructMeta(Pair<string, IMetaType>[] fields, bool packed = false) : IMetaType
{
    public readonly bool packed = packed;
    public readonly int[] hashed_fields = [.. fields.Select(v => v.ident.GetHashCode())];
    public readonly Pair<string, IMetaType>[] fields = fields;

    public bool HasFields => true;
    public LLVMValue? FieldGet(Scope sc, Info info, LLVMValueRef val, string name, bool as_pointer, bool type_only = false)
    {
        int hash = name.GetHashCode();
        int i = hashed_fields.IndexOf(hash);
        if (i == -1)
            return null;

        var type = fields[i].value;

        if (type_only) return new(type);

        if (as_pointer)
            return new(type, link: sc.bi.BuildStructGEP2(Type(sc), val, (uint)i));

        return new(type, val: sc.bi.BuildExtractValue(val, (uint)i));
    }

    public bool IsGeneric() => false;

    public string Name(Scope sc)
        => $"({string.Join(", ", fields.Select(v => $"{v.value.Name(sc)} {v.ident}"))})";

    public LLVMTypeRef Type(Scope sc)
        => LLVMTypeRef.CreateStruct(
            [.. fields.Select(v => v.value.Type(sc))],
            Packed: packed
        );

    public void Binary(BinaryWriter writer)
    {
        writer.Write((byte)BinaryType.StructStart);
        writer.Write((byte)(packed ? 1 : 0));
        foreach (var e in fields)
        {
            e.value.Binary(writer);
        }
        writer.Write((byte)BinaryType.StructEnd);
    }
}
