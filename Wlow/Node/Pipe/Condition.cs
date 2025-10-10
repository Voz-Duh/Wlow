using LLVMSharp;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct Condition(Info info, IValue cond, IValue then, IValue other) : IValue
{
    public IMetaType Type(Scope sc)
    {
        // validate only
        cond.Type(sc).ImplicitCast(sc, cond.info, BoolMeta.Get);

        var a_ty = then.Type(sc);
        var b_ty = other.Type(sc);

        if (a_ty.Is<VoidMeta>() && b_ty.Is<VoidMeta>())
            return VoidMeta.Get;

        if (a_ty.Is<VoidMeta>())
            return b_ty;

        if (b_ty.Is<VoidMeta>())
            return a_ty;

        try
        {
            return b_ty.ImplicitCast(sc, other.info, a_ty);
        }
        catch
        {
            return a_ty.ImplicitCast(sc, then.info, b_ty);
        }
    }

    public LLVMValue Compile(Scope sc)
    {
        var block_then = sc.Block();
        var block_other = sc.Block();
        var block_result = sc.Block();

        var at_ty = cond.Type(sc);
        var at = cond.Compile(sc).Get(cond.info, sc);

        sc.bi.BuildCondBr(
            at_ty.ImplicitCast(sc, cond.info, at, BoolMeta.Get),
            block_then,
            block_other
        );

        sc.bi.PositionAtEnd(block_then);

        var a_ty = then.Type(sc);
        var a = then.Compile(sc);

        LLVMValueRef? a_llvm = null;
        try { a_llvm = a.Get(then.info, sc); } catch { }

        if (a_llvm != null) sc.bi.BuildBr(block_result);

        sc.bi.PositionAtEnd(block_other);

        var b_ty = other.Type(sc);
        var b = other.Compile(sc);

        LLVMValueRef? b_llvm = null;
        try { b_llvm = b.Get(other.info, sc); } catch { }

        if (b_llvm != null) sc.bi.BuildBr(block_result);

        if (a_llvm == null && b_llvm == null)
            return new(VoidMeta.Get, is_jump: true);

        if (a_llvm == null)
        {
            sc.bi.PositionAtEnd(block_result);
            return new(b_ty, val: b_llvm.Value);
        }

        if (b_llvm == null)
        {
            sc.bi.PositionAtEnd(block_result);
            return new(a_ty, val: a_llvm.Value);
        }

        try
        {
            sc.bi.PositionAtEnd(block_other);
            var b_res = b_ty.ImplicitCast(sc, other.info, b_llvm.Value, a_ty, generic_frendly: false);

            sc.bi.PositionAtEnd(block_result);
            var phi = sc.bi.BuildPhi(a_ty.Type(sc));
            phi.AddIncoming([a_llvm.Value, b_res], [block_then, block_other], 2);
            return new(a_ty, val: phi);
        }
        catch
        {
            sc.bi.PositionAtEnd(block_then);
            var a_res = a_ty.ImplicitCast(sc, other.info, a_llvm.Value, b_ty, generic_frendly: false);

            sc.bi.PositionAtEnd(block_result);
            var phi = sc.bi.BuildPhi(b_ty.Type(sc));
            phi.AddIncoming([a_res, b_llvm.Value], [block_other, block_then], 2);
            return new(b_ty, val: phi);
        }
    }

    public override string ToString() => $"if({cond}) ?> then({then}) :> else({other})";
}
