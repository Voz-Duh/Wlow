
using System.Numerics;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow;

public readonly record struct FunctionDefinition(FunctionMeta type, LLVMTypeRef llvm_type, LLVMValueRef llvm_value);

public class FunctionDecl(
    Info info,
    Pair<string, IMetaType>[] arguments,
    IValue block,
    Dictionary<string, FunctionDefinition> definitions = null,
    BigInteger? unique_number = null)
{
    [ThreadStatic]
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStackValue;
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStack => ResolvingStackValue ??= [];
    
    [ThreadStatic]
    private static Stack<Info> CallingStackValue;
    private static Stack<Info> CallingStack => CallingStackValue ??= [];

    private static BigInteger UniqueNumberGenerator = 0;
    public readonly BigInteger UniqueNumber = unique_number ?? UniqueNumberGenerator++;

    public readonly Info info = info;
    public readonly Pair<string, IMetaType>[] arguments = arguments;
    public readonly IValue block = block;

    private readonly Dictionary<string, FunctionDefinition> Definitions = definitions ?? [];

    private T TryDo<T>(Info info, string bin, Func<T> func, FunctionDefinition definition=default, bool remove_resolver=false)
    {
        ResolvingStack[(UniqueNumber, bin)] = definition;
        CallingStack.Push(info);
        try
        {
            var res = func();
            CallingStack.Pop();
            if (remove_resolver)
                ResolvingStack.Remove((UniqueNumber, bin));
            return res;
        }
        catch (CompileException e) 
        {
            if (e is StackedCompileException) throw;
            var error = new StackedCompileException(CallingStack, e.Info, e.BaseMessage, e);
            CallingStack.Pop();
            ResolvingStack.Remove((UniqueNumber, bin));
            throw error;
        }
    }

    public LLVMValue CastTo(Scope sc, Info info, FunctionMeta to)
    {
        if (to.arguments.Length != arguments.Length)
            throw new CompileException(info, $"function with {arguments.Length} waited arguments cannot be casted to function with {to.arguments.Length} waited arguments");

        var i = 0;
        var args =
            arguments
            .Select(v =>
            {
                var arg = to.arguments[i++];
                try
                {
                    if (v.value.Is<FunctionMeta>(out var meta))
                    {
                        return Pair.From(v.ident, CastTo(sc, info, meta).type);
                    }
                    return Pair.From(v.ident, arg.ImplicitCast(sc, info, v.value));
                }
                catch (CompileException e)
                {
                    throw new CompileException(e.Info, $"at argument {i + 1}: {e.BaseMessage}");
                }
            })
            .ToArray();

        var type = new FunctionMeta(
            [
                ..
                args.Select(v =>  v.value)
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

    public (bool include, LLVMValueRef llvm, IMetaType type)[] SpecifyArguments<TElement>(
        Scope sc,
        TElement[] collection,
        Func<uint, TElement, IMetaType, bool> valid_selector,
        Func<uint, TElement, IMetaType, bool, LLVMValueRef> value_selector,
        Func<uint, TElement, IMetaType, IMetaType> type_selector)
    {
        var i = 0u;
        return [
            ..
            arguments
            .Select(v =>
            {
                var arg = collection[i];
                var type = type_selector(i, arg, v.value);
                var valid = valid_selector(i, arg, type);

                return (valid, value_selector(i++, arg, type, valid), type);
            })
        ];
    }

    public FunctionMeta CallFunctionType(Scope sc, Info info, (Info info, IMetaType type)[] args)
    {
        if (args.Length != arguments.Length)
            throw new CompileException(info, $"called function waiting for {arguments.Length} arguments but {args.Length} is passed");

        var arg_types =
            SpecifyArguments(
                sc,
                args,
                valid_selector: (i, v, t) => true,
                value_selector: (i, v, t, a) => default,
                type_selector: (i, v, to) => v.type.ImplicitCast(sc, v.info, to)
            )
            .Select(v => v.type)
            .ToArray();

        var bin_type = new FunctionMeta(arg_types, VoidMeta.Get);
        var bin = bin_type.AsBin();

        if (ResolvingStack.TryGetValue((UniqueNumber, bin), out var resolve))
        {
            if (resolve.type.arguments is null)
                return new FunctionMeta(arg_types, GenericMeta.Get);
            else
                return resolve.type;
        }

        var (result_type, _, _) = CreateDefinition(sc, info, arg_types, type_only: true, bin_type, bin);

        return result_type;
    }

    public LLVMValue Call(Scope sc, Info info, (Info info, LLVMValue value)[] args)
    {
        if (args.Length != arguments.Length)
            throw new CompileException(info, $"called function waiting for {arguments.Length} but {args.Length} is passed");

        var arg_types = new IMetaType[args.Length];

        var result_args =
            SpecifyArguments(
                sc,
                args,
                valid_selector: (i, val, type) => type.IsNot<FunctionMeta>(),
                value_selector: (i, val, type, valid) => valid ? val.value.type.ImplicitCast(sc, val.info, val.value.Get(val.info, sc), type) : default,
                type_selector: (i, val, to) => arg_types[i] = val.value.type.ImplicitCast(sc, val.info, to)
            );

        var bin_type = new FunctionMeta([.. result_args.Select(v => v.type) ], VoidMeta.Get);
        var bin = bin_type.AsBin();

        var call_args = (LLVMValueRef[])[.. SelectIf(result_args, v => (v.include, v.llvm))];

        if (ResolvingStack.TryGetValue((UniqueNumber, bin), out var definition))
        {
            return new(definition.type.result, val: sc.bi.BuildCall2(definition.llvm_type, definition.llvm_value, call_args));
        }

        var (result_type, llvm_func_type, llvm_func) = CreateDefinition(sc, info, arg_types, type_only: false, bin_type, bin);

        return new(result_type.result, val: sc.bi.BuildCall2(llvm_func_type, llvm_func, call_args));
    }

    private FunctionDefinition CreateDefinition(Scope base_scope, Info info, IMetaType[] args, bool type_only, FunctionMeta resolve_type, string resolve_bin)
    {
        var i = 0u;
        var real_args =
            new Dictionary<string, IMetaType>([
                .. SelectPairsNoFunctions(args, ref i, this.arguments, selector: v => v),
            ]);

        if (Definitions.TryGetValue(resolve_bin, out var defined) && defined.llvm_type.Handle != 0)
            return defined;

        i = 0u;
        var type_scope = new Scope(
            variables: new([.. FictiveVariablesFrom(new(SelectKeyValuePairs(args, ref i, this.arguments)))]),
            base_scope.global_variables,
            ctx: base_scope.ctx,
            bi: default,
            mod: base_scope.mod,
            fn: default
        );

        i = 0u;
        var resolved_args = (IMetaType[])[.. SelectFunctionsResolved(args)];

        Definitions[resolve_bin] = new(resolve_type, default, default);

        var result_type = TryDo(info, resolve_bin, () => this.block.Type(type_scope)).Unwrap();
        if (result_type.Is<GenericMeta>())
            result_type = VoidMeta.Get;

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

        LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");

        using var bi = base_scope.ctx.CreateBuilder();
        bi.PositionAtEnd(entry);

        i = 0u;
        var scope = new Scope(
            new(
                [
                    .. SelectVariablesFrom(args, ref i, func, bi, base_scope, this.arguments),
                ]
            ),
            base_scope.global_variables,
            ctx: base_scope.ctx,
            bi: bi,
            mod: base_scope.mod,
            fn: func
        );

        Definitions[resolve_bin] = result;

        var ret = TryDo(info, resolve_bin, () => this.block.Compile(scope), definition: result, remove_resolver: true);
        try
        {
            bi.BuildRet(ret.Get(info, scope));
        }
        catch
        {
            bi.BuildRetVoid();
        }

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

    private static IEnumerable<KeyValuePair<string, Variable>> SelectVariablesFrom(
        IMetaType[] args,
        ref uint i,
        LLVMValueRef llvm_function,
        LLVMBuilderRef llvm_function_builder,
        Scope base_scope,
        Pair<string, IMetaType>[] collection)
    {
        uint real_arg = 0u;
        uint j = i;
        var res =
            collection
            .Select(v =>
            {
                var type = args[j];
                if (type.Is<FunctionMeta>(out var meta))
                {
                    j++;
                    return KeyValuePair.Create(
                        v.ident,
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
                    v.ident,
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
        .Where(v => v.IsNot<FunctionMeta>());

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectPairsNoFunctions<T>(
        IMetaType[] args,
        ref uint i,
        Pair<string, T>[] collection,
        Func<T, IMetaType> selector)
    {
        var j = i;
        var res =
            collection
            .Where(v =>
            {
                var skip = selector(v.value).Is<FunctionMeta>();
                if (skip) j++;
                return !skip;
            })
            .Select(v => KeyValuePair.Create(
                v.ident,
                args[j++]
            ));
        i = j;
        return res;
    }

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectKeyValuePairs<T>(
        IMetaType[] args,
        ref uint i,
        Pair<string, T>[] collection)
    {
        var j = i;
        var res =
            collection
            .Select(v => KeyValuePair.Create(
                v.ident,
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
