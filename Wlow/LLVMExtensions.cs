namespace LLVMSharp.Interop;

public static class LLVMExtensions
{
    public static bool TryForModule(this LLVMMCJITCompilerOptions Self, LLVMModuleRef Module, out LLVMExecutionEngineRef OutEngine, out string OutMessage)
    {
        unsafe
        {
            LLVMOpaqueExecutionEngine* engine;
            sbyte* pMessage = null;
            var result = LLVM.CreateMCJITCompilerForModule(&engine, Module, &Self, 0, &pMessage);

            if (pMessage == null)
            {
                OutMessage = string.Empty;
            }
            else
            {
                OutMessage = SpanExtensions.AsString(pMessage);
            }
            OutEngine = engine;

            return result == 0;
        }
    }
}