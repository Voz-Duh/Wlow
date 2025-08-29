using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace LLVMSharp.Interop;

public static class LLVMConst
{
    public static LLVMValueRef CreateBigIntConstant(LLVMTypeRef intType, BigInteger value)
    {
        unsafe
        {
            var s = Encoding.UTF8.GetBytes(value.ToString());
            fixed (byte* c = s)
            {
                return LLVM.ConstIntOfStringAndSize(intType, (sbyte*)c, (uint)s.Length, 10);
            }
        }
    }

    public static LLVMValueRef CreateDecimalConstant(LLVMTypeRef intType, decimal value)
    {
        unsafe
        {
            var s = Encoding.UTF8.GetBytes(value.ToString());
            fixed (byte* c = s)
            {
                return LLVM.ConstRealOfStringAndSize(intType, (sbyte*)c, (uint)s.Length);
            }
        }
    }

    [DllImport("libLLVM", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LLVMBuildIntCast2", ExactSpelling = true)]
    static extern unsafe LLVMOpaqueValue* BuildIntCast2(LLVMOpaqueBuilder* _, LLVMOpaqueValue* Val, LLVMOpaqueType* DestTy, int IsSigned, sbyte* Name);

    public static LLVMValueRef BuildIntCast2(this LLVMBuilderRef builder, LLVMValueRef val, LLVMTypeRef dest_ty, bool is_signed, string name = "")
    => builder.BuildIntCast2(val, dest_ty, is_signed, name.AsSpan());

    public static LLVMValueRef BuildIntCast2(this LLVMBuilderRef builder, LLVMValueRef val, LLVMTypeRef dest_ty, bool is_signed, ReadOnlySpan<char> name)
    {
        unsafe
        {
            using var marshaledName = new MarshaledString(name);
            return BuildIntCast2(builder, val, dest_ty, is_signed ? 1 : 0, marshaledName.Value);
        }
    }
}
