namespace Wlow.TypeResolving;

public enum TypeMutability
{
    PlaceHolder,
    Copy,
    Const,
    // use only to define variables
    Mutate
}

public static class MutabilityHelper
{
    public static string GetString(this TypeMutability mutability)
        => mutability switch
        {
            TypeMutability.Const => "let ",
            _ => "",
        };
}
