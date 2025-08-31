using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct Set(Info info, IValue value, string name) : IValue
{
    public IMetaType Type(Scope sc)
    {
        bool has = sc.GetVariable(name, out Variable variable, true);
        if (!has)
        {
            var type = value.Type(sc);
            variable = new(
                type,
                default,
                default,
                VariableFlags.TypeOnly
            );
            sc.variables[name] = variable;
        }
        return variable.type;
    }

    public LLVMValue Compile(Scope sc)
    {
        bool has = sc.GetVariable(name, out Variable variable);
        if (has && !variable.flags.HasFlag(VariableFlags.Setable))
        {
            if (variable.function != null)
                throw new CompileException(info, $"{name} is cannot be changed, function variables is immutable");
            throw new CompileException(info, $"{name} is cannot be changed from here");
        }
        bool is_func = false;
        if (!has)
        {
            var type = value.Type(sc);
            var block = sc.Block();
            is_func = type.Is<FunctionMeta>();
            variable = new(
                type,
                is_func ? default : sc.bi.BuildAlloca(type.Type(sc)),
                block,
                Variable.std_mode
            );
            sc.variables[name] = variable;
        }
        var res = value.Compile(sc.CloneArgumented());

        LLVMValueRef llvm = default;

        if (is_func)
        {
            variable = new Variable(
                res.type,
                default,
                variable.block,
                Variable.std_mode,
                function: res.function
            ).Exclude(VariableFlags.Setable);
            sc.variables[name] = variable;
            goto Final;
        }
        
        llvm = res.type.ImplicitCast(sc, res.info, res.Get(sc), variable.type);
        sc.bi.BuildStore(llvm, variable.llvm);

    Final:
        if (!has)
        {
            sc.bi.BuildBr(variable.block);
            sc.bi.PositionAtEnd(variable.block);
        }
        return new(res.type, val: llvm, function: variable.function);
    }

    public override string ToString() => $"set({value})={name}";
}
