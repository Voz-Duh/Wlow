
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static Or<(TSelf self, TScope scope), TNoscoped> Scoped<TSelf, TScope, TNoscoped>(
        ref ManualTokens toks,
        ManualTokens.SplitedStep<TSelf> Self,
        ManualTokens.Step<TScope> Scope,
        ManualTokens.Step<TNoscoped> NoScope)
    => toks.Until(
        [TokenType.Set],
        Self,
        Scope,
        NoScope
    );

    static INode Variable(
        ref ManualTokens toks,
        Func<Info, string, IMetaType, INode?, INode> Create,
        string RequiredName,
        string RequiredDone,
        string RequiredDefined,
        bool ValueOptional)
    {
        (Token, IMetaType?) self(ref ManualTokens toks, Token tok) =>
            toks.Start(
                OnEmpty: (ref _, tok) => throw CompilationException.Create(tok.info, RequiredName),
                Do: (ref toks) =>
                {
                    var name = toks.Get(TokenType.Ident,
                        Else: null,
                        Fail: (ref _, tok) => throw CompilationException.Create(tok.info, RequiredName),
                        Success: (ref _, tok) => tok
                    );

                    var type = Type(name, ref toks, optional: true);

                    toks.Done(Fail: (tok) => throw CompilationException.Create(tok.info, RequiredDone));

                    return (name, type);
                }
            );

        var (left, value) = Scoped<(Token, IMetaType?), INode?, ((Token, IMetaType?), INode?)>(
            ref toks,
            Self: (toks, tok) =>
            {
                var inner = ManualTokens.Create(tok, toks);
                return self(ref inner, tok);
            },
            Scope: (ref toks, tok) => Expression(ref toks),
            NoScope: (ref toks, tok) => ValueOptional ? (self(ref toks, tok), null) : throw CompilationException.Create(tok.info, RequiredDefined)
        ).Unwrap();

        var (name, type) = left;

        return Create(name.info, name.value, type, value);
    }

    static INode LetVariable(ref ManualTokens toks)
        => Variable(
            ref toks,
            Create: (info, name, type, value) => new LetNode(info, name, type, value),
            RequiredName: "let must have a name",
            RequiredDone: "let value was done, did you missed '='?",
            RequiredDefined: null!, // basically, unreachable: "let value is not defined",
            ValueOptional: true
        );

    static INode MutVariable(ref ManualTokens toks)
        => Variable(
            ref toks,
            // value cannot be null, optional is false
            Create: (info, name, type, value) => new MutNode(info, name, type, value!),
            RequiredName: "mut must have a name",
            RequiredDone: "mut value was done, did you missed '='?",
            RequiredDefined: "mut value is not defined",
            ValueOptional: false
        );

    static INode Function(ref ManualTokens toks)
    {
        Info info = toks.Context.info;

        var (arguments, body) = Scoped(
            ref toks,
            Self: (toks, tok) => toks.IsEmpty ? [] : Token.LeftSplit(tok, toks, [TokenType.Comma], Argument),
            Scope: (ref toks, tok) => Expression(ref toks),
            NoScope: (ref toks, tok) => Nothing.From(() => throw CompilationException.Create(tok.info, "function body is not defined"))
        ).Unwrap(v => v, v => default);

        return new FunctionValueNode(info, arguments, body);
    }

    static INode IfElse(ref ManualTokens toks, string Name="if")
    {
        Info info = toks.Context.info;

        var (Cond, (If, Else)) = Scoped(
            ref toks,
            Self: (tok, toks) => ExpressionBinary(toks, tok),
            Scope: (ref toks, tok) =>
            {
                var If = Expression(ref toks, EatDelimiter: false);
                var Else = toks.Switch(
                    Else: (ref _, tok) => throw CompilationExceptionList.UnexpectedEnd(tok.info),
                    Default: (ref _, tok) => throw CompilationException.Create(tok.info, $"{Name} operator must contain else branch after self"),
                    (TokenType.Elif, (ref toks, _) => IfElse(ref toks, "elif")),
                    (TokenType.Else, (ref toks, _) =>
                    {
                        toks.Get(TokenType.Set,
                            Else: (ref _, tok) => throw CompilationExceptionList.UnexpectedEnd(tok.info),
                            Fail: (ref _, tok) => throw CompilationException.Create(tok.info, "else body is not defined"),
                            Success: (ref _, _) => Nothing.Value
                        );
                        return Expression(ref toks, CommaEnd: true);
                    }
                )
                );
                return (If, Else);
            },
            NoScope: (ref toks, tok) => Nothing.From(() => throw CompilationException.Create(tok.info, $"{Name} body is not defined"))
        ).Unwrap(v => v, v => default);

        return new ConditionalNode(info, Cond, If, Else);
    }

    static (bool, INode) ExpressionUndelimited(ref ManualTokens toks)
        => toks.Start(
            OnEmpty: (ref _, tok) => throw CompilationException.Create(tok.info, "value is cannot be empty"),
            (ref toks) => toks.Switch(
                Else: (ref _, tok) => default, // unreachable
                Default: (ref toks, tok) => (Continue: false, ExpressionUnstructured(ref toks)),

                (TokenType.If, (ref toks, tok) => (Continue: false, IfElse(ref toks))),
                (TokenType.Mut, (ref toks, tok) => (Continue: true, MutVariable(ref toks))),
                (TokenType.Let, (ref toks, tok) => (Continue: true, LetVariable(ref toks))),
                (TokenType.Function, (ref toks, tok) => (Continue: false, Function(ref toks)))
            )
        );
}
