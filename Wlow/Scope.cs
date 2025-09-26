using System.Numerics;
using LLVMSharp.Interop;

namespace Wlow;

public readonly record struct Scope(
    Dictionary<string, Variable> variables,
    Dictionary<string, LLVMValue> global_variables,
    LLVMContextRef ctx,
    LLVMBuilderRef bi,
    LLVMModuleRef mod,
    LLVMValueRef fn
)
{
    static BigInteger unique = BigInteger.Zero;

    public LLVMBasicBlockRef Block()
        => ctx.AppendBasicBlock(fn, unique++.ToString());

    public LLVMValueRef CreateFunction(LLVMTypeRef type)
        => mod.AddFunction(unique++.ToString(), type);
    
    public bool GetVariable(string name, out Variable variable, bool type_only = false)
    {
        if (variables.TryGetValue(name, out variable))
        {
            if (type_only) return true;
            return !variable.flags.HasFlag(VariableFlags.TypeOnly);
        }
        if (global_variables.TryGetValue(name, out var val))
        {
            variable = new(val.type, val.link.Value, default, VariableFlags.None, val.function);
            return true;
        }
        return false;
    }

    public Scope CloneNoVariable() => new([], global_variables, ctx, bi, mod, fn);

    public Scope CloneArgumented()
    {
        Dictionary<string, Variable> new_variables = [];
        foreach (var (key, variable) in variables)
            new_variables[key] = variable.Exclude(VariableFlags.Jumpable);
        return new(new_variables, global_variables, ctx, bi, mod, fn);
    }

    public Scope CloneBraced()
    {
        Dictionary<string, Variable> new_variables = [];
        foreach (var (key, variable) in variables)
            new_variables[key] = variable;
        return new(new_variables, global_variables, ctx, bi, mod, fn);
    }
}
