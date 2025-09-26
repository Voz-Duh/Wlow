using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct IntMetaType : IMetaType
{
    public static readonly IntMetaType Get8 = new(8, BinaryTypeRepr.Int8);
    public static readonly IntMetaType Get16 = new(16, BinaryTypeRepr.Int16);
    public static readonly IntMetaType Get32 = new(32, BinaryTypeRepr.Int32);
    public static readonly IntMetaType Get64 = new(64, BinaryTypeRepr.Int64);

    readonly uint Bits;
    readonly BinaryTypeRepr Repr;

    public IntMetaType() => throw new NotSupportedException("use getters");

    IntMetaType(uint bits, BinaryTypeRepr repr)
    {
        Bits = bits;
        Repr = repr;
    }

    public ID TypeID => ID.Zero;

    public string Name => $"i{Bits}";

    public Mutability Mutability => Mutability.Copy;

    public void Binary(BinaryTypeBuilder bin) =>
        bin.Push(Repr);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => IMetaType.SmartTypeSelect(
            ctx,
            info,
            this, to,
            (from, to) =>
                to is IntMetaType
                ? to
                : null
        );

    public static IntMetaType GetGreater(IntMetaType a, IntMetaType b)
        => a.Bits < b.Bits ? b : a;

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => throw IMetaType.CastError(info, this, to);

    public IMetaType OperationEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.Equals);

    public IMetaType OperationNotEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.NotEquals);

    public IMetaType OperationLower(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.Lower);

    public IMetaType OperationLowerEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.LowerEquals);
            
    public IMetaType OperationGreater(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.Greater);

    public IMetaType OperationGreaterEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => BoolMetaType.Get)
            .Done(IMetaType.OpName.GreaterEquals);

    public IMetaType OperationNegate(Scope ctx, Info info) => this;

    public IMetaType OperationPlus(Scope ctx, Info info) => this;

    public IMetaType OperationInv(Scope ctx, Info info) => this;

    public IMetaType OperationSub(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Sub);

    public IMetaType OperationAdd(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Add);

    public IMetaType OperationMul(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Mul);

    public IMetaType OperationDiv(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Div);

    public IMetaType OperationMod(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Mod);

    public IMetaType OperationShr(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Shr);

    public IMetaType OperationShl(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Shl);

    public IMetaType OperationRor(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Ror);

    public IMetaType OperationRol(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.Rol);
    
    public IMetaType OperationBitwiseAnd(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.BitwiseAnd);

    public IMetaType OperationBitwiseOr(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<IntMetaType>((a, b) => GetGreater(a, b))
            .Done(IMetaType.OpName.BitwiseOr);
}

