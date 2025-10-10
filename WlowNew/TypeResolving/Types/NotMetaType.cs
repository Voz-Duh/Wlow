using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct NotMetaType : IMetaType
{
    public static NotMetaType Get(IMetaType to) => new(to);

    public readonly IMetaType To;

    public string Name => $"!{To.Name}";
    public TypeMutability Mutability(Scope ctx) => To.Mutability(ctx);
    public Flg<TypeConvention> Convention(Scope ctx) => TypeConvention.Any;

    NotMetaType(IMetaType to) => To = to;

    public Nothing Binary(BinaryTypeBuilder bin) =>
        bin.Push(BinaryTypeRepr.Never);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => throw IMetaType.CastError(info, this, to);
}
