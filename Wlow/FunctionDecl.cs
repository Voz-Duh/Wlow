
using System.Numerics;
using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow;

public readonly record struct FunctionDefinition(FunctionMeta type, LLVMTypeRef llvm_type, LLVMValueRef llvm_value);

public class FunctionDecl(
    Info info,
    Dictionary<string, IMetaType> arguments,
    Dictionary<string, GhostArgument> ghosts,
    IValue block,
    Dictionary<string, FunctionDefinition> definitions = null,
    BigInteger? unique_number = null,
    int ghosts_start = -1)
{
    [ThreadStatic]
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStackValue;
    private static Dictionary<(BigInteger, string), FunctionDefinition> ResolvingStack => ResolvingStackValue ??= [];

    [ThreadStatic]
    private static Stack<Info> CallingStackValue;
    private static Stack<Info> CallingStack => CallingStackValue ??= [];

    private static BigInteger UniqueNumberGenerator = 1;
    public readonly BigInteger UniqueNumber = unique_number ?? UniqueNumberGenerator++;

    public readonly Info info = info;
    public readonly Dictionary<string, IMetaType> arguments = arguments;
    public readonly Dictionary<string, GhostArgument> ghosts = ghosts;
    public readonly int ghosts_start = ghosts_start == -1 ? arguments.Count : ghosts_start;
    public readonly IValue block = block;

    private readonly Dictionary<string, FunctionDefinition> Definitions = definitions ?? [];

    private T TryDo<T>(Info info, string bin, Func<T> func, FunctionDefinition definition = default, bool remove_resolver = false)
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
        if (to.arguments.Length != arguments.Count)
            throw new CompileException(info, $"function with {arguments.Count} waited arguments cannot be casted to function with {to.arguments.Length} waited arguments");

        var i = 0;
        var args = new Dictionary<string, IMetaType>(
            arguments
            .Select(v =>
            {
                var arg = to.arguments[i++];
                try
                {
                    if (v.Value.Is<FunctionMeta>(out var meta))
                    {
                        return KeyValuePair.Create(v.Key, CastTo(sc, info, meta).type);
                    }
                    return KeyValuePair.Create(v.Key, arg.ImplicitCast(sc, info, v.Value));
                }
                catch (CompileException e)
                {
                    throw new CompileException(e.Info, $"at argument {i + 1}: {e.BaseMessage}");
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

        return new(type, function: new(info, args, ghosts, block, Definitions));
    }

    public (bool include, LLVMValueRef llvm, IMetaType type)[] SpecifyArguments<TElement>(
        Scope sc,
        TElement[] collection,
        Func<TElement, IMetaType, bool> valid_selector,
        Func<TElement, IMetaType, bool, LLVMValueRef> value_selector,
        Func<TElement, IMetaType, IMetaType> type_selector)
    {
        var i = 0u;
        return [
                ..
                arguments
                .Select(v =>
                {
                    var arg = collection[i];
                    var type = type_selector(arg, v.Value);
                    var valid = valid_selector(arg, type);

                    return (valid, value_selector(arg, type, valid), type);
                }),
                ..
                ghosts
                .Select(v => {
                    if (sc.Identifier == v.Value.Identifier)
                    {
                        if (sc.GetVariable(v.Key, out var variable))
                            return (true, variable.llvm, variable.type);
                        throw new CompileException(info, $"{Errors.NotNormal}");
                    }
                    var ghost = sc.ghosts[v.Key];
                    return (true, ghost.Link, ghost.Type);
                })
            ];
    }

    public FunctionMeta CallFunctionType(Scope sc, Info info, (Info info, IMetaType type)[] args)
    {
        if (args.Length != arguments.Count)
            throw new CompileException(info, $"called function waiting for {arguments.Count} arguments but {args.Length} is passed");

        var arg_types =
            SpecifyArguments(
                sc,
                args,
                valid_selector: (v, t) => true,
                value_selector: (v, t, a) => default,
                type_selector: (v, to) => v.type.ImplicitCast(sc, v.info, to)
            )
            .Select(v => v.type)
            .ToArray();

        var bin_type = new FunctionMeta(arg_types, VoidMeta.Get);
        var bin = bin_type.AsBin();

        if (ResolvingStack.ContainsKey((UniqueNumber, bin)))
            return new FunctionMeta(arg_types, GenericMeta.Get);

        var (result_type, _, _) = CreateDefinition(sc, info, arg_types, type_only: true);

        return result_type;
    }

    public FunctionMeta Closure(Scope sc, Info info, FunctionMeta type)
    {
        var closure_scope = new Scope(
            variables: new(this.arguments.Select(v => KeyValuePair.Create(v.Key, new Variable(v.Value, default, default, default, default)))),
            ghosts: [],
            ctx: sc.ctx,
            bi: default,
            mod: sc.mod,
            fn: default,
            Identifier: UniqueNumber
        );

        var registered_ghosts = new Dictionary<string, ClosureType>();// (sc.ghosts.Select(v => KeyValuePair.Create(v.Key, ClosureType.Invisible)));
        block.Closure(closure_scope, registered_ghosts);

        return new(
            type.arguments,
            type.result,
            new(
                info,
                arguments.ToDictionary(),
                [
                    ..
                    registered_ghosts
                    .Where(v => v.Value != ClosureType.None)
                    .Select(sc.)
                ]
            )
        );
    }

    public LLVMValue Call(Scope sc, Info info, LLVMValue[] args)
    {
        if (args.Length != arguments.Count)
            throw new CompileException(info, $"called function waiting for {arguments.Count} but {args.Length} is passed");

        var arg_types = new IMetaType[args.Length + ghosts.Count];
        var result_args =
            SpecifyArguments(
                sc,
                args,
                valid_selector: (v, t) => !t.Is<FunctionMeta>(out var meta),
                value_selector: (v, t, a) => !a ? default : v.type.ImplicitCast(sc, v.info, v.Get(sc), t),
                type_selector: (v, to) => v.type.ImplicitCast(sc, v.info, to)
            );

        var bin_type = new FunctionMeta([.. result_args.Select(v => v.type)], VoidMeta.Get);
        var bin = bin_type.AsBin();

        var call_args = (LLVMValueRef[])[.. SelectIf(result_args, v => (v.include, v.llvm))];

        if (ResolvingStack.TryGetValue((UniqueNumber, bin), out var definition))
        {
            return new(definition.type.result, val: sc.bi.BuildCall2(definition.llvm_type, definition.llvm_value, call_args));
        }

        var (result_type, llvm_func_type, llvm_func) = CreateDefinition(sc, info, arg_types, type_only: false);

        return new(result_type.result, val: sc.bi.BuildCall2(llvm_func_type, llvm_func, call_args));
    }

    private FunctionDefinition CreateDefinition(Scope base_scope, Info info, IMetaType[] args, bool type_only)
    {
        var i = 0u;
        var real_args =
            new Dictionary<string, IMetaType>([
                .. SelectPairsNoFunctions(args, ref i, this.arguments, selector: v => v),
                .. SelectPairsNoFunctions(args, ref i, this.ghosts,    selector: v => v.Type, ghosted: true),
            ]);

        var resolve_type = new FunctionMeta([.. real_args.Select(v => v.Value)], VoidMeta.Get);
        var resolve_bin = resolve_type.AsBin();

        if (Definitions.TryGetValue(resolve_bin, out var defined) && defined.llvm_type.Handle != 0)
            return defined;

        i = 0u;
        var type_scope = new Scope(
            variables: new([.. FictiveVariablesFrom(new(SelectPairs(args, ref i, this.arguments)))]),
            ghosts: [],
            ctx: base_scope.ctx,
            bi: default,
            mod: base_scope.mod,
            fn: default,
            Identifier: UniqueNumber
        );

        i = 0u;
        var resolved_args = (IMetaType[])[.. SelectFunctionsResolved(args)];

        Definitions[resolve_bin] = new(resolve_type, default, default);
        var registered_ghosts = (Dictionary<string, ClosureType>)[];
        TryDo(info, resolve_bin, () => { this.block.Closure(type_scope, registered_ghosts); return 0; });
        var ghosts = new Dictionary<string, GhostVariable>(
                from v in registered_ghosts
                where v.Value != ClosureType.None
                where !this.ghosts.ContainsKey(v.Key)
                select KeyValuePair.Create(v.Key, base_scope.ghosts[v.Key])
            );

        var result_type = TryDo(info, resolve_bin, () => this.block.Type(type_scope));
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

        var variables = new Dictionary<string, Variable>(
            [
                .. SelectVariablesFromTypes(
                    args,
                    ref i,
                    func,
                    bi,
                    base_scope,
                    this.arguments),
                .. SelectVariablesFromTypes(
                    args,
                    ref i,
                    func,
                    bi,
                    base_scope,
                    new(this.ghosts.Select(v => KeyValuePair.Create(v.Key, v.Value.Type))),
                    ghosted: true)
            ]
        );

        i = 0u;
        var scope = new Scope(
            variables,
            ghosts,
            ctx: base_scope.ctx,
            bi: bi,
            mod: base_scope.mod,
            fn: func,
            Identifier: UniqueNumber
        );

        var ret = TryDo(info, resolve_bin, () => this.block.Compile(scope), definition: result, remove_resolver: true);
        try
        {
            bi.BuildRet(ret.Get(scope));
        }
        catch
        {
            bi.BuildRetVoid();
        }

        Definitions[resolve_bin] = result;

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
        Dictionary<string, IMetaType> collection,
        bool ghosted = false)
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
                var link = llvm_function.GetParam(real_arg++);
                if (!ghosted)
                {
                    var val = link;
                    link = llvm_function_builder.BuildAlloca(type.Type(base_scope));
                    llvm_function_builder.BuildStore(val, link);
                }
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
        .Where(v => v.IsNot<FunctionMeta>());

    private static IEnumerable<KeyValuePair<string, IMetaType>> SelectPairsNoFunctions<T>(
        IMetaType[] args,
        ref uint i,
        Dictionary<string, T> collection,
        Func<T, IMetaType> selector,
        bool ghosted = false)
    {
        var j = i;
        var res =
            collection
            .Where(v =>
            {
                var add = selector(v.Value).IsNot<FunctionMeta>();
                if (!add) j++;
                return add;
            })
            .Select(v =>
                ghosted
                ? KeyValuePair.Create(
                    v.Key,
                    selector(v.Value)
                )
                : KeyValuePair.Create(
                    v.Key,
                    args[j++]
                )
            );
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
