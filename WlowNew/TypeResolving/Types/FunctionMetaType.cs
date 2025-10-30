using System.Collections.Immutable;
using Wlow.Parsing;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct FunctionMetaType(
    IMetaType result,
    ImmutableArray<TypedValueAnnot> arguments,
    FunctionDeclaration? declaration,
    bool fixated = false) : IMetaType
{
    readonly bool Fixated = fixated;
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
    public readonly ImmutableArray<TypedValueAnnot> Arguments = arguments;

    public override string ToString() => $"(fn {string.Join(", ", Arguments)} -> {Result})";
    public bool IsKnown => false;
    public Opt<uint> ByteSize => Opt<uint>.Hasnt();
    public TypeMutability Mutability => TypeMutability.Const;
    public Flg<TypeConvention> Convention => TypeConvention.InitVariable;

    private readonly struct FictiveScope
    {
        readonly Scope Scope;

        FictiveScope(Scope scope) => Scope = scope;
        public FictiveScope() => throw new InvalidOperationException("Use FictiveScope.Create to create a new fictive scope");

        public static FictiveScope Create() => new(Scope.Create());
        public static implicit operator Scope(FictiveScope fictive) => fictive.Scope;

        public IMetaType Arg(Info info, int index, TypedValueAnnot annot, FunctionMetaType function)
        {
            var value = annot >> (Scope, info);
            var arg = function.Declaration.Arguments[index];
            var type = WeakRefMetaType.From(value.Type);
            Scope.CreateArgument(default, TypeMutability.Const, arg.id, type);
            return type;
        }
    }

    public Nothing Binary(BinaryTypeBuilder bin, Info info)
    {
        bin.Push(BinaryTypeRepr.FunctionStart);
        if (_declaration is not null)
            bin.Push(Declaration.Identifier);
        Result.Binary(bin, info);

        var fictive_scope = FictiveScope.Create();
        for (int i = 0; i < Arguments.Length; i++)
        {
            TypedValueAnnot arg = Arguments[i];
            var type = fictive_scope.Arg(info, i, arg, this);
            type.Binary(bin, info);
        }

        bin.Push(BinaryTypeRepr.FunctionEnd);
        return Nothing.Value;
    }

    public IMetaType? UnwrapFn()
        => Fixated
        ? null
        : new FunctionMetaType(
            Result.Unwrap(),
            [.. Arguments],
            Declaration,
            true
        );

    public IFunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
    {
        var definition = Declaration.ResolveCall(ctx, info, args);

        return definition;
    }

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => IMetaType.SmartTypeSelect(
            ctx, info,
            this, to,
            (from, to) =>
            {
                // TODO function type template
                return null;

                if (to is not FunctionMetaType other)
                    return null;

                if (other.Arguments.Length != from.Arguments.Length)
                    throw CompilationException.Create(
                        info,
                        $"function with {from.Arguments.Length} waited arguments cannot be casted to function with {other.Arguments.Length} waited arguments"
                    );

                var fictive_scope = FictiveScope.Create();
                var fictive_scope_to = FictiveScope.Create();

                var args =
                    from.Arguments
                    .Select((arg, i) =>
                    {
                        var type = fictive_scope.Arg(info, i, arg, from).Unweak();

                        var argTo = other.Arguments[i];
                        try
                        {
                            return type.TemplateCast(ctx, info, argTo.Type >> (Scope.Empty, info));
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
                        [.. args.Select(v => TypedValueAnnot.From(TypedValue.From(v)))],
                        from.Declaration
                    );
                }
                catch (CompilationException e)
                {
                    throw CompilationException.Create(e.Info, $"at function return type: {e.BaseMessage}");
                }
            },
            is_template: true,
            repeat: repeat
        );
}
