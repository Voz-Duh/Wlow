using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial class WeakRefMetaType : IMetaType
{
    public static WeakRefMetaType From(IMetaType Type) => new(Type);

    public IMetaType Current;

    WeakRefMetaType(IMetaType current) => Current = current;

    public override string ToString() => Current.ToString()!;
    public bool IsKnown => Current.IsKnown;
    public Opt<uint> ByteSize => Current.ByteSize;
    public TypeMutability Mutability => Current.Mutability;
    public Flg<TypeConvention> Convention => Current.Convention;

    public Nothing Binary(BinaryTypeBuilder bin, Info info)
        => Current.Binary(bin, info);

    public IMetaType? UnwrapFn()
        => Current;

    public IMetaType? UnweakFn()
        => Current;

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ExplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => Current = Current.ImplicitCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => Current = Current.TemplateCast(ctx, info, to, repeat);
    
    public IMetaType AccessIndex(Scope ctx, Info info, int index)
        => Current.AccessIndex(ctx, info, index);
    public IMetaType AccessName(Scope ctx, Info info, string name)
        => Current.AccessName(ctx, info, name);
    public IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
        => Current.IndexAddressation(ctx, info, index);

    public bool Callable(Scope ctx) => Current.Callable(ctx);
    public IFunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
        => Current.Call(ctx, info, args);
}
