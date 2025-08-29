using LLVMSharp.Interop;

namespace Wlow;

public interface IMetaType
{
    bool IsGeneric();
    void Binary(BinaryWriter writer);
    LLVMValueRef ImplicitCast(Scope sc, Info info, LLVMValueRef val, IMetaType to, bool generic_frendly=true);
    IMetaType ImplicitCast(Scope sc, Info info, IMetaType to);
    LLVMTypeRef Type(Scope sc);
    string Name(Scope sc);
}

public static class MetaTypeHelper
{
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
