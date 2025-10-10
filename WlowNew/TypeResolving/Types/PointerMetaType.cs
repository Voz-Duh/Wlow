using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial record struct PointerMetaType(bool IsMutable, IMetaType Type) : IMetaType
{
    public static PointerMetaType Mutable(IMetaType type)
        => new(true, type);

    public static PointerMetaType Const(IMetaType type)
        => new(false, type);

    public string Name => $"^{(IsMutable ? "mut " : "")}{Type.Name}";
    public TypeMutability Mutability(Scope ctx) => TypeMutability.Copy;
    public Flg<TypeConvention> Convention(Scope ctx) => TypeConvention.Any;

    public IMetaType OperationDeref(Scope ctx, Info info) => Type;

    public IMetaType IndexAddressation(Info info, IMetaType index) => Type;

    public Nothing Binary(BinaryTypeBuilder bin) =>
        bin.Push(BinaryTypeRepr.Pointer)
        .Of(IsMutable).Unwrap(
            () => bin.Push(BinaryTypeRepr.Mutable),
            () => Nothing.Value
        )
        .Of(Type).Binary(bin);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => IMetaType.SmartTypeSelect(
            ctx, info,
            this, to,
            (from, to) =>
            {
                if (to is not PointerMetaType other)
                    return null;

                try
                {
                    return new PointerMetaType(from.IsMutable, from.Type.TemplateCast(ctx, info, other.Type));
                }
                catch
                {
                    return null;
                }
            }
        );
}
