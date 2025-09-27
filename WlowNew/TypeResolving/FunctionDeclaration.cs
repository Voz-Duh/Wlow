using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.Parsing;
using System.Collections.Concurrent;

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

    static readonly DMutex<ID> IdentifierGenerator = DMutex.From(ID.NegOne);
    public readonly ID Identifier = IdentifierGenerator.Request().Effect(v => v.Inc()).Done();

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

    public static void ResolveArgumentMutability(Info info, Mutability from, Mutability to, Mutability toType)
    {
        switch (to)
        {
            case Mutability.Copy:
                switch (toType)
                {
                    case Mutability.Mutate: throw CompilationException.Create(info, "primitive argument cannot get mutable type value");
                }
                break;
            case Mutability.Mutate:
                switch (from)
                {
                    case Mutability.Copy: throw CompilationException.Create(info, "mutable argument cannot get primitive value");
                    case Mutability.Const: throw CompilationException.Create(info, "mutable argument cannot get immutable value");
                }
                break;
            case Mutability.Const:
                switch (from)
                {
                    case Mutability.Copy: throw CompilationException.Create(info, "immutable argument cannot get primitive value");
                    case Mutability.Mutate: throw CompilationException.Create(info, "immutable argument cannot get mutable value");
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
                    ResolveArgumentMutability(v.info, v.value.Mutability, to.Mutability, to.Type.Mutability);

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
                    bin_type
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
        if (definitonsResolve.UnwrapValue1(out var def)) return def;
        var definitionToken = definitonsResolve.Unwrap(v => default!, v => v);

        i = 0;
        var type_scope = Scope.FictiveVariables(new(SelectKeyValuePairs(args, ref i, _arguments)));

        i = 0;
        var resolved_args = (ImmutableArray<TypedValue>)[.. SelectFunctionsResolved(args)];

        var result_resolved = TryDo(info, resolve_bin, () => _body.TypeResolve(type_scope));
        var result_type = result_resolved.ValueTypeInfo.Type;

        if (result_type.Mutability == Mutability.Mutate)
            throw CompilationException.Create(info, "mutable only type is cannot be returned");

        if (result_type.Unwrap() is PlaceHolderMetaType)
        {
            result_type = NeverMetaType.Get;
            result_resolved = new NeverResultNodeTypeResolved(_body.Info, result_resolved);
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

    // public ITypedValue Call(Scope sc, Info info, ImmutableArray<(Info info, ITypedValue value)> args)
    // {
    //     if (args.Length != _arguments.Length)
    //         throw CompilationException.Create(info, $"called function waiting for {_arguments.Length} but {args.Length} is passed");

    //     var arg_types = new IMetaType[args.Length];

    //     var result_args =
    //         SpecifyArguments(
    //             args,
    //             valid_selector: (i, val, type) => type.Type.Unwrap() is not FunctionMetaType,
    //             value_selector: (i, val, type, valid) => valid ? _castValue(sc, val.info, val.value, type) : default!,
    //             type_selector: (i, val, to) => new(
    //                 val.value.Mutability,
    //                 arg_types[i] = val.value.Type.ImplicitCast(sc, val.info, to.Type)
    //             )
    //         );

    //     var bin_type = new FunctionMetaType(
    //         PlaceHolderMetaType.Get,
    //         [.. result_args.Select(v => v.type)],
    //         null
    //     );
    //     var bin_builder = new BinaryTypeBuilder();
    //     bin_type.Binary(bin_builder);
    //     var bin = bin_builder.Done();

    //     var call_args = (ImmutableArray<ITypedValue>)[.. SelectIf(result_args, v => (v.include, v.value))];

    //     if (ResolvingStack.TryGetValue(bin, out var definition))
    //     {
    //         return _callValue(definition, call_args);
    //     }

    //     var function = CreateDefinition(
    //         sc,
    //         info,
    //         [.. arg_types.Select((v, i) => new ArgumentMetaType(_arguments[i].val.Mutability, v))],
    //         type_only: false,
    //         bin_type,
    //         bin
    //     );

    //     return _callValue(function, call_args);
    // }


    // private FunctionDefinition CreateDefinition(
    //     Scope base_scope,
    //     Info info,
    //     ImmutableArray<ArgumentMetaType> args,
    //     bool type_only,
    //     FunctionMetaType resolve_type,
    //     BinaryType resolve_bin)
    // {
    //     var i = 0;
    //     var real_args =
    //         new Dictionary<string, IMetaType>([
    //             .. SelectPairsNoFunctions(args, ref i, _arguments, selector: v => v.Type),
    //         ]);

    //     if (_definitions.TryGetValue(resolve_bin, out var defined))
    //         return defined;

    //     i = 0;
    //     var type_scope = base_scope.FictiveVariables(new(SelectKeyValuePairs(args, ref i, _arguments)));

    //     i = 0;
    //     var resolved_args = (ImmutableArray<ArgumentMetaType>)[.. SelectFunctionsResolved(args)];

    //     var result_type = TryDo(info, resolve_bin, () => _body.TypeResolve(type_scope));
    //     if (result_type.Unwrap() is PlaceHolderMetaType)
    //         result_type = PlaceHolderMetaType.Get;

    //     var func_type = new FunctionMetaType(
    //         result_type,
    //         resolved_args,
    //         declaration: this
    //     );

    //     if (type_only)
    //     {
    //         ResolvingStack.Remove(resolve_bin);
    //         return new(func_type, default);
    //     }

    //     var data = _createFunction(base_scope, func_type);

    //     var compiled =
    //         TryDo(
    //             info,
    //             resolve_bin,
    //             () => _compileFunction(
    //                 base_scope.Variables(
    //                     new(SelectVariablesFrom(
    //                         args,
    //                         ref i,
    //                         data,
    //                         _registerArgument,
    //                         base_scope,
    //                         _arguments))),
    //                 data,
    //                 func_type
    //             )
    //         );

    //     var result = new FunctionDefinition(
    //         func_type,
    //         compiled
    //     );

    //     _definitions[resolve_bin] = result;

    //     return result;
    // }

    // private static IEnumerable<TResult> SelectIf<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, (bool, TResult)> selector)
    // {
    //     foreach (TSource element in source)
    //     {
    //         var (add, value) = selector(element);
    //         if (add) yield return value;
    //     }
    // }

    // private static IEnumerable<KeyValuePair<string, ITypedValue>> SelectVariablesFrom(
    //     ImmutableArray<ArgumentMetaType> args,
    //     ref int i,
    //     FunctionCompileData data,
    //     RegisterArgument register,
    //     Scope base_scope,
    //     IEnumerable<Pair<string, ArgumentMetaType>> collection)
    // {
    //     var real_arg = 0;
    //     var j = i;
    //     var res =
    //         collection
    //         .Select(v =>
    //         {
    //             var type = args[j];
    //             if (type.Type.Unwrap() is FunctionMetaType meta)
    //             {
    //                 j++;
    //                 return KeyValuePair.Create(
    //                     v.id,
    //                     register(
    //                         base_scope,
    //                         0,
    //                         data,
    //                         type.Type,
    //                         meta.Declaration
    //                     )
    //                 );
    //             }
    //             j++;
    //             return KeyValuePair.Create(
    //                 v.id,
    //                 register(
    //                     base_scope,
    //                     real_arg++,
    //                     data,
    //                     type.Type,
    //                     null
    //                 )
    //             );
    //         });
    //     i = j;
    //     return res;
    // }

    // private static IEnumerable<KeyValuePair<string, Variable>> SelectVariablesFromValues(
    //     IMetaType[] args,
    //     ref uint i,
    //     LLVMValueRef llvm_function,
    //     Dictionary<string, LLVMValue> collection)
    // {
    //     var j = i;
    //     var res =
    //         from v in collection
    //         let type = args[j]
    //         select KeyValuePair.Create(
    //             v.Key,
    //             new Variable(
    //                 type,
    //                 llvm_function.GetParam(j++),
    //                 default,
    //                 VariableFlags.Setable
    //             )
    //         );
    //     i = j;
    //     return res;
    // }
}

