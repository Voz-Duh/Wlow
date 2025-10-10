using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct UIntMetaType : IMetaType
{
    public static readonly UIntMetaType Get8 = new(8, BinaryTypeRepr.UInt8);
    public static readonly UIntMetaType Get16 = new(16, BinaryTypeRepr.UInt16);
    public static readonly UIntMetaType Get32 = new(32, BinaryTypeRepr.UInt32);
    public static readonly UIntMetaType Get64 = new(64, BinaryTypeRepr.UInt64);

    public readonly uint Bits;
    readonly BinaryTypeRepr Repr;

    public UIntMetaType() => throw new NotSupportedException("use getters");

    UIntMetaType(uint bits, BinaryTypeRepr repr)
    {
        Bits = bits;
        Repr = repr;
    }

    public string Name => $"u{Bits}";
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

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => throw IMetaType.CastError(info, this, to);

    public IMetaType OperationPlus(Scope ctx, Info info) => this;

    public IMetaType OperationInv(Scope ctx, Info info) => this;
}

