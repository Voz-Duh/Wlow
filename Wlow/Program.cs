using System.Data;
using System.Numerics;
using System.Text.RegularExpressions;
using LLVMSharp.Interop;
using Wlow.Node;
using Wlow.Types;

namespace Wlow;

public enum TokenType
{
    NaT,
    Ignore,
    Bracket,
    BracketEnd,
    Figure,
    FigureEnd,
    NotEquals,
    Equals,
    Function,
    Comma,
    Definition,
    Cast,
    If,
    Else,
    Cascade,
    Flow,
    LowerEquals,
    GreaterEquals,
    Lower,
    Greater,
    Set,
    NumberLeftDotted,
    Number,
    Dot,
    Mul,
    Div,
    Add,
    Sub,
    Int8,
    Int16,
    Int32,
    Int64,
    Packed,
    Identifier,
    Newline,
    Error,
}

public readonly partial record struct Token(Info info, TokenType type, string value = null, Token[] inner = null)
{

    private Token WithType(TokenType type)
        => new(info, type, value, inner);

    public override string ToString() => $"({info}: {type} {value ?? inner.FmtString()})";


    [GeneratedRegex(@"([\t ]+|--[^\n]*)|(\()|(\))|(\{)|(\})|(!=)|(==)|(')|(,)|(\:\:)|(->)|(\?>)|(\:>)|(\|\|>)|(\|>)|(<=)|(>=)|(<)|(>)|(=)|(\.\d+)|-?(\d+\.?\d*)|(\.)|(\*)|(\\)|(\+)|(\-)|(\bi8\b)|(\bi16\b)|(\bi32\b)|(\bi64\b)|(\bpacked\b)|(\b[a-zA-Z_]\w*\b)|(\r?\n)|(.)", RegexOptions.Multiline)]
    private static partial Regex Regex();
    private readonly static Regex regex = Regex();
    private readonly static (TokenType start, TokenType end, string name, string start_str, string end_str)[] GroupTokens = [
        (
            TokenType.Bracket,
            TokenType.BracketEnd,
            "bracket",
            "(", ")"
        ),
        (
            TokenType.Figure,
            TokenType.FigureEnd,
            "figure",
            "{", "}"
        )
    ];

    public static Token[] Tokenize(string text)
    {
        Stack<(TokenType end, string name, string start_str, string end_str, List<Token> body, Info info)> stack = [];
        List<Token> tokens = [];
        var line = 1;
        var linestart = 0;
        foreach (Match m in regex.Matches(text))
        {
            var start = m.Index;
            var info = new Info(start - linestart + 1, line);
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
                    linestart = start + m.Length;
                    line++;
                    continue;
            }
            var grouped = false;
            for (int i = 0; i < GroupTokens.Length; i++)
            {
                var (tstart, end, name, start_str, end_str) = GroupTokens[i];
                if (tstart == type)
                {
                    stack.Push((end, name, start_str, end_str, tokens, info));
                    tokens = [];
                    grouped = true;
                    break;
                }
                if (end == type)
                {
                    var inner = tokens;
                    if (!stack.TryPop(out var back) || back.end != end)
                    {
                        throw new CompileException(info, $"unexpected '{end_str}' to end {name} without '{start_str}' for openning");
                    }
                    tokens = back.body;
                    tokens.Add(new(back.info, tstart, inner: [.. inner]));
                    grouped = true;
                    break;
                }
            }
            if (!grouped)
            {
                if (type == TokenType.Error)
                {
                    throw new CompileException(info, $"invalid entry");
                }
                tokens.Add(new(info, type, value: value));
            }
        }
        if (stack.TryPop(out var check))
        {
            throw new CompileException(check.info, $"{check.name} is not closed by '{check.end_str}'");
        }
        return [.. tokens];
    }

    public static T[] LeftSplit<T>(
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
        Func<Token, ReadOnlySpan<Token>, T> same = null,
        int count = -1)
    {
        context = context.WithType(TokenType.NaT);
        if (count == 0) return next(context, tokens);
        for (int i = 0; i < tokens.Length; i++)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                return executor(
                    cur,
                    next(cur, tokens[..i]),
                    same is not null
                        ? same.Invoke(
                            cur,
                            tokens[(i + 1)..])
                        : LeftParseExpression(
                            cur,
                            tokens[(i + 1)..],
                            types,
                            next,
                            executor,
                            same,
                            count - 1)
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
        Func<Token, ReadOnlySpan<Token>, T> same = null,
        int count = -1)
    {
        context = context.WithType(TokenType.NaT);
        if (count == 0) return next(context, tokens);
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                return executor(
                    cur,
                    same is not null
                        ? same.Invoke(
                            cur,
                            tokens[..i])
                        : RightParseExpression(
                            cur,
                            tokens[..i],
                            types,
                            next,
                            executor,
                            same,
                            count - 1),
                    next(cur, tokens[(i + 1)..])
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
        Func<Token, ReadOnlySpan<Token>, T> same = null)
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
        Func<Token, ReadOnlySpan<Token>, T> same = null)
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

class Program
{
    static (IMetaType type, int offset) ASTType(Token ctx, ReadOnlySpan<Token> inner, bool throw_errors = false)
    {
        if (throw_errors && inner.Length == 0)
            throw new CompileException(ctx.info, "type is cannot be empty");

        var first = inner[0];

        return first.type switch
        {
            TokenType.Int8 => (new IntMeta(8, BinaryType.Int8), 1),
            TokenType.Int16 => (new IntMeta(16, BinaryType.Int16), 1),
            TokenType.Int32 => (new IntMeta(32, BinaryType.Int32), 1),
            TokenType.Int64 => (new IntMeta(64, BinaryType.Int64), 1),
            TokenType.Function =>
                inner.Length < 2
                    ? throw new CompileException(first.info, "function type must have arguments, ex.: '(args) optional return")
                : inner[1].type != TokenType.Bracket
                    ? throw new CompileException(first.info, "function type must have arguments, ex.: '(args) optional return")
                : ((Func<ReadOnlySpan<Token>, (IMetaType, int)>)((inner) =>
                    {
                        var args =
                            Token.LeftSplit(inner[1], inner[1].inner, [TokenType.Comma],
                                (ctx, inner) =>
                                {
                                    if (inner.Length == 0)
                                        throw new CompileException(ctx.info, "function type cannot have empty arguments");

                                    var (typ, j) = ASTType(ctx, inner);
                                    if (j != inner.Length)
                                        throw new CompileException(inner[0].info, "function type cannot have named arguments");

                                    return typ;
                                });

                        var ret = inner[2..];
                        var (type, i) = ASTType(ctx, ret);

                        return (new FunctionMeta(args, type), 2 + i);
                    }))(inner),
            _ =>
                throw_errors
                ? throw new CompileException(first.info, "invalid type identifier")
                : (VoidMeta.Get, 0)
        };
    }

    static (IMetaType typ, string name) ASTArgument(Token ctx, ReadOnlySpan<Token> inner)
    {
        if (inner.Length == 0)
            throw new CompileException(ctx.info, $"argument is cannot be empty");

        if (inner.Length == 1 && inner[0].type == TokenType.Bracket)
            // TODO tuple-destructed arguments
            throw new CompileException(ctx.info, $"tuple-destructed argument is not supported yet");

        if (inner.Length == 1 && inner[0].type == TokenType.Identifier)
            return (GenericMeta.Get, inner[0].value);

        var (type, i) = ASTType(ctx, inner);

        if (i >= inner.Length)
            throw new CompileException(inner[^1].info, $"argument name expected after type");
        if (i + 1 < inner.Length)
            throw new CompileException(inner[i + 1].info, $"argument name must be last, did you miss comma?");

        var name = inner[i];
        if (name.type != TokenType.Identifier)
            throw new CompileException(inner[i].info, $"argument name must be an identifier");

        return (type, name.value);
    }
    static FunctionValue ASTFunction(Token ctx, ReadOnlySpan<Token> inner)
    {
        Pair<string, IMetaType>[] args = null;
        IValue body = null;

        Token.LeftSplit(ctx, inner,
            [TokenType.Flow],
            next: (ctx, inner) =>
            {
                if (args is null)
                    args =
                        Token.LeftSplit(ctx, inner, [TokenType.Comma],
                        (ctx, inner) =>
                        {
                            var (typ, name) = ASTArgument(ctx, inner);
                            return Pair.From(name, typ);
                        });
                else body = ASTExpression(ctx, inner);
                return 0;
            },
            split_count: 1);

        if (body == null)
            throw new CompileException(inner.IsEmpty ? ctx.info : inner[^1].info, $"function body is missed, ex.: 'arg1, arg2, ..., argn |> body");

        return new FunctionValue(ctx.info, args, body);
    }

    static IValue ASTAccessors(IValue at, Token token)
        => token.type switch
        {
            TokenType.NumberLeftDotted
            => int.TryParse(token.value, out var val)
                ? new IndexedFieldAccessor(token.info, at, val)
                : throw new CompileException(token.info, $"index {token.value} is too big to be processed"),
            TokenType.Identifier
            => new FieldAccessor(token.info, at, token.value),
            _ => throw new CompileException(token.info, $"invalid token used to access value")
        };

    static IValue ASTValue(Token ctx, ReadOnlySpan<Token> inner)
    {
        if (inner.Length == 0)
            throw new CompileException(ctx.info, $"value is empty");

        static IValue FigureParse(Token t, bool packed = false)
        {
            var args = Token.LeftSplit(
                t,
                t.inner,
                [TokenType.Comma],
                next: (ctx, inner)
                => Token.RightParseExpression(
                    ctx,
                    tokens: inner,
                    count: 1,
                    types: [TokenType.Set],
                    next: (op, inner) =>
                    {
                        if (op.type != TokenType.Set)
                            throw new CompileException(op.info, "structure field cannot be unnamed");

                        if (inner.Length != 1)
                            throw new CompileException(op.info, "structure field must contains only name at right, value must be at left");

                        var t = inner[0];
                        if (t.type != TokenType.Identifier)
                            throw new CompileException(op.info, "structure field must contains name at right");
                        return (value: (IValue)null, name: t.value);
                    },
                    same: (ctx, inner) => (value: ASTExpression(ctx, inner), name: null),
                    executor: (op, a, b) => (a.value, b.name)
                )
            );

            return new StructValue(
                t.info,
                [.. args.Select(v => Pair.From(v.name, v.value))],
                packed
            );
        }

        static (IValue value, bool tuple) BracketParse(Token t, bool packed = false)
        {
            var tupleable = !t.inner.Any(v => v.type == TokenType.Function);
            if (tupleable)
            {
                var args = Token.LeftSplit(
                    t,
                    t.inner,
                    [TokenType.Comma],
                    next: (ctx, inner) => (ctx, inner: (Token[])[.. inner])
                );

                if (args.Length != 1)
                {
                    if (args.Last().inner.Length == 0)
                        args = args[..^1];

                    return (new TupleValue(
                        t.info,
                        [.. args.Select(v => ASTExpression(v.ctx, v.inner, jumpable: false))],
                        packed
                    ), true);
                }
            }
            return (ASTExpression(t, t.inner), false);
        }

        IValue value = null;
        int i = 0;

        if (inner.Length >= (i = 1))
        {
            var t = inner[0];
            switch (t.type)
            {
                case TokenType.Number:
                    value = new IntValue(t.info, BigInteger.Parse(t.value));
                    break;
                case TokenType.Identifier:
                    value = new IdentValue(t.info, t.value);
                    break;
                case TokenType.Bracket:
                    value = BracketParse(t).value;
                    break;
                case TokenType.Figure:
                    value = FigureParse(t);
                    break;
            }
        }
        if (value is null && inner.Length >= (i = 2))
        {
            var t0 = inner[0];
            var t1 = inner[1];
            switch (t0.type)
            {
                case TokenType.Packed:
                    var valid = false;
                    if (t1.type == TokenType.Bracket)
                        (value, valid) = BracketParse(t1, packed: true);
                    else if (t1.type == TokenType.Figure)
                        (value, valid) = (FigureParse(t1, packed: true), true);

                    if (!valid)
                        throw new CompileException(t0.info, "packed is must be used before of tuple or structure literal");
                    break;
            }
        }

        while (i < inner.Length)
        {
            var t = inner[i];
            var valid = false;
            if (t.type == TokenType.NumberLeftDotted)
            {
                value = ASTAccessors(value, new(new(t.info.column + 1, t.info.line), t.type, t.value[1..]));
                i++;
                valid = true;
            }
            else if (t.type == TokenType.Dot)
            {
                if (i + 1 >= inner.Length)
                {
                    throw new CompileException(t.info, $"dot access is ended with nothing");
                }
                i++;

                var t1 = inner[i];

                value = ASTAccessors(value, t1);
                i++;
                valid = true;
            }
            if (!valid)
            {
                throw new CompileException(t.info, $"unexpected token {t.value}");
            }
        }
        return value;
    }
    static IValue ASTCast(Token ctx, ReadOnlySpan<Token> inner)
    {
        var slices = Token.LeftSplit(
            ctx,
            tokens: inner,
            types: [TokenType.Cast],
            next: (ctx, toks) => (ctx, (Token[])[.. toks])
        );

        var (context, tokens) = slices[0];
        IValue value = ASTValue(context, tokens);

        for (int i = 1; i < slices.Length; i++)
        {
            (context, tokens) = slices[i];
            var (type, off) = ASTType(context, tokens, throw_errors: true);
            if (off < tokens.Length)
                throw new CompileException(tokens[off].info, "unexpected tokens after valid type identifier");
            value = new Cast(context.info, value, type);
        }

        return value;
    }
    static IValue ASTMath1(Token ctx, ReadOnlySpan<Token> inner) =>
            Token.LeftParseExpression(
                ctx,
                tokens: inner,
                types: [TokenType.Div, TokenType.Mul],
                next: ASTCast,
                executor: (op, a, b) => op.type switch
                {
                    TokenType.Div => new DivValue(op.info, a, b),
                    TokenType.Mul => new MulValue(op.info, a, b)
                }
            );
    static IValue ASTMath(Token ctx, ReadOnlySpan<Token> inner) =>
        Token.LeftParseExpression(
            ctx,
            tokens: inner,
            types: [TokenType.Add, TokenType.Sub],
            next: ASTMath1,
            executor: (op, a, b) => op.type switch
            {
                TokenType.Add => new AddValue(op.info, a, b),
                TokenType.Sub => new SubValue(op.info, a, b)
            }
        );
    static IValue ASTOperations(Token ctx, ReadOnlySpan<Token> inner) =>
        Token.LeftParseExpression(
            ctx,
            tokens: inner,
            types: [
                TokenType.Equals,
                TokenType.NotEquals,
                TokenType.Lower,
                TokenType.Greater,
                TokenType.LowerEquals,
                TokenType.GreaterEquals,
            ],
            next: ASTMath,
            executor: (op, a, b) => op.type switch
            {
                TokenType.Equals => new EqualsValue(op.info, a, b),
                TokenType.NotEquals => new NotEqualsValue(op.info, a, b),
                TokenType.Lower => new LowerValue(op.info, a, b),
                TokenType.Greater => new GreaterValue(op.info, a, b),
                TokenType.LowerEquals => new LowerEqualsValue(op.info, a, b),
                TokenType.GreaterEquals => new GreaterEqualsValue(op.info, a, b)
            }
        );
    static IValue ASTExpressionGlobals(Token ctx, ReadOnlySpan<Token> inner)
    {
        (Token, Token[])[] cascade_slices = Token.LeftSplit(ctx, inner, [TokenType.Cascade], (ctx, inner) => (ctx, inner.ToArray()));

        if (cascade_slices.Length > 1)
        {
            // TODO cascade operator
            throw new CompileException(ctx.info, $"cascade operator is not supported yet");
            if (cascade_slices.Length == 2)
            {
                throw new($"");
            }
        }

        if (inner.Length == 1 && inner[0].type == TokenType.Identifier)
            return new Jump(inner[0].info, inner[0].value);

        static IValue ValueParse(Token ctx, ReadOnlySpan<Token> inner)
            => Token.RightParseExpression(
                ctx,
                tokens: inner,
                count: 1,
                types: [TokenType.Set],
                next: (op, inner) =>
                {
                    if (op.type != TokenType.Set)
                        return ASTOperations(op, inner);
                    if (inner.Length != 1)
                        throw new CompileException(op.info, $"set operation must contains only name at right, value must be at left");
                    var t = inner[0];
                    if (t.type != TokenType.Identifier)
                        throw new CompileException(op.info, $"set must contains name at right");
                    return new IdentValue(t.info, t.value);
                },
                same: ASTOperations,
                executor: (op, a, b) => new Set(op.info, a, ((IdentValue)b).name)
            );

        IValue value = null;
        IValue[] args = null;
        Token.LeftSplit(
            ctx,
            inner,
            [TokenType.Function],
            (ctx, inner) =>
            {
                if (value is null)
                    value = ValueParse(ctx, inner);
                else args = inner.IsEmpty ? [] : Token.LeftSplit(ctx, inner, [TokenType.Comma], (ctx, inner) => ASTExpression(ctx, inner));
                return 0;
            },
            split_count: 1
        );

        if (args is not null)
            return new CallValue(value.info, value, args);
        return value;
    }
    static IValue ASTExpression(Token ctx, ReadOnlySpan<Token> inner, bool jumpable = true)
    {
        if (!inner.IsEmpty)
        {
            if (inner.Length == 1)
            {
                if (jumpable && inner[0].type == TokenType.Identifier)
                    return new IdentValue(inner[0].info, inner[0].value);
            }

            if (inner[0].type == TokenType.Function)
                return ASTFunction(inner[0], inner[1..]);
        }

        return Token.LeftParseExpression(
                ctx,
                tokens: inner,
                types: [
                    TokenType.Flow,
                TokenType.If
                ],
                next: ASTExpressionGlobals,
                same: (ctx, ts) =>
                    Token.IndentLeftParseExpression(
                        ctx,
                        tokens: ts,
                        dedent: [TokenType.Else],
                        indent: [TokenType.If],
                        next: (ctx, ts) => ASTExpression(ctx, ts, false),
                        same: (ctx, ts) => ASTExpression(ctx, ts),
                        executor: (op, a, b) => new Else(op.info, a, b)
                    ),
                executor: (op, a, b) =>
                {
                    IValue result = null;
                    switch (op.type)
                    {
                        case TokenType.Flow:
                            result = new Flow(op.info, a, b);
                            break;
                        case TokenType.If:
                            if (b is not Else block)
                            {
                                throw new CompileException(op.info, $"?> is not closed by :>");
                            }
                            result = new Condition(op.info, a, block.then, block.other);
                            break;
                        case TokenType.Function:
                            result = b;
                            break;
                    }
                    return result;
                }
            );
    }

    static Definition[] ASTGenerate(Token ctx, ReadOnlySpan<Token> inner)
    {
        var definitions = Token.LeftSplit(ctx, inner, [TokenType.Definition], next: (ctx, inner) => (ctx, value: inner.ToArray()));
        if (definitions.Length == 1)
            throw new CompileException(definitions[0].ctx.info, "Wlow program must have at least main definition, ex.: main :: 0");

        foreach (var (tok, value) in definitions)
            if (value.Length == 0)
                throw new CompileException(tok.info, "definition is cannot be empty");

        return [
            ..
            definitions[..^1].Select((v, i) =>
            {
                if (i == 0)
                {
                    if (v.value.Length != 1)
                        throw new CompileException(v.ctx.info, "definition must contains only name at left");

                    var t = v.value[0];
                    if (t.type != TokenType.Identifier)
                        throw new CompileException(v.ctx.info, $"definition must contains name at left, not token {t.value}");

                    var (ctx, value) = definitions[i + 1];
                    var toks =
                        (i + 1 == definitions.Length - 1)
                        ? value
                        : value[0..^1];
                    return new Definition(t.info, t.value, ASTExpression(ctx, toks));
                }
                else
                {
                    if (v.value.Length < 2)
                        throw new CompileException(v.ctx.info, "definition must contains at least name at left");

                    var t = v.value[^1];
                    if (t.type != TokenType.Identifier)
                        throw new CompileException(v.ctx.info, $"definition must contains name at left, not token {t.value}");

                    var (ctx, value) = definitions[i + 1];
                    var toks =
                        i + 1 == definitions.Length - 1
                        ? value
                        : value[0..^1];
                    return new Definition(t.info, t.value, ASTExpression(ctx, toks));
                }
            })
        ];
    }

    static void RunProgram(string name, string code, bool log_tokens = false, bool log_ast = false, bool log_llvm = false)
    {
        var toks = Token.Tokenize(code);
        if (log_tokens) Console.WriteLine(toks.FmtString());
        var ast = ASTGenerate(new(Info.One, TokenType.Error), toks);
        if (log_ast) foreach (var node in ast) Console.WriteLine(node);

        // Initialize LLVM components.
        LLVM.LinkInMCJIT();
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();
        LLVM.InitializeNativeDisassembler();

        using var context = LLVMContextRef.Create();

        // Create a new LLVM module.
        using var mod = context.CreateModuleWithName("LLVMSharpIntro");

        // Define the function's return type and parameter types.
        var main_type = LLVMTypeRef.CreateFunction(
            context.Int32Type,
            // int argc, char *argv[]
            [context.Int32Type, context.Int8Type.Ptr(0).Ptr(0)],
            IsVarArg: false
        );

        // Add the 'main' function to the module.
        var main = mod.AddFunction("main", main_type);

        // Append a basic block to the function.
        var entry = main.AppendBasicBlock("entry");

        // Create an instruction builder and position it at the end of the entry block.
        using (var builder = context.CreateBuilder())
        {
            builder.PositionAtEnd(entry);

            var scope = new Scope([], [], context, builder, mod, main);
            foreach (var node in ast) node.Compile(scope);

            if (!scope.global_variables.TryGetValue("main", out var result))
                throw new CompileException(Info.One, "entry point is not founded, define main");

            if (result.function != null)
            {
                switch (result.function.arguments.Length)
                {
                    case 0:
                        result = result.function.Call(scope.CloneNoVariable(), result.function.info, []);
                        break;
                    case 1:
                        var i8 = new IntMeta(8, BinaryType.Int8);
                        var i32 = new IntMeta(32, BinaryType.Int32);

                        var cstr = PointerMeta.To(i8, mut: false);
                        var arr_cstr = PointerMeta.To(cstr, mut: false);

                        var str =
                            new StructMeta([
                                new("len", i32),
                                new("data", PointerMeta.To(i8, mut: false))
                            ]);
                        var array =
                            new StructMeta([
                                new("len", i32),
                                new("data", PointerMeta.To(str, mut: false))
                            ]);

                        // get llvm types
                        var llvm_i8_ty = i8.Type(scope);
                        var llvm_i32_ty = i32.Type(scope);
                        var llvm_cstr_ty = cstr.Type(scope);
                        var llvm_arr_cstr_ty = arr_cstr.Type(scope);
                        var llvm_str_ty = str.Type(scope);
                        var llvm_array_ty = array.Type(scope);

                        /*                        
                            i = alloca i32
                            *i = 0
                            goto cond

                            cond:
                            i_ld = load i32 i
                            if While(i_ld)
                            then goto end
                            else goto start

                            start:
                            Do(i_ld)
                            *i = i_ld + 1
                            goto cond

                            end:
                        */
                        LLVMValueRef Loop(
                            Func<LLVMValueRef, LLVMValueRef> While,
                            Action<LLVMValueRef> Do,
                            bool ReturnI = false)
                        {
                            // define blocks
                            var bb_cond = scope.Block();
                            var bb_start = scope.Block();
                            var bb_end = scope.Block();

                            /*
                                i = alloca i32
                                *i = 0
                                goto cond
                            */
                            var llvm_i = scope.bi.BuildAlloca(llvm_i32_ty);
                            scope.bi.BuildStore(LLVMValueRef.CreateConstInt(llvm_i32_ty, 0), llvm_i);
                            scope.bi.BuildBr(bb_cond);

                            /*
                                cond:
                                i_ld = load i32 i
                                if While(i_ld)
                                then goto end
                                else goto start
                            */
                            scope.bi.PositionAtEnd(bb_cond);
                            var llvm_i_ld = scope.bi.BuildLoad2(llvm_i32_ty, llvm_i);
                            scope.bi.BuildCondBr(
                                If: While(llvm_i_ld),
                                Then: bb_end,
                                Else: bb_start
                            );

                            /*
                                start:
                                Do(i_ld)
                                *i = i_ld + 1
                                goto cond
                            */
                            scope.bi.PositionAtEnd(bb_start);
                            Do(llvm_i_ld);
                            scope.bi.BuildStore(
                                Val: scope.bi.BuildAdd(llvm_i_ld, LLVMValueRef.CreateConstInt(llvm_i32_ty, 1)),
                                Ptr: llvm_i
                            );
                            scope.bi.BuildBr(bb_cond);

                            /*
                                end:
                            */
                            scope.bi.PositionAtEnd(bb_end);
                            return ReturnI ? scope.bi.BuildLoad2(llvm_i32_ty, llvm_i) : default;
                        }
                        /*
                            len = args[0]
                            ptr = args[1]
                            strs = alloc[len] str
                        */
                        var llvm_len = main.GetParam(0);
                        var llvm_ptr = main.GetParam(1);
                        var llvm_strs = scope.bi.BuildArrayAlloca(llvm_str_ty, llvm_len);

                        Loop(
                            While: i => scope.bi.BuildICmp(LLVMIntPredicate.LLVMIntEQ, i, llvm_len),
                            Do: i =>
                            {
                                var str = scope.bi.BuildGEP2(llvm_str_ty, llvm_strs, [i]);
                                var cstr = scope.bi.BuildGEP2(llvm_i8_ty, llvm_ptr, [i]);
                                var str_len = Loop(
                                    ReturnI: true,
                                    While: i =>
                                        scope.bi.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                                            scope.bi.BuildLoad2(llvm_i8_ty, scope.bi.BuildGEP2(llvm_i8_ty, cstr, [i])),
                                            LLVMValueRef.CreateConstInt(llvm_i8_ty, 0)
                                        ),
                                    Do: i => { }
                                );

                                scope.bi.BuildStore(
                                    str_len,
                                    scope.bi.BuildStructGEP2(llvm_str_ty, str, 0)
                                );
                                scope.bi.BuildStore(
                                    cstr,
                                    scope.bi.BuildStructGEP2(llvm_str_ty, str, 1)
                                );
                            }
                        );

                        var llvm_res = scope.bi.BuildAlloca(llvm_array_ty);
                        scope.bi.BuildStore(
                            llvm_len,
                            scope.bi.BuildStructGEP2(llvm_array_ty, llvm_res, 0)
                        );
                        scope.bi.BuildStore(
                            llvm_strs,
                            scope.bi.BuildStructGEP2(llvm_array_ty, llvm_res, 1)
                        );

                        result = result.function.Call(scope.CloneNoVariable(), result.function.info, [
                            (
                                result.function.info,
                                new LLVMValue(
                                    array,
                                    val: scope.bi.BuildLoad2(llvm_array_ty, llvm_res)
                                )
                            )
                        ]);
                        break;
                    default:
                        throw new CompileException(result.function.info, "main can have only 2 signatures '() optional i32 or '({i32 len, *{i32 len, *i8 data}}) optional i32");
                }
            }

            var fallback = true;
            if (result.type.Is<IntMeta>())
            {
                try
                {
                    builder.BuildRet(result.Get(new(), scope));
                    fallback = false;
                }
                catch { }
            }
            if (fallback) builder.BuildRet(LLVMValueRef.CreateConstInt(context.Int32Type, 0));
        }

        if (log_llvm) Console.WriteLine($"Module:\n{mod}");
        if (!mod.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out var message))
        {
            Console.WriteLine($"Error: {message}");
            return;
        }

        // Create an execution engine.
        var options = LLVMMCJITCompilerOptions.Create();
        if (!options.TryForModule(mod, out var engine, out message))
        {
            Console.WriteLine($"Error creating JIT: {message}");
            return;
        }

        unsafe
        {
            var wlow = (delegate* unmanaged[Cdecl]<int, byte*, int>)LLVM.GetPointerToGlobal(engine, main);
            Console.WriteLine($"Result '{name}': {wlow(0, null)}");
        }
    }

    static void Main()
    {
        // TODO nothing/nil/void as value to make functions without specific return type 
        /* TODO
            FunctionDecl - add ghost arguments (aka. ghost variables)
            Feature pseudo example:
            |> 0=a
            |> (closure&call |> 2=a) -- ex. name - closure
            |> (a)
            here a and current scope UniqueNumber must be registered into ghosts of closure
                unique Scope mode which will register ghosts as used, uses Type function
                ex.:
                put ghosts inside of temporary Scope
                call Type at AST node
                get all ghosts which is marked as "used"
                save as function ghosts
            when closure is called
                if outer function is closure's source
                    call closure and pass references to variables which is was registered in closure as ghosts  
                else
                    outer function must register closure's ghosts as invisible ghosts
                    it makes sure that outer-outer function will pass needed ghosts here
                    call closure and pass all ghosts registered in closure
        */
        /* TODO
            Anonimous tuple and structure types.
            Tuple:
                Must be available to use FieldIndexAccessor node which is a representation for the "tuple.0"/"tuple.1" construction
                FieldIndexAccessor will use overloading of "bool HasFieldIndex" and "LLVMValue FieldIndexGet(int i)"
                FieldIndexAccessor pseudo example:
                    if type.HasFieldIndex
                        res = type.FieldIndexGet(index)
                        if res.Has
                            return res.Value
                        else
                            comperr "index {index} is not valid in tuple {type.Name(sc)} with {type.elements.Length} count"    
                    else
                        comperr "index access as field is available only for tuple types, or types derived from tuple"

            Structures has the same arch but with string dictionary instead of array.
        */

        RunProgram(
            "Document try",
@"
main :: packed (2, 1)=b |> (1, 45)=a |> b.0 + b.1 + a.0 + a.1 + ((1, 2).0)
");
        RunProgram(
            "Document based fib",
@"
fib :: 'n |> n <= 1 ?> (n) :> (fib' n - 1) + (fib' n - 2)
main :: 'a
    -- a.len here is 0
    |> a.len=i
    -- you also can use a.data, but right now pointers is not fully supported
    -- and i mean that language only has pointers, nothing else
    |> (fib' 10) + i
");
        RunProgram(
            "Fib in main",
@"
main :: 'a
    |> ('fib, n |> n <= 1 ?> (n) :> (fib' fib, n - 1) + (fib' fib, n - 2))=fib
    -- a.len here is 0
    |> a.len=i
    -- you also can use a.data, but right now pointers is not fully supported
    -- and i mean that language only has pointers, nothing else
    |> (fib' fib, 10) + i
");
        /* outdated tests
        RunProgram(
            "Tuple",
            @"packed (2, 1)=b |> (1, 45)=a |> b.0 + b.1 + a.0 + a.1 + ((1, 2).0)"
        );
        RunProgram(
            "Structure",
            @"packed {2=x, 1=y}=b |> {1=a, 45=b}=a |> b.x + b.y + a.a + a.b + {1=c, 2=e}.e"
        );
        RunProgram(
            "Infinity",
            @"('a |> a' a)=b |> b' b",
            log_llvm: true
        );
        RunProgram(
            "Iterate",
            @"0=i |> i==10 ?> (i) :> (i+1) -> i64 -> i32=i |> i"
        );
        RunProgram(
            "Iterate through Addition Function",
            @"('a, b |> a + b)=add |> 0=x |> 10==x ?> (x) :> (add' x, 1)=x |> x"
        );
        RunProgram(
            "Iterate through Anonymous Addition Function passed to Another Function",
            @"('f, a, b |> f' a, b)=y |> 0=x |> 10==x ?> (x) :> (y' ('a, b |> a + b), x, 1) =x |> x"
        );
        RunProgram(
            "Iterate through Iterator Function",
            @"('to, f |> 0=i |> i==to ?> (i) :> f' i |> i+1=i |> i)=repeat |> repeat' 10, ('a |> (a))"
        );
        RunProgram(
            "Iterate through Recursion",
            @"('self, i, to |> i==to ?> (i) :> self' self, i+1, to)=repeat
                |> repeat' repeat, 0, 10
                |> repeat' repeat, 0, 10
                |> repeat' ('ignore, a, b |> a + b), 0, 10
                |> repeat' ('ignore, a, b |> a + b), 0, 10",
            //@"('self, i, to |> i==to ?> (i) :> self' self, i+1, to)=repeat |> repeat' repeat, 0, 10 |> repeat' repeat, 0, 10",
            log_llvm: true
        );
        RunProgram(
            "Will break many compilers, but not that",
            @"
('self, x |> 
    ('outer, y |> outer' outer, y) = inner
    |> inner' self, x
)=F

|> F' F, 0
|> 0
",
            log_llvm: true
        );
        */
        //RunProgram(@"('to, f |> 0=i |> i==to ?> (i) :> 'f i |> i+1=i |> i)=count |> 0=sum |> (10 ||> count ||> 'e |> sum+e=sum) |> sum");
        //RunProgram(@"0=i |> (closure a |> a=i)=cls |> cls' 5 |> (i)");
        //RunProgram(@"(object ||> each ||> 'e |> log' e)");
        //RunProgram(@"('self |> self' self)=fn |> fn' fn");

        // 0=sum |> ([1, 2, 3, 4, 5, 6, 7] ||> each ||> 'e |> sum+e=sum) |> (sum)
        /*
        __List_try_expand |||> 'list, count |> count+list:len>=list:cap ?> list:cap*2=list:cap |> (realoc' list:data, list:cap) -> ?list:data=list:data :> list:data

        List     |||> 'type        |> { 0=len, 0=cap, (malloc' sizeof type) -> mut *?type=data }
        List_add |||> 'list, value |> value=(__List_try_expand' list, 1)[list:len] |> list:len+1=list:len |> list:len-1

        each |||> 'array, f |> 0=i |> i<array.len ?> f' array.data[i] |> i+1=i |> i :> (i)

        example
          :: List' .i32=list
            |> List_add(&list, 34)
            |> List_add(&list, 4)
            |> List_add(&list, 54)
            |> (list ||> each ||> 'e |> log' e)
        */
    }
}
