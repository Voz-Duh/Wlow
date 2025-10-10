using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct IntMetaType : IMetaType
{
    public static readonly IntMetaType Get8 = new(8, BinaryTypeRepr.Int8);
    public static readonly IntMetaType Get16 = new(16, BinaryTypeRepr.Int16);
    public static readonly IntMetaType Get32 = new(32, BinaryTypeRepr.Int32);
    public static readonly IntMetaType Get64 = new(64, BinaryTypeRepr.Int64);

    public readonly uint Bits;
    readonly BinaryTypeRepr Repr;

    public IntMetaType() => throw new NotSupportedException("use getters");

    IntMetaType(uint bits, BinaryTypeRepr repr)
    {
        Bits = bits;
        Repr = repr;
    }


    public string Name => $"i{Bits}";
    public TypeMutability Mutability(Scope ctx) => TypeMutability.Copy;
    public Flg<TypeConvention> Convention(Scope ctx) => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin) =>
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

    public IMetaType OperationNegate(Scope ctx, Info info) => this;

    public IMetaType OperationPlus(Scope ctx, Info info) => this;

    public IMetaType OperationInv(Scope ctx, Info info) => this;
}

