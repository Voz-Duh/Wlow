using LLVMSharp.Interop;

namespace Wlow;

[Flags]
public enum VariableFlags
{
    None,
    Jumpable,
    Setable,
    Label,
    TypeOnly
}

public readonly record struct Variable(
    IMetaType type,
    LLVMValueRef llvm,
    LLVMBasicBlockRef block,
    VariableFlags flags,
    FunctionDecl function = null)
{
    public Variable Include(VariableFlags flags)
        => new(type, llvm, block, this.flags | flags, function);
    public Variable Exclude(VariableFlags flags)
        => new(type, llvm, block, this.flags & ~flags, function);
    public const VariableFlags std_mode =
        VariableFlags.Jumpable
        | VariableFlags.Setable;
}
