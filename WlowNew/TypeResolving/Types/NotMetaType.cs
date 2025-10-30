using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct NotMetaType : IMetaType
{
    public static NotMetaType Get(IMetaType to) => new(to);

    public static readonly IMetaType ErrorType = IntMetaType.Get8;
    public readonly IMetaType To;
    readonly bool Fixated;

    public override string ToString() => $"!{To}";
    public bool IsKnown => To.IsKnown;
    public Opt<uint> ByteSize => ErrorType.ByteSize + To.ByteSize;
    public TypeMutability Mutability => To.Mutability;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    NotMetaType(IMetaType to, bool fixated = false)
    {
        To = to;
        Fixated = fixated;
    }

    public Nothing Binary(BinaryTypeBuilder bin, Info info)
        => bin
        .Push(BinaryTypeRepr.NotType)
        .Of(To).Binary(bin, info);

    public IMetaType? UnwrapFn()
        => Fixated
        ? null
        : new NotMetaType(To.Fixate(), true);
}
