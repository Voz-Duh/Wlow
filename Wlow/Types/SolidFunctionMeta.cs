using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial struct SolidFunctionMeta(IMetaType[] arguments, IMetaType result) : IMetaType
{
    public readonly IMetaType[] arguments = arguments;
    public readonly IMetaType result = result;

    public bool IsGeneric() => false;
    
    public string Name(Scope sc)
        => $"'({string.Join(", ", arguments.Select(v => v.Name(sc)))}) {result.Name(sc)}";

    public LLVMTypeRef LLVMBaseType(Scope sc)
        => LLVMTypeRef.CreateFunction(
            result.Type(sc),
            [.. arguments.Select(v => v.Type(sc))],
            IsVarArg: false
        );

    public LLVMTypeRef Type(Scope sc) => LLVMTypeRef.CreatePointer(LLVMBaseType(sc), 0);

    public void Binary(BinaryWriter writer)
    {
        writer.Write((byte)BinaryType.SolidFunctionStart);
        foreach (var e in arguments)
        {
            e.Binary(writer);
        }
        writer.Write((byte)BinaryType.FunctionEnd);
    }
}
