
using System.Numerics;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow;

public readonly record struct FunctionDefinition(FunctionMeta type, LLVMTypeRef llvm_type, LLVMValueRef llvm_value);

public class FunctionDecl(
    Info info,
    Dictionary<string, IMetaType> arguments,
    IValue block,
    Dictionary<string, FunctionDefinition> definitions = null,
    BigInteger? unique_number = null)
{
    [ThreadStatic]
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStackValue;
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStack => ResolvingStackValue ??= new();
    private static BigInteger UniqueNumberGenerator = 0;
    public readonly BigInteger UniqueNumber = unique_number ?? UniqueNumberGenerator++;

    public Info info
    { get; init; } = info;
    public readonly Dictionary<string, IMetaType> arguments = arguments;
    public readonly IValue block = block;

    private readonly Dictionary<string, FunctionDefinition> Definitions = definitions ?? [];

    public LLVMValue CastTo(Scope sc, Info info, FunctionMeta to)
    {
        if (to.arguments.Length != arguments.Count)
            throw new($"{info} function with {arguments.Count} waited arguments cannot be casted to function with {to.arguments.Length} waited arguments");

        var i = 0;
        var args = new Dictionary<string, IMetaType>(
            arguments
            .Select(v =>
            {
                var arg = to.arguments[i++];
                try
                {
                    if (v.Value is FunctionMeta meta)
                    {
                        return KeyValuePair.Create(v.Key, CastTo(sc, info, meta).type);
                    }
                    return KeyValuePair.Create(v.Key, arg.ImplicitCast(sc, info, v.Value));
                }
                catch (Exception e)
                {
                    throw new($"{e.Message} at argument {i + 1}");
                }
            })
        );

        var type = new FunctionMeta(
            [
                ..
                args.Select(v =>  v.Value)
            ],
            result: GenericMeta.Get,
            declaration: this
        );

        if (!type.IsGeneric() && Definitions.TryGetValue(type.AsBin(), out var definition))
        {
            return new(definition.type, definition.llvm_value);
        }

        return new(type, function: new(info, args, block, Definitions));
    }

    public FunctionMeta CallFunctionType(Scope sc, Info info, IMetaType[] args)
    {
        var bin_type = new FunctionMeta([.. args], VoidMeta.Get);
        var bin = bin_type.AsBin();

        if (ResolvingStack.ContainsKey((UniqueNumber, bin)))
            return new FunctionMeta(args, GenericMeta.Get);

        if (args.Length != arguments.Count)
            throw new($"{info} called function waiting for {arguments.Count} arguments but {args.Length} is passed");

        var i = 0u;
        var arg_types =
            (IMetaType[])[
                ..
                arguments
                .Where(v =>
                {
                    var arg = args[i];
                    if (v.Value is FunctionMeta meta)
                        return false;
                    return true;
                })
                .Select(v =>
                {
                    var arg = args[i++];
                    var type = arg.ImplicitCast(sc, info, v.Value);
                    return type;
                }),
            ];

        var (result_type, _, _) = CreateDefinition(sc, arg_types, type_only: true);

        return result_type;
    }

    public LLVMValue Call(Scope sc, LLVMValue[] args)
    {
        if (args.Length != arguments.Count)
            throw new($"{info} called function waiting for {arguments.Count} but {args.Length} is passed");

        var i = 0u;
        var arg_types = new IMetaType[args.Length];
        var result_args =
            ((bool include, LLVMValueRef llvm, IMetaType type)[])[
                ..
                arguments
                .Select(v =>
                {
                    var arg = args[i];

                    var type = arg.type.ImplicitCast(sc, arg.info, v.Value);
                    arg_types[i++] = type;

                    if (type is FunctionMeta meta)
                        return (false, default, type);

                    return (true, arg.type.ImplicitCast(sc, arg.info, arg.Get(sc), type), arg.type);
                }),
            ];

        var bin_type = new FunctionMeta([.. result_args.Select(v => v.type) ], VoidMeta.Get);
        var bin = bin_type.AsBin();

        var call_args = (LLVMValueRef[])[.. SelectIf(result_args, v => (v.include, v.llvm))];

        if (ResolvingStack.TryGetValue((UniqueNumber, bin), out var definition))
        {
            return new(definition.type.result, val: sc.bi.BuildCall2(definition.llvm_type, definition.llvm_value, call_args));
        }

        var (result_type, llvm_func_type, llvm_func) = CreateDefinition(sc, arg_types, type_only: false);

        return new(result_type.result, val: sc.bi.BuildCall2(llvm_func_type, llvm_func, call_args));
    }

    private FunctionDefinition CreateDefinition(Scope base_scope, IMetaType[] args, bool type_only)
    {
        var i = 0u;
        var real_args =
            new Dictionary<string, IMetaType>([
                .. SelectPairsNoFunctions(args, ref i, this.arguments, selector: v => v),
            ]);

        var resolve_type = new FunctionMeta([.. real_args.Select(v => v.Value)], VoidMeta.Get);
        var resolve_bin = resolve_type.AsBin();

        if (Definitions.TryGetValue(resolve_bin, out var defined) && defined.llvm_type.Handle != 0)
            return defined;

        ResolvingStack[(UniqueNumber, resolve_bin)] = default;

        i = 0u;
        var type_scope = new Scope(
            variables: new([.. FictiveVariablesFrom(new(SelectPairs(args, ref i, this.arguments)))]),
            ctx: base_scope.ctx,
            bi: default,
            mod: base_scope.mod,
            fn: default
        );

        i = 0u;
        var resolved_args = (IMetaType[])[.. SelectFunctionsResolved(args)];

        Definitions[resolve_bin] = new(resolve_type, default, default);

        var result_type = this.block.Type(type_scope);

        if (result_type is GenericMeta)
            throw new($"{info} ambiguity return type in function body, try to explicit some types");

        var func_type = new FunctionMeta(
            resolved_args,
            result_type,
            declaration: this
        );

        if (type_only)
        {
            ResolvingStack.Remove((UniqueNumber, resolve_bin));
            return new(func_type, default, default);
        }

        var llvm_func_type = func_type.LLVMBaseType(base_scope);

        var func = base_scope.CreateFunction(llvm_func_type);

        var result = new FunctionDefinition(func_type, llvm_func_type, func);

        ResolvingStack[(UniqueNumber, resolve_bin)] = result;

        LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");

        using var bi = base_scope.ctx.CreateBuilder();
        bi.PositionAtEnd(entry);

        i = 0u;
        var scope = new Scope(
            new(
                [
                    .. SelectVariablesFromTypes(args, ref i, func, bi, base_scope, this.arguments),
                ]
            ),
            ctx: base_scope.ctx,
            bi: bi,
            mod: base_scope.mod,
            fn: func
        );

        var ret = this.block.Compile(scope);
        try
        {
            bi.BuildRet(ret.Get(scope));
        }
        catch
        {
            bi.BuildRetVoid();
        }

        Definitions[resolve_bin] = result;

        ResolvingStack.Remove((UniqueNumber, resolve_bin));
        return result;
    }

    private static IEnumerable<TResult> SelectIf<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, (bool, TResult)> selector)
    {
        foreach (TSource element in source)
        {
            var (add, value) = selector(element);
            if (add) yield return value;
        }
    }

    private static IEnumerable<KeyValuePair<string, Variable>> SelectVariablesFromTypes(
        IMetaType[] args,
        ref uint i,
        LLVMValueRef llvm_function,
        LLVMBuilderRef llvm_function_builder,
        Scope base_scope,
        Dictionary<string, IMetaType> collection)
    {
        uint real_arg = 0u;
        uint j = i;
        var res =
            collection
            .Select(v =>
            {
                var type = args[j];
                if (type is FunctionMeta meta)
                {
                    j++;
                    return KeyValuePair.Create(
                        v.Key,
                        new Variable(
                            type,
                            default,
                            default,
                            VariableFlags.None,
                            function: meta.Declaration
                        )
                    );
                }
                var link = llvm_function_builder.BuildAlloca(type.Type(base_scope));
                llvm_function_builder.BuildStore(llvm_function.GetParam(real_arg++), link);
                j++;
                return KeyValuePair.Create(
                    v.Key,
                    new Variable(
                        type,
                        link,
                        default,
                        VariableFlags.Setable
                    )
                );
            });
        i = j;
        return res;
    }

    private static IEnumerable<KeyValuePair<string, Variable>> SelectVariablesFromValues(
        IMetaType[] args,
        ref uint i,
        LLVMValueRef llvm_function,
        Dictionary<string, LLVMValue> collection)
    {
        var j = i;
        var res =
            from v in collection
            let type = args[j]
            select KeyValuePair.Create(
                v.Key,
                new Variable(
                    type,
                    llvm_function.GetParam(j++),
                    default,
                    VariableFlags.Setable
                )
            );
        i = j;
        return res;
    }

    private static IEnumerable<IMetaType> SelectFunctionsResolved(
        IMetaType[] collection)
        =>
        collection
        .Where(v => v is not FunctionMeta);

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectPairsNoFunctions<T>(
        IMetaType[] args,
        ref uint i,
        Dictionary<string, T> collection,
        Func<T, IMetaType> selector)
    {
        var j = i;
        var res =
            collection
            .Where(v =>
            {
                var add = selector(v.Value) is not FunctionMeta;
                if (!add) j++;
                return add;
            })
            .Select(v => KeyValuePair.Create(
                v.Key,
                args[j++]
            ));
        i = j;
        return res;
    }

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectPairs<T>(
        IMetaType[] args,
        ref uint i,
        Dictionary<string, T> collection)
    {
        var j = i;
        var res =
            collection
            .Select(v => KeyValuePair.Create(
                v.Key,
                args[j++]
            ));
        i = j;
        return res;
    }

    private static IEnumerable<KeyValuePair<string, Variable>> FictiveVariablesFrom(
        Dictionary<string, IMetaType> collection)
        =>
        from v in collection
        select KeyValuePair.Create(
            v.Key,
            new Variable(
                v.Value,
                default,
                default,
                VariableFlags.TypeOnly
            )
        );
}
