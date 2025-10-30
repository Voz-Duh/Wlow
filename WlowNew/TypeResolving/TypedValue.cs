using System.Diagnostics.CodeAnalysis;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly record struct TypedValue(TypeMutability Mutability, IMetaType Type)
{
    public TypedValue(IMetaType Type) : this(TypeMutability.PlaceHolder, Type) { }

    public TypedValue Fixate()
        => new(Mutability, Type.Fixate());

    public TypedValue Unweak()
        => new(Mutability, Type.Unweak());

    public TypedValue TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => new(Mutability, Type.TemplateCast(ctx, info, to, repeat));

    public override string ToString() => $"{Mutability.GetString()}{Type}";

    public static TypedValue From(IMetaType Type) => new(Type.Mutability, Type);
}
