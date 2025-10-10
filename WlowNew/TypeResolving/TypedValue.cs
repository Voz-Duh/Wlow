using System.Diagnostics.CodeAnalysis;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly record struct TypedValue(TypeMutability Mutability, IMetaType Type)
{
    public TypedValue(IMetaType Type) : this(TypeMutability.PlaceHolder, Type) { }

    public TypedValue Fixate()
        => new(Mutability, Type.Fixate());

    public TypedValue TemplateCast(Scope ctx, Info info, IMetaType to)
        => new(Mutability, Type.TemplateCast(ctx, info, to));

    public override string ToString() => $"{Mutability.GetString()}{Type.Name}";

    public static TypedValue From(Scope ctx, IMetaType Type) => new(Type.Mutability(ctx), Type);
}
