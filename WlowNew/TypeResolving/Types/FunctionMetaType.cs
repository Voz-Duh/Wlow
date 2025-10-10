using System.Collections.Immutable;
using Wlow.Parsing;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct FunctionMetaType(
    IMetaType result,
    ImmutableArray<TypedValue>? arguments,
    FunctionDeclaration? declaration) : IMetaType
{
    readonly FunctionDeclaration? _declaration = declaration;
    public FunctionDeclaration Declaration
    {
        get
        {
            if (_declaration is null)
                throw new NotSupportedException("NNE: declaration is null");
            return _declaration;
        }
    }

    public IMetaType Result { get; init; } = result;
    public ImmutableArray<TypedValue> Arguments { get; init; } = arguments!.Value;

    public string Name => $"(fn {string.Join(", ", Arguments)} -> {Result.Name})";
    public TypeMutability Mutability(Scope ctx) => TypeMutability.Const;
    public Flg<TypeConvention> Convention(Scope ctx) => TypeConvention.InitVariable;

    public Nothing Binary(BinaryTypeBuilder bin)
    {
        bin.Push(BinaryTypeRepr.FunctionStart);
        if (_declaration is not null)
            bin.Push(Declaration.Identifier);
        Result.Binary(bin);
        foreach (var arg in Arguments)
            arg.Type.Binary(bin);
        bin.Push(BinaryTypeRepr.FunctionEnd);
        return Nothing.Value;
    }

    public FunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
    {
        var definition = Declaration.ResolveCall(ctx, info, args);

        return definition;
    }

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => IMetaType.SmartTypeSelect(
            ctx, info,
            this, to,
            (from, to) => {
                if (to is not FunctionMetaType other)
                    return null;

                if (other.Arguments.Length != from.Arguments.Length)
                    throw CompilationException.Create(
                        info,
                        $"function with {from.Arguments.Length} waited arguments cannot be casted to function with {other.Arguments.Length} waited arguments"
                    );

                var args =
                    from.Arguments
                    .Select((arg, i) =>
                    {
                        var argTo = other.Arguments[i];
                        try
                        {
                            return arg.TemplateCast(ctx, info, argTo.Type);
                        }
                        catch (CompilationException e)
                        {
                            throw CompilationException.Create(e.Info, $"at function argument {i + 1}: {e.BaseMessage}");
                        }
                    })
                    .ToImmutableArray();

                try
                {
                    return new FunctionMetaType(
                        from.Result.TemplateCast(ctx, info, other.Result),
                        args,
                        from.Declaration
                    );
                }
                catch (CompilationException e)
                {
                    throw CompilationException.Create(e.Info, $"at function return type: {e.BaseMessage}");
                }
            }
        );
}
