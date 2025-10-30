using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial record struct PointerMetaType(bool IsMutable, IMetaType Type, bool Fixated = false) : IMetaType
{
    static Nothing CheckType(Info info, IMetaType type)
        => type.IsKnown
        ? Nothing.Value
        : throw CompilationException.Create(info, $"type {type} cannot be used as pointer type because it's a template");

    public static PointerMetaType Mutable(Info info, IMetaType type)
        => CheckType(info, type).Of(new PointerMetaType(true, type));

    public static PointerMetaType Const(Info info, IMetaType type)
        => CheckType(info, type).Of(new PointerMetaType(false, type));

    public override string ToString() => $"^{(IsMutable ? "mut " : "")}{Type}";
    public bool IsKnown => true;
    public Opt<uint> ByteSize => Settings.TargetPlatform.PtrSize();
    public TypeMutability Mutability => TypeMutability.Copy;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    public IMetaType? UnwrapFn()
        => Fixated
        ? null
        : new PointerMetaType(IsMutable, Type.Fixate(), true);

    public IMetaType OperationDeref(Scope ctx, Info info) => Type;

    public IMetaType IndexAddressation(Info info, IMetaType index) => Type;

    public Nothing Binary(BinaryTypeBuilder bin, Info info) =>
        bin.Push(BinaryTypeRepr.Pointer)
        .Of(IsMutable).Unwrap(
            () => bin.Push(BinaryTypeRepr.Mutable),
            () => Nothing.Value
        )
        .Of(Type).Binary(bin, info);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
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
            },
            is_template: true,
            repeat: repeat
        );
}
