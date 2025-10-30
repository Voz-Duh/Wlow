using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct IntMetaType : IMetaType
{
    public static readonly IntMetaType Get8 = new(8, BinaryTypeRepr.Int8);
    public static readonly IntMetaType Get16 = new(16, BinaryTypeRepr.Int16);
    public static readonly IntMetaType Get32 = new(32, BinaryTypeRepr.Int32);
    public static readonly IntMetaType Get64 = new(64, BinaryTypeRepr.Int64);
    public static readonly IntMetaType Get128 = new(128, BinaryTypeRepr.Int128);

    public readonly uint Bits;
    public readonly uint Bytes;
    readonly BinaryTypeRepr Repr;

    public IntMetaType() => throw new NotSupportedException("use getters");

    IntMetaType(uint bits, BinaryTypeRepr repr)
    {
        Bits = bits;
        Bytes = bits / 8;
        Repr = repr;
    }


    public override string ToString() => $"i{Bits}";
    public TypeMutability Mutability => TypeMutability.Copy;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin, Info info) => bin.Push(Repr);

    public bool IsKnown => true;
    public Opt<uint> ByteSize => Bytes;

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

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => IMetaType.SmartTypeSelect(ctx, info, this, to, (_, _) => null, is_template: true, repeat: repeat);

    public IMetaType OperationNegate(Scope ctx, Info info) => this;

    public IMetaType OperationPlus(Scope ctx, Info info) => this;

    public IMetaType OperationInv(Scope ctx, Info info) => this;
}

