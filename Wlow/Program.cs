using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using LLVMSharp.Interop;
using Wlow.Node;
using Wlow.Types;

namespace Wlow;

public enum TokenType
{
    NaT,
    Bracket,
    BracketEnd,
    NotEquals,
    Equals,
    Set,
    Function,
    Comma,
    Cast,
    If,
    Else,
    Cascade,
    Flow,
    Number,
    Mul,
    Div,
    Add,
    Sub,
    Int8,
    Int16,
    Int32,
    Int64,
    Identifier,
    Newline,
    Ignore,
    Error,
}

public readonly partial record struct Token(Info info, TokenType type, string value = null, Token[] inner = null)
{
    public override string ToString() => $"({info}: {type} {value ?? inner.FmtString()})";


    [GeneratedRegex(@"(\()|(\))|(!=)|(==)|(=)|(')|(,)|(->)|(\?>)|(\:>)|(\|\|>)|(\|>)|-?(\d+)|(\*)|(\\)|(\+)|(\-)|(\bi8\b)|(\bi16\b)|(\bi32\b)|(\bi64\b)|(\b[a-zA-Z_]\w*\b)|(\r?\n)|([\t ]+)|(.)", RegexOptions.Multiline)]
    private static partial Regex Regex();
    private readonly static Regex regex = Regex();

    public static Token[] Tokenize(string text)
    {
        Stack<(List<Token> body, Info info)> stack = [];
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
                case TokenType.Bracket:
                    stack.Push((tokens, info));
                    tokens = [];
                    continue;
                case TokenType.BracketEnd:
                    var inner = tokens;
                    if (!stack.TryPop(out (List<Token> body, Info info) back))
                    {
                        throw new CompileException(info, $"unexpected brace end ')'");
                    }
                    tokens = back.body;
                    tokens.Add(new(back.info, TokenType.Bracket, inner: [.. inner]));
                    continue;
                case TokenType.Newline:
                    linestart = start + m.Length;
                    line++;
                    continue;
            }
            tokens.Add(new(info, type, value: value));
        }
        if (stack.TryPop(out (List<Token> body, Info info) check))
        {
            throw new CompileException(check.info, $"brace is not closed '('");
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
        if (split_count == 0) return [next(context, tokens)];

        Token ctx = context;
        List<T> result = [];

        for (int i = 0; i < tokens.Length; i++)
        {
            var cur = tokens[i];
            if (types.Any(v => v == cur.type))
            {
                result.Add(next(ctx, tokens[..i]));
                cur = ctx;
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
    static (IMetaType type, int offset) ASTType(Token ctx, ReadOnlySpan<Token> inner, bool throw_errors=false)
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
        Dictionary<string, IMetaType> args = null;
        IValue body = null;

        Token.LeftSplit(ctx, inner,
            [TokenType.Flow],
            (ctx, inner) =>
            {
                if (args is null)
                    args = new(
                        Token.LeftSplit(ctx, inner, [TokenType.Comma],
                        (ctx, inner) =>
                        {
                            var (typ, name) = ASTArgument(ctx, inner);
                            return KeyValuePair.Create(name, typ);
                        })
                    );
                else body = ASTGenerate(ctx, inner);
                return 0;
            },
            split_count: 1);

        if (body == null)
            throw new CompileException(inner.IsEmpty ? ctx.info : inner[^1].info, $"function body is missed, ex.: 'arg1, arg2, ..., argn |> body");

        return new FunctionValue(ctx.info, args, body);
    }

    static IValue ASTValue(Token ctx, ReadOnlySpan<Token> inner)
    {
        if (inner.Length == 0)
            throw new CompileException(ctx.info, $"value is empty");
        else if (inner.Length == 1)
        {
            var t = inner[0];
            return t.type switch
            {
                TokenType.Number => new IntValue(t.info, BigInteger.Parse(t.value)),
                TokenType.Identifier => new IdentValue(t.info, t.value),
                TokenType.Bracket => ASTGenerate(t, t.inner),
                _ => throw new CompileException(t.info, $"unexpected token {t.type}")
            };
        }
        else
        {
            var t = inner[1];
            throw new CompileException(t.info, $"unexpected token {t.value}");
        }
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
    static IValue ASTExpr(Token ctx, ReadOnlySpan<Token> inner) =>
        Token.LeftParseExpression(
            ctx,
            tokens: inner,
            types: [TokenType.Equals, TokenType.NotEquals],
            next: ASTMath,
            executor: (op, a, b) => op.type switch
            {
                TokenType.Equals => new EqualsValue(op.info, a, b),
                TokenType.NotEquals => new NotEqualsValue(op.info, a, b)
            }
        );
    static IValue ASTGlobals(Token ctx, ReadOnlySpan<Token> inner)
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
                        return ASTExpr(op, inner);
                    if (inner.Length != 1)
                        throw new CompileException(op.info, $"set operation must contains only name at right, value must be at left");
                    var t = inner[0];
                    if (t.type != TokenType.Identifier)
                        throw new CompileException(op.info, $"set must contains name at right");
                    return new IdentValue(t.info, t.value);
                },
                same: ASTExpr,
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
                else args = inner.IsEmpty ? [] : Token.LeftSplit(ctx, inner, [TokenType.Comma], (ctx, inner) => ASTGenerate(ctx, inner));
                return 0;
            },
            split_count: 1
        );

        if (args is not null)
            return new CallValue(value.info, value, args);
        return value;
    }
    static IValue ASTGenerate(Token ctx, ReadOnlySpan<Token> inner, bool jumpable = true)
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
                next: ASTGlobals,
                same: (ctx, ts) =>
                    Token.IndentLeftParseExpression(
                        ctx,
                        tokens: ts,
                        dedent: [TokenType.Else],
                        indent: [TokenType.If],
                        next: (ctx, ts) => ASTGenerate(ctx, ts, false),
                        same: (ctx, ts) => ASTGenerate(ctx, ts),
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
                                throw new CompileException(op.info, $"?-> is not closed by :->");
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

    static void TEST(Action<Scope> func)
    {
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
        LLVMTypeRef main_type = LLVMTypeRef.CreateFunction(
            context.Int32Type,
            [context.Int32Type, context.Int32Type],
            IsVarArg: false
        );

        // Add the 'main' function to the module.
        LLVMValueRef main = mod.AddFunction("main", main_type);

        // Append a basic block to the function.
        LLVMBasicBlockRef entry = main.AppendBasicBlock("entry");

        // Create an instruction builder and position it at the end of the entry block.
        using (var builder = context.CreateBuilder())
        {
            builder.PositionAtEnd(entry);

            var scope = new Scope([], context, builder, mod, main);
            func(scope);
        }

        Console.WriteLine(mod);
    }

    static void RunProgram(string name, string code, bool log_tokens=false, bool log_ast=false, bool log_llvm=false)
    {
        var toks = Token.Tokenize(code);
        if (log_tokens) Console.WriteLine(toks.FmtString());
        var ast = ASTGenerate(new(Info.One, TokenType.Error), toks);
        if (log_ast) Console.WriteLine(ast);

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
        LLVMTypeRef main_type = LLVMTypeRef.CreateFunction(
            context.Int32Type,
            [context.Int32Type, context.Int32Type],
            IsVarArg: false
        );

        // Add the 'main' function to the module.
        LLVMValueRef main = mod.AddFunction("main", main_type);

        // Append a basic block to the function.
        LLVMBasicBlockRef entry = main.AppendBasicBlock("entry");

        // Create an instruction builder and position it at the end of the entry block.
        using (var builder = context.CreateBuilder())
        {
            builder.PositionAtEnd(entry);

            var scope = new Scope([], context, builder, mod, main);
            var a = ast.Compile(scope);
            try
            {
                builder.BuildRet(a.Get(scope));
            }
            catch
            {
                builder.BuildRetVoid();
            }
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
            var wlow = (delegate* unmanaged[Cdecl]<int>)LLVM.GetPointerToGlobal(engine, main);
            Console.WriteLine($"Result '{name}': {wlow()}");
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

        RunProgram(
            "Infinity",
            @"('a |> a' a)=b |> b' b",
            log_llvm: true
        );
        RunProgram(
            "Iterate",
            @"0=i |> i==10 ?> (i) :> (i+1) -> i64 -> i32=i |> i",
            log_llvm: true
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
            "Break?",
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
          |||> List' .i32=list
            |> List_add(&list, 34)
            |> List_add(&list, 4)
            |> List_add(&list, 54)
            |> (list ||> each ||> 'e |> log' e)
        */
    }
}
