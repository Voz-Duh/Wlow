using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public class TypeAnnot(
    Func<Info, Scope, IMetaType> annotBase,
    Func<string> annotName)
{
    readonly Func<Info, Scope, IMetaType> annotBase = annotBase;
    readonly Func<string> annotName = annotName;

    string? name;
    public override string ToString() => name ??= annotName();

    IMetaType? type;
    IMetaType Parse(Scope ctx, Info info) => type ??= annotBase(info, ctx);

    public static IMetaType operator >>(TypeAnnot annot, (Scope ctx, Info info) block) => annot.Parse(block.ctx, block.info);

    public static TypeAnnot Unit(string name, IMetaType type) => new((_, _) => type, () => name);
    public readonly static TypeAnnot
        Int8 = Unit("i8", IntMetaType.Get8),
        Int16 = Unit("i16", IntMetaType.Get16),
        Int32 = Unit("i32", IntMetaType.Get32),
        Int64 = Unit("i64", IntMetaType.Get64),
        Int128 = Unit("i128", IntMetaType.Get128),
        UInt8 = Unit("u8", UIntMetaType.Get8),
        UInt16 = Unit("u16", UIntMetaType.Get16),
        UInt32 = Unit("u32", UIntMetaType.Get32),
        UInt64 = Unit("u64", UIntMetaType.Get64),
        UInt128 = Unit("u128", UIntMetaType.Get128),
        Placeholder = Unit("?", PlaceHolderMetaType.Get),
        Bool = Unit("bool", BoolMetaType.Get),
        Never = Unit("never", NeverMetaType.Get),
        Void = Unit("void", VoidMetaType.Get);
    
    public static TypeAnnot HomoTuple(int count, TypeAnnot type)
        => new(
            (inf, ctx) => TupleMetaType.CreateHomogeneous(count, type >> (ctx, inf)),
            () => $"({count} {type})"
        );
    public static TypeAnnot Tuple(TypeAnnot[] elements)
        => new(
            (inf, ctx) => TupleMetaType.Create(inf, ctx, [.. elements.Select(v => v >> (ctx, inf))]),
            () => $"({string.Join(", ", elements)})"
        );

    public static TypeAnnot Ptr(TypeAnnot type)
        => new(
            (inf, ctx) => PointerMetaType.Const(inf, type >> (ctx, inf)),
            () => $"^{type}"
        );
    public static TypeAnnot MutPtr(TypeAnnot type)
        => new(
            (inf, ctx) => PointerMetaType.Mutable(inf, type >> (ctx, inf)),
            () => $"^mut {type}"
        );

    public static TypeAnnot Error(TypeAnnot type)
        => new(
            (inf, ctx) => NotMetaType.Get(type >> (ctx, inf)),
            () => $"!{type}"
        );
    public static TypeAnnot TypeOf(INode node)
        => new(
            (inf, ctx) => node.TypeResolve(ctx).ValueTypeInfo.Type,
            () => $"&({node})"
        );
}
