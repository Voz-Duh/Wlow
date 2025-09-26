using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static Pair<string, TypedValue> Argument(Token ctx, ReadOnlySpan<Token> inner)
        => ManualTokens.Create(ctx, inner).Start(
            OnEmpty: tok => throw CompilationException.Create(tok.info, "function argument is cannot be empty"),
            Do: (ref toks) =>
            {
                var mutability = toks.Switch(
                    Else: null,
                    Default: (ref _, _) => Mutability.Copy,
                    (TokenType.Let, (ref _, _) => Mutability.Const),
                    (TokenType.Mut, (ref _, _) => Mutability.Mutate)
                );

                static Token nameRequired(Token tok)
                    => throw CompilationException.Create(tok.info, "function argument must have a name");
                var name = toks.Get(
                    Token: TokenType.Ident,
                    Else: nameRequired,
                    Fail: (ref _, tok) => nameRequired(tok),
                    Success: (ref _, ctx) => ctx
                );

                var type = Type(name, ref toks, optional: true);

                toks.Done(Fail: (tok) => throw CompilationException.Create(tok.info, "function argument was done, did you missed comma?"));

                return Pair.From(name.value, new TypedValue(mutability, type));
            }
        );
}
