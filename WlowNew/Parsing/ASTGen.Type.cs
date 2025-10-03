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
                    (TokenType.Int32, (ref _, _) => IntMetaType.Get32),
                    (TokenType.PlaceHolder, (ref _, _) => PlaceHolderMetaType.Get),
                    (TokenType.Not, (ref toks, tok) => NotMetaType.Get(Type(tok, ref toks)))
                )
        );
}
