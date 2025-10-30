
using Wlow.TypeResolving;

namespace Wlow.Gen;

public readonly record struct FixTypedValue(TypeMutability Mutability, IFixType Type)
{
    public FixTypedValue(IFixType Type) : this(TypeMutability.PlaceHolder, Type) { }

    public override string ToString() => $"{Mutability.GetString()}{Type}";

    public static TypedValue From(IMetaType Type) => new(Type.Mutability, Type);
}