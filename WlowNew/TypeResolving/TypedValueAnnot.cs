using Wlow.Parsing;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly record struct TypedValueAnnot(TypeMutability Mutability, TypeAnnot Type)
{
    public TypedValueAnnot(TypeAnnot Type) : this(TypeMutability.PlaceHolder, Type) { }

    public static TypedValue operator >>(TypedValueAnnot annot, (Scope ctx, Info info) block) => new(annot.Mutability, annot.Type >> block);
    public override string ToString() => $"{Mutability.GetString()}{Type}";

    public static TypedValueAnnot From(TypedValue Value) => new(Value.Mutability, new TypeAnnot((_, _) => Value.Type, () => Value.Type.ToString()));
}
