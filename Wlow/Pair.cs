namespace System;

public readonly record struct Pair<TIdent, TValue>(TIdent ident, TValue value);

public static class Pair
{
    public static Pair<TIdent, TValue> From<TIdent, TValue>(TIdent ident, TValue value) => new(ident, value);
}
