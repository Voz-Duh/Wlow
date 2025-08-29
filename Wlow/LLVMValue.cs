using LLVMSharp;
using LLVMSharp.Interop;

namespace Wlow;

public readonly record struct LLVMValue(
    IMetaType type,
    LLVMValueRef val = default,
    LLVMValueRef? link = null,
    FunctionDecl function = null,
    bool is_jump = false,
    Info info = default)
{
    public LLVMValueRef Get(Scope sc)
    {
        if (is_jump)
            throw new($"{info} value is cannot be loop");
        if (link != null)
            return sc.bi.BuildLoad2(type.Type(sc), (LLVMValueRef)link);
        return val;
    }
}
