using System.Collections.Immutable;
using Wlow.Parsing;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial record class TypeOfMetaType(INode Node) : IMetaType
{
    IMetaType? _current;
    public IMetaType CheckedCurrent => _current ?? throw new AggregateException("unvalid use of CheckedCurrent");
    public IMetaType Current(Scope ctx) => _current ??= Node.TypeResolve(ctx).ValueTypeInfo.Type;
    public readonly ID Identifier = ID.Unqiue;

    public string Name => _current?.ToString() ?? $"$({Node})";
    public TypeMutability Mutability(Scope ctx) => Current(ctx).Mutability(ctx);
    public Flg<TypeConvention> Convention(Scope ctx) => Current(ctx).Convention(ctx);

    public Nothing Binary(BinaryTypeBuilder bin)
        => bin.Push(BinaryTypeRepr.TypeOf).Of(bin).Push(Identifier);
    
    public IMetaType? FixateFn()
        => CheckedCurrent;

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => Current(ctx).ExplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => Current(ctx).ImplicitCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => Current(ctx).TemplateCast(ctx, info, to);
    
    public IMetaType AccessIndex(Scope ctx, Info info, int index)
        => Current(ctx).AccessIndex(ctx, info, index);
    public IMetaType AccessName(Scope ctx, Info info, string name)
        => Current(ctx).AccessName(ctx, info, name);
    public IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
        => Current(ctx).IndexAddressation(ctx, info, index);

    public bool Callable(Scope ctx) => Current(ctx).Callable(ctx);
    public FunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
        => Current(ctx).Call(ctx, info, args);
}
