using LLVMSharp.Interop;

namespace Wlow.Types;

public readonly partial struct FunctionMeta(IMetaType[] arguments, IMetaType result, FunctionDecl declaration = null) : IMetaType
{
    public readonly IMetaType[] arguments = arguments;
    public readonly IMetaType result = result;

    private readonly FunctionDecl declaration = declaration;
    public FunctionDecl Declaration
    {
        get
        {
            if (declaration is null)
                throw new("null declaration on function meta type is not a normal state, compilator works bad");
            return declaration;
        }
    }

    public FunctionMeta IncludeDeclaration(FunctionDecl declaration)
        => new(arguments, result, declaration);

    public bool IsGeneric() => arguments.Any(v => v.IsGeneric());
    
    public string Name(Scope sc)
        => $"'({string.Join(", ", arguments.Select(v => v.Name(sc)))}) {result}";

    public LLVMTypeRef LLVMBaseType(Scope sc)
        => LLVMTypeRef.CreateFunction(
            result.Type(sc),
            [.. arguments.Select(v => v.Type(sc))],
            IsVarArg: false
        );

    public LLVMTypeRef Type(Scope sc) => LLVMTypeRef.CreatePointer(LLVMBaseType(sc), 0);

    public void Binary(BinaryWriter writer)
    {
        writer.Write((byte)BinaryType.FunctionStart);
        foreach (var e in arguments)
        {
            e.Binary(writer);
        }
        if (declaration is not null)
        {
            writer.Write((byte)BinaryType.FunctionDecl);
            writer.Write(Declaration.UniqueNumber.ToByteArray());
        }
        writer.Write((byte)BinaryType.FunctionEnd);
    }
}
