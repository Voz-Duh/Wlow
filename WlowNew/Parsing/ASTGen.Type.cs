using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static IMetaType Type(Token ctx, ref ManualTokens toks, bool optional = false)
        => toks.Start(
            OnEmpty: (ref _, tok) => optional ? PlaceHolderMetaType.Get : throw CompilationException.Create(ctx.info, "type is cannot be empty"),
            Do: (ref toks) =>
                toks.Switch<IMetaType>(
                    Else: null,
                    Default: (ref toks, tok) => optional ? PlaceHolderMetaType.Get : throw CompilationException.Create(tok.info, "unknown type"),
                    (TokenType.Int8, (ref _, _) => IntMetaType.Get8),
                    (TokenType.Int16, (ref _, _) => IntMetaType.Get16),
                    (TokenType.Int32, (ref _, _) => IntMetaType.Get32),
                    (TokenType.Int64, (ref _, _) => IntMetaType.Get64),
                    (TokenType.BitwiseAnd, (ref toks, tok) =>
                        new TypeOfMetaType(toks.Get(
                            Token: TokenType.Bracket,

                            Else: (ref _, _) => throw CompilationException.Create(tok.info, "brackets expected after '&' in type"),
                            Fail: (ref _, _) => throw CompilationException.Create(tok.info, "brackets expected after '&' in type"),
                            Success: (ref _, tok) => ManualTokens.Create(tok, tok.inner).Start(
                                OnEmpty: (ref _, ctx) => throw CompilationExceptionList.ValueCannotBeEmpty(ctx.info),
                                Do: (ref toks) => Expression(ref toks, FullScoped: true)
                            )
                        ))),
                    (TokenType.PlaceHolder, (ref _, _) => PlaceHolderMetaType.Get),
                    (TokenType.Not, (ref toks, tok) => NotMetaType.Get(Type(tok, ref toks)))
                )
        );
}
