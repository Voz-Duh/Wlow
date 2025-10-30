using Wlow.Shared;

namespace Wlow.Parsing;

public partial class ASTGen
{
    static TypeAnnot Type(Token ctx, ref ManualTokens toks, bool optional = false)
        => toks.Start(
            OnEmpty: (ref _, tok) => optional ? TypeAnnot.Placeholder : throw CompilationException.Create(ctx.info, "type is cannot be empty"),
            Do: (ref toks) =>
                toks.Switch(
                    Else: null,
                    Default: (ref toks, tok) => optional ? TypeAnnot.Placeholder : throw CompilationException.Create(tok.info, "unknown type"),
                    (TokenType.Int8, (ref _, _) => TypeAnnot.Int8),
                    (TokenType.Int16, (ref _, _) => TypeAnnot.Int16),
                    (TokenType.Int32, (ref _, _) => TypeAnnot.Int32),
                    (TokenType.Int64, (ref _, _) => TypeAnnot.Int64),
                    (TokenType.UInt8, (ref _, _) => TypeAnnot.UInt8),
                    (TokenType.UInt16, (ref _, _) => TypeAnnot.UInt16),
                    (TokenType.UInt32, (ref _, _) => TypeAnnot.UInt32),
                    (TokenType.UInt64, (ref _, _) => TypeAnnot.UInt64),
                    // TODO implement float types
                    (TokenType.Float8, (ref _, _) => throw new NotImplementedException()),
                    (TokenType.Float16, (ref _, _) => throw new NotImplementedException()),
                    (TokenType.Float32, (ref _, _) => throw new NotImplementedException()),
                    (TokenType.Float64, (ref _, _) => throw new NotImplementedException()),
                    (TokenType.Float128, (ref _, _) => throw new NotImplementedException()),
                    (TokenType.BitwiseAnd, (ref toks, tok) =>
                        TypeAnnot.TypeOf(
                            toks.Get(
                                Token: TokenType.Bracket,

                                Else: (ref _, _) => throw CompilationException.Create(tok.info, "brackets expected after '&' in type"),
                                Fail: (ref _, _) => throw CompilationException.Create(tok.info, "brackets expected after '&' in type"),
                                Success: (ref _, tok) => ManualTokens.Create(tok, tok.inner).Start(
                                    OnEmpty: (ref _, ctx) => throw CompilationExceptionList.ValueCannotBeEmpty(ctx.info),
                                    Do: (ref toks) => Expression(ref toks, FullScoped: true)
                                )
                            )
                        )),
                    (TokenType.PlaceHolder, (ref _, _) => TypeAnnot.Placeholder),
                    (TokenType.Not, (ref toks, tok) => TypeAnnot.Error(Type(tok, ref toks)))
                )
        );
}
