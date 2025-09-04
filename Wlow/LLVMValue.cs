using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow;

public readonly record struct LLVMValue(
    IMetaType type,
    LLVMValueRef val = default,
    LLVMValueRef? link = null,
    FunctionDecl function = null,
    bool is_jump = false)
{
    public LLVMValueRef Get(Info info, Scope sc)
    {
        if (type.Is<VoidMeta>())
            throw new CompileException(info, $"value type is cannot be void");
        if (link != null)
            return sc.bi.BuildLoad2(type.Type(sc), (LLVMValueRef)link);
        return val;
    }
}
