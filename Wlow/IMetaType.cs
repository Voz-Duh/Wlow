using System.Diagnostics.CodeAnalysis;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow;

public interface IMetaType
{
    int IndexedFieldsCount => 0;
    bool HasIndexedFields => false;
    LLVMValue? IndexedFieldGet(Scope sc, Info info, LLVMValueRef val, int index, bool as_pointer, bool type_only = false) => null;

    bool HasFields => false;
    LLVMValue? FieldGet(Scope sc, Info info, LLVMValueRef val, string name, bool as_pointer, bool type_only = false) => null;

    bool IsGeneric();
    void Binary(BinaryWriter writer);
    LLVMValueRef ImplicitCast(Scope sc, Info info, LLVMValueRef val, IMetaType to, bool generic_frendly = true);
    IMetaType ImplicitCast(Scope sc, Info info, IMetaType to);
    LLVMValueRef ExplicitCast(Scope sc, Info info, LLVMValueRef val, IMetaType to);
    IMetaType ExplicitCast(Scope sc, Info info, IMetaType to);
    LLVMTypeRef Type(Scope sc);
    string Name(Scope sc);
}

public static class MetaTypeHelper
{
    public static bool SameWith(this TupleMeta a, TupleMeta b) => a.AsBin() == b.AsBin();

    public static bool Is<T>(this IMetaType type)
    where T : IMetaType
    {
        type = type.Unwrap();
        return type is T;
    }

    public static bool Is<T>(this IMetaType type, [MaybeNullWhen(true)] out T result)
    where T : IMetaType
    {
        type = type.Unwrap();
        if (type is T meta)
        {
            result = meta;
            return true;
        }
        result = default;
        return false;
    }

    public static bool IsNot<T>(this IMetaType type)
    where T : IMetaType
    => !Is<T>(type);

    public static bool IsNot<T>(this IMetaType type, [MaybeNullWhen(false)] out T result)
    where T : IMetaType
    => !Is<T>(type, out result);

    public static IMetaType Unwrap(this IMetaType type)
    {
        while (type is GenericLinkMeta meta) type = meta.CurrentType;
        return type;
    }

    public static string AsBin(this IMetaType type)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);
        type.Binary(writer);
        if ((memory.Position & 1) == 1) writer.Write(0);
        unsafe
        {
            fixed (byte* bytes = memory.GetBuffer())
            {
                return new string((char*)(void*)bytes, 0, ((int)memory.Position) >> 1);
            }
        }
    }
}
