using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.Parsing;

namespace Wlow.TypeResolving;

public class FunctionDeclaration(
    Info info,
    IEnumerable<Pair<string, TypedValue>> arguments,
    INode body)
{
    [ThreadStatic]
    static Dictionary<BinaryType, FunctionDefinition>? ResolvingStackValue;
    static Dictionary<BinaryType, FunctionDefinition> ResolvingStack => ResolvingStackValue ??= [];

    [ThreadStatic]
    static Stack<Info>? CallingStackValue;
    static Stack<Info> CallingStack => CallingStackValue ??= [];

    static readonly DMutex<Dictionary<BinaryType, DMutex<FunctionDefinition>>> Definitions = new([]);
    public readonly ID Identifier = ID.Unqiue;

    readonly Info _info = info;
    readonly ImmutableArray<Pair<string, TypedValue>> _arguments = [.. arguments];
    readonly INode _body = body;

    public FunctionMetaType CreateType()
        => new(
            PlaceHolderMetaType.Get,
            [.. arguments.Select(v => v.val)],
            this
        );

    private static T TryDo<T>(
        Info info,
        BinaryType bin,
        Func<T> func,
        FunctionDefinition definition = default)
    {
        ResolvingStack[bin] = definition;
        CallingStack.Push(info);
        try
        {
            var res = func();
            CallingStack.Pop();
            ResolvingStack.Remove(bin);
            return res;
        }
        catch (CompilationException e)
        {
            if (e is StackedCompilationException) throw;
            var error = new StackedCompilationException([.. CallingStack], e.Info, e.BaseMessage, e);
            CallingStack.Pop();
            ResolvingStack.Remove(bin);
            throw error;
        }
    }

    public ImmutableArray<(bool include, TypedValue value, TypedValue type)> SpecifyArguments<TElement>(
        ImmutableArray<TElement> collection,
        Func<int, TElement, TypedValue,       /* return: */ bool> valid_selector,
        Func<int, TElement, TypedValue, bool, /* return: */ TypedValue> value_selector,
        Func<int, TElement, TypedValue,       /* return: */ TypedValue> type_selector)
        => [
            ..
            _arguments
            .Select((v, i) =>
            {
                var arg = collection[i];
                var type = type_selector(i, arg, v.val);
                var valid = valid_selector(i, arg, type);

                return (valid, value_selector(i++, arg, type, valid), type);
            })
        ];

    public static void ResolveArgumentMutability(Info info, TypeMutability from, TypeMutability to, TypeMutability toType)
    {
        switch (to)
        {
            case TypeMutability.Copy:
                switch (toType)
                {
                    case TypeMutability.Mutate: throw CompilationException.Create(info, "primitive argument cannot get mutable type value");
                }
                break;
            case TypeMutability.Mutate:
                switch (from)
                {
                    case TypeMutability.Copy: throw CompilationException.Create(info, "mutable argument cannot get primitive value");
                    case TypeMutability.Const: throw CompilationException.Create(info, "mutable argument cannot get immutable value");
                }
                break;
            case TypeMutability.Const:
                switch (from)
                {
                    case TypeMutability.Copy: throw CompilationException.Create(info, "immutable argument cannot get primitive value");
                    case TypeMutability.Mutate: throw CompilationException.Create(info, "immutable argument cannot get mutable value");
                }
                break;
        }
    }

    public FunctionDefinition ResolveCall(
        Scope sc,
        Info info,
        ImmutableArray<(Info info, TypedValue value)> args)
    {
        if (args.Length != _arguments.Length)
            throw CompilationException.Create(info, $"called function waiting for {_arguments.Length} arguments but {args.Length} is passed");

        var arg_types =
            SpecifyArguments(
                args,
                valid_selector: (i, v, t) => true,
                value_selector: (i, v, t, a) => default!,
                type_selector: (i, v, to) =>
                {
                    ResolveArgumentMutability(v.info, v.value.Mutability, to.Mutability, to.Type.Mutability(sc));

                    return new(
                        to.Mutability,
                        v.value.Type.ImplicitCast(sc, v.info, to.Type)
                    );
                }
            )
            .Select(v => v.type)
            .ToImmutableArray();

        var bin_type = new FunctionMetaType(
            PlaceHolderMetaType.Get,
            arg_types,
            declaration: this
        );
        var bin_builder = new BinaryTypeBuilder();
        bin_type.Binary(bin_builder);
        var bin = bin_builder.Done();

        if (ResolvingStack.TryGetValue(bin, out var resolve))
        {
            if (resolve.Node == null)
                return new FunctionDefinition(
                    null!,
                    new FunctionMetaType(
                        new ResolveMetaType(),
                        arg_types,
                        declaration: this
                    )
                );
            else
                return resolve;
        }

        return ResolveDefinition(
            sc,
            info,
            arg_types,
            bin,
            bin_type
        );
    }

    private FunctionDefinition ResolveDefinition(
        Scope base_scope,
        Info info,
        ImmutableArray<TypedValue> args,
        BinaryType resolve_bin,
        FunctionMetaType bin_type)
    {
        var i = 0;
        var real_args =
            new Dictionary<string, IMetaType>([
                .. SelectPairsNoFunctions(args, ref i, _arguments, selector: v => v.Type),
            ]);

        var definitonsResolve =
            Definitions.Request()
            .EffectResult<Or<FunctionDefinition, DMutex<FunctionDefinition>.Access>>(def =>
            {
                if (def.TryGetValue(resolve_bin, out var defined))
                    return defined.RequestValue();

                var mutex = DMutex.From(new FunctionDefinition());
                def[resolve_bin] = mutex;
                return mutex.Request();
            });
        if (definitonsResolve.UnwrapInline(
                out var def,
                out var definitionToken))
            return def;

        i = 0;
        var type_scope = Scope.FictiveVariables(new(SelectKeyValuePairs(args, ref i, _arguments)));

        i = 0;
        var resolved_args = (ImmutableArray<TypedValue>)[.. SelectFunctionsResolved(args)];

        var result_resolved = TryDo(info, resolve_bin, () => _body.TypeResolve(type_scope));
        var result_type = result_resolved.ValueTypeInfo.Type.Unwrap();

        if (result_type is PlaceHolderMetaType)
        {
            result_type = NeverMetaType.Get;
            result_resolved = new NeverResultNodeTypeResolved(_body.Info, result_resolved);
        }

        if (result_type is NotMetaType not)
            type_scope.FinalizeErrorType(not.To);
        else if (type_scope.IsError)
        {
            type_scope.FinalizeErrorType(result_type);
            result_type = NotMetaType.Get(result_type);
        }

        if (!result_type.Convention(type_scope) << TypeConvention.Return)
        {
            throw CompilationException.Create(_body.Info, $"type {result_type.Name} of returned value is not suitable for returning");
        }

        var definition = new FunctionDefinition(
            result_resolved,
            new FunctionMetaType(
                result_type,
                bin_type.Arguments,
                declaration: this
            )
        );

        definitionToken.Value = definition;
        definitionToken.Done();

        return definition;
    }
    private static IEnumerable<TypedValue> SelectFunctionsResolved(
        ImmutableArray<TypedValue> collection)
        =>
        from v in collection
        where v.Type.Unwrap() is not FunctionMetaType
        select v;

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectPairsNoFunctions<T>(
        ImmutableArray<TypedValue> args,
        ref int i,
        ImmutableArray<Pair<string, T>> collection,
        Func<T, IMetaType> selector)
    {
        var j = i;
        var res =
            collection
            .Where(v =>
            {
                var skip = selector(v.val).Unwrap() is FunctionMetaType;
                if (skip) j++;
                return !skip;
            })
            .Select(v => KeyValuePair.Create(
                v.id,
                args[j++].Type
            ));
        i = j;
        return res;
    }

    private static IEnumerable<KeyValuePair<string, TypedValue?>> SelectKeyValuePairs<T>(
        ImmutableArray<TypedValue> args,
        ref int i,
        ImmutableArray<Pair<string, T>> collection)
    {
        var j = i;
        var res =
            collection
            .Select(v => KeyValuePair.Create(
                v.id,
                args[j++] as TypedValue?
            ));
        i = j;
        return res;
    }
}

