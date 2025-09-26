using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct NeverMetaType : IMetaType
{
    public static readonly NeverMetaType Get = default;

    public ID TypeID => ID.Zero;

    public string Name => $"never";

    public Mutability Mutability => Mutability.Copy;

    public void Binary(BinaryTypeBuilder bin) =>
        bin.Push(BinaryTypeRepr.Never);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => throw IMetaType.CastError(info, this, to);
}

