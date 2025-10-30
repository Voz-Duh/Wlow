using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static Pair<string, TypedValueAnnot> Argument(Token ctx, ReadOnlySpan<Token> inner)
        => ManualTokens.Create(ctx, inner).Start(
            OnEmpty: (ref _, tok) => throw CompilationException.Create(tok.info, "function argument is cannot be empty"),
            Do: (ref toks) =>
            {
                var mutability = toks.Switch(
                    Else: null,
                    Default: (ref _, _) => TypeMutability.Copy,
                    (TokenType.Let, (ref _, _) => TypeMutability.Const),
                    (TokenType.Mut, (ref _, _) => TypeMutability.Mutate)
                );

                static Token nameRequired(ref ManualTokens _, Token tok)
                    => throw CompilationException.Create(tok.info, "function argument must have a name");
                var name = toks.Get(
                    Token: TokenType.Ident,
                    Else: nameRequired,
                    Fail: nameRequired,
                    Success: (ref _, ctx) => ctx
                );

                var type = Type(name, ref toks, optional: true);

                toks.Done(Fail: tok => throw CompilationException.Create(tok.info, "function argument was done, did you missed comma?"));

                return Pair.From(name.value, new TypedValueAnnot(mutability, type));
            }
        );
}
