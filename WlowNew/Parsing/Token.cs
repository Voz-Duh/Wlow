using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Wlow.Shared;

namespace Wlow.Parsing;

public readonly partial record struct Token(Info info, TokenType type, string value = null!, Token[] inner = null!)
{
    private Token WithType(TokenType type)
        => new(info, type, value, inner);

    public override string ToString() => $"{{({info.Line}:{info.Column}) : {type} = \"{value ?? string.Join(", ", inner)}}}\"";

    private readonly static (TokenType start, TokenType end, string name, string start_str, string end_str, bool add)[] GroupTokens = [
        (
            TokenType.Bracket,
            TokenType.BracketEnd,
            "bracket",
            "(", ")",
            add: true
        ),
        (
            TokenType.Figure,
            TokenType.FigureEnd,
            "figure",
            "{", "}",
            add: true
        ),
        (
            TokenType.Comment,
            TokenType.CommentEnd,
            "comment",
            "--:", ":--",
            add: false
        )
    ];

    public static ImmutableArray<Token> Tokenize(string text)
    {
        Stack<(TokenType start, TokenType end, string name, string start_str, string end_str, bool add, List<Token> body, Info info)> stack = [];
        List<Token> tokens = [];
        var line = 1;
        var linestart = 0;
        string lineText = "";
        int start = 0;
        var curadd = true;

        void NewLineText()
        {
            var end = text.IndexOf('\n', startIndex: start);
            lineText = text[linestart..(end == -1 ? ^0 : end)];
        }

        NewLineText();
        
        foreach (Match m in TokensRegex.Instance.Matches(text))
        {
            start = m.Index;
            var info = new Info(start - linestart + 1, line, lineText);
            var value = m.Value;
            TokenType type = TokenType.Error;
            for (int i = 1; i < m.Groups.Count; i++)
            {
                if (m.Groups[i].Success)
                {
                    type = (TokenType)i;
                    break;
                }
            }
            switch (type)
            {
                case TokenType.Ignore:
                    continue;
                case TokenType.Newline:
                    linestart = start += m.Length;
                    line++;
                    NewLineText();
                    continue;
            }
            var grouped = false;
            
            var (curstart, curend) =
                stack.TryPeek(out var cur)
                ? (cur.start, cur.end)
                : (TokenType.NaT, TokenType.NaT);

            for (int i = 0; i < GroupTokens.Length; i++)
            {
                var (tstart, end, name, start_str, end_str, add) = GroupTokens[i];
                if (tstart == type)
                {
                    if (!curadd && type != curstart)
                        break;

                    stack.Push((tstart, end, name, start_str, end_str, curadd, tokens, info));
                    tokens = [];
                    grouped = true;
                    curadd = add;
                    break;
                }
                if (end == type)
                {
                    if (!curadd && type != curend)
                        break;

                    var inner = tokens;
                    if (!stack.TryPop(out var back)
                        || back.end != end)
                    {
                        throw CompilationException.Create(info, $"unexpected '{end_str}' to end {name} without '{start_str}' for openning");
                    }
                    tokens = back.body;
                    if (add)
                        tokens.Add(new(back.info, tstart, inner: [.. inner]));
                    grouped = true;
                    curadd = back.add;
                    break;
                }
            }
            if (!grouped && curadd)
            {
                if (type == TokenType.Error)
                    throw CompilationException.Create(info, $"invalid entry");
                tokens.Add(new(info, type, value: value));
            }
        }
        if (stack.TryPop(out var check))
        {
            throw CompilationException.Create(check.info, $"{check.name} is not closed by '{check.end_str}'");
        }
        return [.. tokens];
    }

    public static ImmutableArray<T> LeftSplit<T>(
        Token context,
        ReadOnlySpan<Token> tokens,
        TokenType[] types,
        Func<Token, ReadOnlySpan<Token>, T> next,
        int split_count = -1)
    {
        context = context.WithType(TokenType.NaT);
        if (split_count == 0) return [next(context, tokens)];

        Token ctx = context;
        List<T> result = [];

        for (int i = 0; i < tokens.Length; i++)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                result.Add(next(ctx, tokens[..i]));
                ctx = cur;
                tokens = tokens[(i + 1)..];
                split_count--;
                if (split_count == 0) break;
                i = 0;
            }
        }
        result.Add(next(ctx, tokens));
        return [.. result];
    }

    public static T LeftParseExpression<T>(
        Token context,
        ReadOnlySpan<Token> tokens,
        TokenType[] types,
        Func<Token, ReadOnlySpan<Token>, T> next,
        Func<Token, T, T, T> executor,
        Func<Token, ReadOnlySpan<Token>, T>? same = null,
        int count = -1,
        bool ignore_if_side_empty = true)
    {
        context = context.WithType(TokenType.NaT);
        if (count == 0) return next(context, tokens);
        for (int i = 0; i < tokens.Length; i++)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                var next_toks = tokens[..i];
                var same_toks = tokens[(i + 1)..];
                if ((same_toks.IsEmpty || next_toks.IsEmpty) && ignore_if_side_empty)
                    continue;

                return executor(
                    cur,
                    next(cur, next_toks),
                    same is not null
                        ? same.Invoke(
                            cur,
                            same_toks)
                        : LeftParseExpression(
                            cur,
                            same_toks,
                            types,
                            next,
                            executor,
                            same,
                            count - 1,
                            ignore_if_side_empty)
                );
            }
        }
        return next(context, tokens);
    }

    public static T RightParseExpression<T>(
        Token context,
        ReadOnlySpan<Token> tokens,
        TokenType[] types,
        Func<Token, ReadOnlySpan<Token>, T> next,
        Func<Token, T, T, T> executor,
        Func<Token, ReadOnlySpan<Token>, T>? same = null,
        int count = -1,
        bool ignore_if_side_empty = true)
    {
        context = context.WithType(TokenType.NaT);
        if (count == 0) return next(context, tokens);
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                var next_toks = tokens[(i + 1)..];
                var same_toks = tokens[..i];
                if ((same_toks.IsEmpty || next_toks.IsEmpty) && ignore_if_side_empty)
                    continue;

                return executor(
                    cur,
                    same is not null
                        ? same.Invoke(
                            cur,
                            same_toks)
                        : RightParseExpression(
                            cur,
                            same_toks,
                            types,
                            next,
                            executor,
                            same,
                            count - 1,
                            ignore_if_side_empty),
                    next(cur, next_toks)
                );
            }
        }
        return next(context, tokens);
    }

    public static T IndentLeftParseExpression<T>(
        Token context,
        ReadOnlySpan<Token> tokens,
        TokenType[] dedent,
        TokenType[] indent,
        Func<Token, ReadOnlySpan<Token>, T> next,
        Func<Token, T, T, T> executor,
        Func<Token, ReadOnlySpan<Token>, T>? same = null)
    {
        context = context.WithType(TokenType.NaT);
        int group = 1;
        for (int i = 0; i < tokens.Length; i++)
        {
            var cur = tokens[i];
            if (dedent.Any(v => v == cur.type))
            {
                if (--group == 0)
                    return executor(
                        cur,
                        next(cur, tokens[..i]),
                        same is not null
                            ? same.Invoke(
                                cur,
                                tokens[(i + 1)..])
                            : IndentLeftParseExpression(
                                cur,
                                tokens[(i + 1)..],
                                dedent,
                                indent,
                                next,
                                executor,
                                same)
                    );
            }
            else if (indent.Any(v => v == cur.type))
            {
                ++group;
            }
        }
        return next(context, tokens);
    }

    public static T IndentRightParseExpression<T>(
        Token context,
        ReadOnlySpan<Token> tokens,
        TokenType[] dedent,
        TokenType[] indent,
        Func<Token, ReadOnlySpan<Token>, T> next,
        Func<Token, T, T, T> executor,
        Func<Token, ReadOnlySpan<Token>, T>? same = null)
    {
        context = context.WithType(TokenType.NaT);
        int group = 1;
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            var cur = tokens[i];
            if (dedent.Any(v => v == cur.type))
            {
                if (--group == 0)
                    return executor(
                        cur,
                        same is not null
                            ? same.Invoke(
                                cur,
                                tokens[..i])
                            : IndentRightParseExpression(
                                cur,
                                tokens[..i],
                                dedent,
                                indent,
                                next,
                                executor,
                                same),
                        next(cur, tokens[(i + 1)..])
                    );
            }
            else if (indent.Any(v => v == cur.type))
            {
                ++group;
            }
        }
        return next(context, tokens);
    }
}
