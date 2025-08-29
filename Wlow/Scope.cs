using System.Numerics;
using LLVMSharp.Interop;

namespace Wlow;

public readonly record struct Scope(
    Dictionary<string, Variable> variables,
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
            if (type_only)
            {
                return true;
            }
            return !variable.flags.HasFlag(VariableFlags.TypeOnly);
        }
        return false;
    }

    public Scope CloneArgumented()
    {
        Dictionary<string, Variable> new_variables = [];
        foreach (var (key, variable) in variables)
            new_variables[key] = variable.Exclude(VariableFlags.Jumpable);
        return new(new_variables, ctx, bi, mod, fn);
    }

    public Scope CloneBraced()
    {
        Dictionary<string, Variable> new_variables = [];
        foreach (var (key, variable) in variables)
            new_variables[key] = variable;
        return new(new_variables, ctx, bi, mod, fn);
    }
}
