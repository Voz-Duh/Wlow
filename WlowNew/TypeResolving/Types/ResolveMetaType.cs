using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial class ResolveMetaType : IMetaType
{
    public IMetaType Current = PlaceHolderMetaType.Get;

    public string Name => Current.Name;
    public ID TypeID => Current.TypeID;
    public Mutability Mutability => Current.Mutability;

    public void Binary(BinaryTypeBuilder bin)
        => Current.Binary(bin);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ExplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ImplicitCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.TemplateCast(ctx, info, to);
}
