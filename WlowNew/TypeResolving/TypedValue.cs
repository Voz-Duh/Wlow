using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly record struct TypedValue(Mutability Mutability, IMetaType Type)
{
    public TypedValue(IMetaType Type) : this(Mutability.PlaceHolder, Type) { }

    public TypedValue TemplateCast(Scope ctx, Info info, IMetaType to)
        => new(Mutability, Type.TemplateCast(ctx, info, to));

    public override string ToString() => $"{Mutability.GetString()}{Type.Name}";

    public static TypedValue From(IMetaType Type) => new(Type.Mutability, Type);
}
