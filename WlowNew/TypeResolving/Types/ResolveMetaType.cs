using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial class ResolveMetaType : IMetaType
{
    public IMetaType Current = PlaceHolderMetaType.Get;

    public string Name => Current.Name;
    public TypeMutability Mutability(Scope ctx) => Current.Mutability(ctx);
    public Flg<TypeConvention> Convention(Scope ctx) => Current.Convention(ctx);

    public Nothing Binary(BinaryTypeBuilder bin)
        => Current.Binary(bin);

    public IMetaType? FixateFn()
        => Current;

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ExplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ImplicitCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.TemplateCast(ctx, info, to);
    
    public IMetaType AccessIndex(Scope ctx, Info info, int index)
        => Current.AccessIndex(ctx, info, index);
    public IMetaType AccessName(Scope ctx, Info info, string name)
        => Current.AccessName(ctx, info, name);
    public IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
        => Current.IndexAddressation(ctx, info, index);

    public bool Callable(Scope ctx) => Current.Callable(ctx);
    public FunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
        => Current.Call(ctx, info, args);
}
