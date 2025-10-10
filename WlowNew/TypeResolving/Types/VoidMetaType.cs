using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct VoidMetaType : IMetaType
{
    public static readonly VoidMetaType Get = default;

    public string Name => "void";
    public TypeMutability Mutability(Scope ctx) => TypeMutability.Copy;
    public Flg<TypeConvention> Convention(Scope ctx) => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin) =>
        bin.Push(BinaryTypeRepr.Never);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => to.Unwrap() is VoidMetaType ? to : throw IMetaType.CastError(info, this, to);
}

