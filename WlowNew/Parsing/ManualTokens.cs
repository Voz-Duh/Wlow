using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.Parsing;

public ref struct ManualTokens
{
    public static ManualTokens Create(Token Context, ReadOnlySpan<Token> Tokens) => new(Context, Tokens);

    public delegate T Step<T>(ref ManualTokens tokens, Token token);
    public delegate T DoFunction<T>(ref ManualTokens tokens);
    public delegate T SplitedStep<T>(ReadOnlySpan<Token> tokens, Token token);

    public readonly ReadOnlySpan<Token> Tokens;
    Token BaseContext;
    int i, start, end;

    ManualTokens(Token context, ReadOnlySpan<Token> tokens)
    {
        BaseContext = context;
        Tokens = tokens;
        end = tokens.Length;
        start = 0;
        i = 0;
    }

    public readonly bool Overflow => i >= end;
    public readonly Token Current => Tokens[i];
    public readonly bool Moved => i != start;
    public readonly Token Context => i == 0 | Tokens.Length == 0 ? BaseContext : Overflow ? Tokens[^1] : Tokens[i - 1];

    T ElseCompute<T>(Step<T>? Else) => Else is null ? default! : Else.Invoke(ref this, Context);

    public readonly ReadOnlySpan<Token> AllRightIncludeCurrent() => Tokens[i..];
    public readonly ReadOnlySpan<Token> AllRight() => Tokens[(i + 1)..];

    public Nothing ToEnd() => (i = end).Ignore();

    public T Get<T>(TokenType Token, Step<T>? Else, Step<T> Fail, Step<T> Success)
    {
        if (Overflow) return ElseCompute(Else);
        var cur = Current;

        if (cur.type == Token)
        {
            i++;
            var res = Success(ref this, cur);
            return res;
        }

        return Fail(ref this, cur);
    }

    public T Switch<T>(Step<T>? Else, Step<T> Default, params IEnumerable<(TokenType type, Step<T> get)> Matches)
    {
        if (Overflow) return ElseCompute(Else);
        var cur = Current;

        foreach (var (type, get) in Matches)
            if (cur.type == type)
            {
                i++;
                var res = get(ref this, cur);
                return res;
            }

        return Default(ref this, cur);
    }

    public Or<(TSuccess value, TAfter after), TFail> Until<TSuccess, TAfter, TFail>(
        IEnumerable<TokenType> Tokens,
        SplitedStep<TSuccess> Success,
        Step<TAfter> After,
        Step<TFail> Fail)
    {
        var back = i;
        for (; i < this.Tokens.Length; i++)
        {
            var cur = Current;
            if (Tokens.Contains(cur.type))
            {
                var value = Success(this.Tokens[back..i], Context);

                i++;
                var after = After(ref this, Context);
                return (value, after);
            }
        }
        i = back;

        return Fail(ref this, Context);
    }

    public Or<(TSuccess value, TAfter after), TFail> UntilWithNot<TSuccess, TAfter, TFail>(
        IEnumerable<TokenType> Tokens,
        IEnumerable<TokenType> NotTokens,
        SplitedStep<TSuccess> Success,
        Step<TAfter> After,
        Step<TFail> Fail)
    {
        var back = i;
        for (; i < this.Tokens.Length; i++)
        {
            var cur = Current;
            if (Tokens.Contains(cur.type))
            {
                var value = Success(this.Tokens[back..i], Context);

                i++;
                var after = After(ref this, Context);
                return (value, after);
            }
            else if (NotTokens.Contains(cur.type))
                break;
        }
        i = back;

        return Fail(ref this, Context);
    }

    public Nothing StepIgnore() => i++.Return(Nothing.Value);
    public Nothing StepBack() => i--.Return(Nothing.Value);

    public readonly void Done(Action<Token> Fail)
    {
        if (!Overflow) Fail(Context);
    }

    public T Start<T>(
        Step<T> OnEmpty,
        DoFunction<T> Do)
    {
        if (Overflow) return OnEmpty(ref this, Context);

        var last = start;

        start = i;
        var res = Do(ref this);

        start = last;

        return res;
    }
}
