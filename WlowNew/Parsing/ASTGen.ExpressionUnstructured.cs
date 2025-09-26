
using System.Collections.Immutable;
using System.Numerics;
using Wlow.TypeResolving;
using Wlow.Shared;

using NodeOrEffect = Wlow.Shared.Or<Wlow.Parsing.INode, System.Func<Wlow.Parsing.INode, Wlow.Parsing.INode>>;
using OptEffect = Wlow.Shared.Opt<System.Func<Wlow.Parsing.INode, Wlow.Parsing.INode>>;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static INode ExpressionUnstructured(ref ManualTokens toks)
    {
        var res = toks.UntilWithNot(
            [TokenType.Call],
            [TokenType.Delimiter, TokenType.ContinueDelimiter],
            Success: (toks, tok) => ExpressionBinary(tok, toks),
            After: (ref toks, tok) =>
            {
                var startEnds = toks.Switch(
                    Else: _ => false,
                    Default: (ref _, tok) => false,
                    (TokenType.Delimiter, (ref _, _) => true),
                    (TokenType.ContinueDelimiter, (ref _, _) => true)
                );
                if (startEnds)
                {
                    toks.StepBack();
                    return (tok.info, []);
                }

                var args = new List<INode>();
                do
                {
                    var node = Expression(ref toks, CommaEnd: true);
                    var (ends, delimiter) = toks.Switch(
                        // no comma = end
                        Else: _ => (true, false),
                        // no comma, but some token = err
                        Default: (ref _, tok) => throw CompilationExceptionList.ExpressionContinue(tok.info),
                        // basic delimiter = continue
                        (TokenType.Delimiter, (ref _, _) => (true, true)),
                        // continue delimiter = end
                        (TokenType.ContinueDelimiter, (ref _, _) => (true, true)),
                        // comma = continue
                        (TokenType.Comma, (ref _, _) => (false, false))
                    );
                    args.Add(node);
                    if (ends)
                    {
                        if (delimiter) toks.StepBack();
                        break;
                    }
                }
                while (true);
                return (tok.info, args: args.ToImmutableArray());
            },
            Fail: (ref toks, tok) => Nothing.Value
        );

        if (res.UnwrapValue1(out var succ))
        {
            return new CallNode(succ.after.info, succ.value, succ.after.args);
        }

        var var = toks.Until(
            /* comma is here because only two types of expressions is provided:
                1. expression from block                 - comma here is an error which is will be handled higher
                2. expression from comma separated block - comma here is a delimiter
            */
            Tokens: [TokenType.Delimiter, TokenType.ContinueDelimiter, TokenType.Comma],
            Success: (toks, tok) => ExpressionBinary(tok, toks),
            // ignore right part
            // step back to make higher scope see the delimitier
            // ignored because a caret move effect 
            After: (ref toks, _) => toks.StepBack(),
            Fail: (ref toks, tok) =>
            {
                var node = ExpressionBinary(tok, toks.AllRightIncludeCurrent());
                toks.ToEnd();
                return node;
            }
        );

        return var.Unwrap(v => v.value, v => v);
    }

    static INode ExpressionBinary(Token ctx, ReadOnlySpan<Token> toks)
        => ExpressionLogicalAnd(ctx, toks);

    static INode ExpressionLogicalAnd(Token ctx, ReadOnlySpan<Token> toks)
        => Token.LeftParseExpression(
            ctx, toks,
            [TokenType.LogicalAnd],
            ExpressionLogicalOr,
            (tok, a, b) => new LogicalAndNode(tok.info, a, b)
        );

    static INode ExpressionLogicalOr(Token ctx, ReadOnlySpan<Token> toks)
        => Token.LeftParseExpression(
            ctx, toks,
            [TokenType.LogicalOr],
            ExpressionComparison,
            (tok, a, b) => new LogicalOrNode(tok.info, a, b)
        );

    static INode ExpressionComparison(Token ctx, ReadOnlySpan<Token> toks)
        => Token.LeftParseExpression(
            ctx, toks,
            [
                TokenType.Equals,  TokenType.NotEquals,
                TokenType.Lower,   TokenType.LowerEquals,
                TokenType.Greater, TokenType.GreaterEquals
            ],
            ExpressionAddition,
            (tok, a, b) => tok.type switch
            {
                TokenType.Equals => new EqualsNode(tok.info, a, b),
                TokenType.NotEquals => new NotEqualsNode(tok.info, a, b),
                TokenType.Lower => new LowerNode(tok.info, a, b),
                TokenType.LowerEquals => new LowerEqualsNode(tok.info, a, b),
                TokenType.Greater => new GreaterNode(tok.info, a, b),
                TokenType.GreaterEquals => new GreaterEqualsNode(tok.info, a, b),
                _ => throw new NotImplementedException(),
            }
        );

    static INode ExpressionAddition(Token ctx, ReadOnlySpan<Token> toks)
        => Token.LeftParseExpression(
            ctx, toks,
            [
                TokenType.Add,        TokenType.Sub,
                TokenType.BitwiseAnd, TokenType.BitwiseOr,
            ],
            ExpressionMultipilication,
            (tok, a, b) => tok.type switch
            {
                TokenType.Add => new AddNode(tok.info, a, b),
                TokenType.Sub => new SubNode(tok.info, a, b),
                TokenType.BitwiseAnd => new BitwiseAndNode(tok.info, a, b),
                TokenType.BitwiseOr => new BitwiseOrNode(tok.info, a, b),
                _ => throw new NotImplementedException(),
            }
        );

    static INode ExpressionMultipilication(Token ctx, ReadOnlySpan<Token> toks)
        => Token.RightParseExpression(
            ctx, toks,
            [
                TokenType.Mul, TokenType.Div,
                TokenType.Mod,
                TokenType.Shr, TokenType.Shl,
                TokenType.Ror, TokenType.Rol,
            ],
            ExpressionUnaryAdapter,
            (tok, a, b) => tok.type switch
            {
                TokenType.Mul => new MulNode(tok.info, a, b),
                TokenType.Div => new DivNode(tok.info, a, b),
                TokenType.Mod => new ModNode(tok.info, a, b),
                TokenType.Shr => new ShrNode(tok.info, a, b),
                TokenType.Shl => new ShlNode(tok.info, a, b),
                TokenType.Ror => new RorNode(tok.info, a, b),
                TokenType.Rol => new RolNode(tok.info, a, b),
                _ => throw new NotImplementedException(),
            }
        );

    static INode ExpressionUnaryAdapter(Token ctx, ReadOnlySpan<Token> toks)
    {
        var inner = ManualTokens.Create(ctx, toks);
        return inner.Start(tok => default!, ExpressionUnary);
    }

    static INode ExpressionUnary(ref ManualTokens toks)
    {
        INode node = null!;

        Monad<INode> prefix = new();
        while (
            toks.Switch(
                Else: tok => throw CompilationException.Create(tok.info, "unexpected expression end"),
                Default: (ref toks, tok) => NodeOrEffect.Create(ExpressionAtomic(ref toks)),
                (TokenType.Sub,
                    (ref toks, tok) => NodeOrEffect.Create(v => new NegateNode(tok.info, v))),
                (TokenType.Add,
                    (ref toks, tok) => NodeOrEffect.Create(v => new PlusNode(tok.info, v))),
                (TokenType.Mul,
                    (ref toks, tok) => NodeOrEffect.Create(v => new DerefNode(tok.info, v))),
                (TokenType.BitwiseAnd,
                    (ref toks, tok) => NodeOrEffect.Create(v => new RefNode(tok.info, v))),
                (TokenType.Not,
                    (ref toks, tok) => NodeOrEffect.Create(v => new NotNode(tok.info, v))),
                (TokenType.Xor,
                    (ref toks, tok) => NodeOrEffect.Create(v => new InvNode(tok.info, v)))
            ).Unwrap(
                nod => (node = nod).Return(false),
                effect => (prefix >>> effect).Return(true)
            )) ;

        Monad<INode> suffix = new();
        while (
            toks.Switch(
                Else: tok => OptEffect.Hasnt(),
                Default: (ref _, tok) => throw CompilationExceptionList.ExpressionContinue(tok.info),
                (TokenType.PlaceHolder,
                    (ref toks, tok) => OptEffect.From(v => new HandleHigherNode(tok.info, v))),
                (TokenType.Not,
                    (ref toks, tok) => OptEffect.From(v => new HandlePanicNode(tok.info, v)))
            )
            .Unwrap(false, effect => (suffix >>> effect).Return(true))) ;

        return prefix[suffix[node]];
    }

    static INode ExpressionAtomic(ref ManualTokens toks)
        => toks.Switch(
            Else: tok => throw CompilationExceptionList.ValueCannotBeEmpty(tok.info),
            Default: (ref toks, tok) => throw CompilationExceptionList.ExpressionInvalid(tok.info),
            (TokenType.Ident, (ref toks, tok) => new IdentNode(tok.info, tok.value)),
            (TokenType.Number, (ref toks, tok) => new IntegerNode(tok.info, BigInteger.Parse(tok.value), IntMetaType.Get32)),
            (TokenType.Bracket, (ref toks, tok) =>
            {
                var inner = ManualTokens.Create(tok, tok.inner);
                return inner.Start(
                    OnEmpty: tok => throw CompilationExceptionList.ValueCannotBeEmpty(tok.info),
                    Do: (ref toks) => Expression(ref toks, FullScoped: true)
                );
            })
        );
}
