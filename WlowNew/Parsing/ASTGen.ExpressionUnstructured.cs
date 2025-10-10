
using System.Collections.Immutable;
using System.Numerics;
using Wlow.TypeResolving;
using Wlow.Shared;

using NodeOrEffect = Wlow.Shared.Or<Wlow.Parsing.INode, System.Func<Wlow.Parsing.INode, Wlow.Parsing.INode>>;
using OptEffect = Wlow.Shared.Opt<System.Func<Wlow.Parsing.INode, Wlow.Parsing.INode>>;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static Func<INode, INode> SetterEffect(Token tok, INode expr)
        => v => tok.type switch
        {
            TokenType.Set => expr,
            TokenType.SetAdd => new AddNode(tok.info, v, expr),
            TokenType.SetSub => new SubNode(tok.info, v, expr),
            TokenType.SetMul => new MulNode(tok.info, v, expr),
            TokenType.SetDiv => new DivNode(tok.info, v, expr),
            TokenType.SetMod => new ModNode(tok.info, v, expr),
            TokenType.SetXor => new XorNode(tok.info, v, expr),
            TokenType.SetRor => new RorNode(tok.info, v, expr),
            TokenType.SetRol => new RolNode(tok.info, v, expr),
            TokenType.SetShr => new ShrNode(tok.info, v, expr),
            TokenType.SetShl => new ShlNode(tok.info, v, expr),
            TokenType.SetBitwiseOr => new BitwiseOrNode(tok.info, v, expr),
            TokenType.SetBitwiseAnd => new BitwiseAndNode(tok.info, v, expr),
            _ => throw new NotSupportedException(),
        };

    static INode SetterIdent(INode v) => v;

    static Func<INode, INode>? ParseForSetterEffect(ref ManualTokens toks)
        => toks.Any<Func<INode, INode>?>(
            [TokenType.Set,
             TokenType.SetAdd,    TokenType.SetSub,
             TokenType.SetMul,    TokenType.SetDiv,
             TokenType.SetMod,    TokenType.SetXor,
             TokenType.SetRor,    TokenType.SetRol,
             TokenType.SetShr,    TokenType.SetShl,
             TokenType.SetBitwiseOr, TokenType.SetBitwiseAnd],
            Else: (ref _, _) => null,
            After: (ref toks, tok) => SetterEffect(tok, Expression(ref toks, CommaEnd: true)),
            Fail: (ref _, tok) => null
        );

    static INode ExpressionUnstructured(ref ManualTokens toks)
    {
#region Setters
        {
            var res = toks.UntilWithNot<INode, (Token tok, Func<INode, INode> effect), Nothing>(
                [TokenType.Set,
                 TokenType.SetAdd,    TokenType.SetSub,
                 TokenType.SetMul,    TokenType.SetDiv,
                 TokenType.SetMod,    TokenType.SetXor,
                 TokenType.SetRor,    TokenType.SetRol,
                 TokenType.SetShr,    TokenType.SetShl,
                 TokenType.SetBitwiseOr, TokenType.SetBitwiseAnd],
                [TokenType.Delimiter, TokenType.ContinueDelimiter],
                Success: (toks, tok) => ExpressionBinary(tok, toks),
                After: (ref toks, tok) => {
                    var expr = Expression(ref toks, CommaEnd: true);
                    return (tok, SetterEffect(tok, expr));
                },
                Fail: (ref toks, tok) => Nothing.Value
            );

            if (res.UnwrapInline(out var succ, out var _))
            {
                return new SetNode(succ.after.tok.info, succ.value, succ.after.effect(succ.value));
            }
        }
#endregion Setters

#region Function Call
        {
            var res = toks.UntilWithNot(
                [TokenType.Call],
                [TokenType.Delimiter, TokenType.ContinueDelimiter],
                Success: (toks, tok) => ExpressionBinary(tok, toks),
                After: (ref toks, tok) =>
                {
                    var startEnds = toks.Switch(
                        Else: (ref _, _) => false,
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
                    if (toks.Switch(
                        Else: (ref toks, _) => false.Effect(toks.StepIgnore()),
                        Default: (ref toks, _) => true.Effect(toks.StepIgnore()),
                        (TokenType.Delimiter, (ref _, _) => false),
                        (TokenType.ContinueDelimiter, (ref _, _) => false),
                        (TokenType.Comma, (ref toks, _) => false)
                    ).Effect(toks.StepBack()))
                        while (true)
                        {
                            var node = Expression(ref toks, CommaEnd: true);
                            var (ends, delimiter) = toks.Switch(
                                // no comma = end
                                Else: (ref _, _) => (true, false),
                                // no comma, but some token = err
                                Default: (ref _, tok) => throw CompilationExceptionList.ExpressionContinue(tok.info),
                                // basic delimiter = end
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
                    return (tok.info, args: args.ToImmutableArray());
                },
                Fail: (ref toks, tok) => Nothing.Value
            );

            if (res.UnwrapInline(out var succ, out var _))
            {
                return new CallNode(succ.after.info, succ.value, succ.after.args);
            }
        }
#endregion Function Call

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
        return inner.Start((ref _, tok) => default!, ExpressionUnary);
    }

    static INode ExpressionUnary(ref ManualTokens toks)
    {
        INode node = null!;

        Monad<INode> prefix = new();
        while (
            toks.Switch(
                Else: (ref _, tok) => throw CompilationExceptionList.UnexpectedEnd(tok.info),
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
        OptEffect dotsuffix(ref ManualTokens toks, Token dot)
            => toks.Switch(
                (ref _, tok) => throw CompilationExceptionList.UnexpectedEnd(tok.info),
                (ref _, tok) => throw CompilationExceptionList.Expected(tok.info, "field name"),
                (TokenType.Ident,
                    (ref toks, tok) => OptEffect.From(v => new AccessNameNode(dot.info, v, tok.value))),
                (TokenType.UNum,
                    (ref toks, tok) => OptEffect.From(v => new AccessIndexNode(tok.info, v, int.Parse(tok.value)))),
                (TokenType.FNum, (ref toks, tok) =>
                {
                    var parts = tok.value.Split('.');
                    var left = parts[0];
                    var right = parts[1];
                    Func<INode, INode> effect;
                    if (right.Length == 0)
                        effect = dotsuffix(ref toks, tok).Unwrap(null!, eff => eff);
                    else
                        effect = v => new AccessIndexNode(tok.info, v, int.Parse(right));
                    return OptEffect.From(
                        v => effect(new AccessIndexNode(tok.info, v, int.Parse(left)))
                    );
                }));

        while (
            toks.Switch(
                Else: (ref _, tok) => OptEffect.Hasnt(),
                Default: (ref _, tok) => throw CompilationExceptionList.ExpressionContinue(tok.info),
                (TokenType.Mul,
                    (ref toks, tok) => OptEffect.From(v => new DerefNode(tok.info, v))),
                (TokenType.PlaceHolder,
                    (ref toks, tok) => OptEffect.From(v => new HandleHigherNode(tok.info, v))),
                (TokenType.Not,
                    (ref toks, tok) => OptEffect.From(v => new HandlePanicNode(tok.info, v))),
                (TokenType.LDNum,
                    (ref toks, tok) => OptEffect.From(v => new AccessIndexNode(tok.info, v, int.Parse(tok.value.AsSpan()[1..])))),
                (TokenType.Dot, dotsuffix)
            )
            .Unwrap(false, effect => (suffix >>> effect).Return(true))) ;

        return prefix[suffix[node]];
    }

    static INode ExpressionAtomic(ref ManualTokens toks)
        => toks.Switch<INode>(
            Else: (ref _, tok) => throw CompilationExceptionList.ValueCannotBeEmpty(tok.info),
            Default: (ref toks, tok) => throw CompilationExceptionList.ExpressionInvalid(tok.info),
            (TokenType.Ident, (ref toks, tok) => new IdentNode(tok.info, tok.value)),
            (TokenType.UNum, (ref toks, tok) => new IntegerNode(tok.info, BigInteger.Parse(tok.value), IntMetaType.Get32)),
            (TokenType.INum, (ref toks, tok) => new IntegerNode(tok.info, BigInteger.Parse(tok.value), IntMetaType.Get32)),
            (TokenType.In, (ref toks, tok) => {
                static (Info, string) error(ref ManualTokens _, Token tok)
                    => throw CompilationExceptionList.Expected(tok.info, "variable name");
                var (name_info, name) = toks.Get(
                    TokenType.Ident,
                    Else: error,
                    Fail: error,
                    Success: (ref _, tok) => (tok.info, tok.value)
                );

                var effect = ParseForSetterEffect(ref toks);
                INode jump = new InNode(tok.info, name);
                
                return effect is not null
                    ? new DelimitedStepsNode(
                        [effect(new IdentNode(name_info, name))],
                        jump)
                    : jump;
            }),
            (TokenType.Fail, (ref toks, tok) => {
                // TODO fail with message, optional data etc what i cna imagine
                return new FailNode(tok.info, new IntegerNode(tok.info, 1, IntMetaType.Get8));
            }),
            (TokenType.Bracket, (ref toks, tok) =>
            {
                var inner = ManualTokens.Create(tok, tok.inner);
                return new ScopeNode(
                    tok.info,
                    inner.Start(
                        OnEmpty: (ref _, tok) => throw CompilationExceptionList.ValueCannotBeEmpty(tok.info),
                        Do: (ref toks) => Expression(ref toks, FullScoped: true)
                    )
                ) as INode;
            })
        );
}
