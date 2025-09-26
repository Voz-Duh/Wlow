namespace Wlow.TypeResolving;

public enum Mutability
{
    PlaceHolder,
    Copy,
    Mutate,
    Const
}

public static class MutabilityHelper
{
    public static string GetString(this Mutability mutability)
        => mutability switch
        {
            Mutability.Const => "let ",
            Mutability.Mutate => "mut ",
            _ => "",
        };
}
