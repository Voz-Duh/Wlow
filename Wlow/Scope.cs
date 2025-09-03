using System.Numerics;
using LLVMSharp.Interop;

namespace Wlow;

public readonly record struct GhostArgument(BigInteger Identifier, IMetaType Type);

public readonly record struct GhostVariable(LLVMValueRef Link, IMetaType Type, bool Used = false)
{
    public GhostVariable AsUsed => new(Link, Type, Used: true);
}

public readonly record struct Scope(
    Dictionary<string, Variable> variables,
    Dictionary<string, GhostVariable> ghosts,
    LLVMContextRef ctx,
    LLVMBuilderRef bi,
    LLVMModuleRef mod,
    LLVMValueRef fn,
    BigInteger Identifier
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

    private Scope Copy(
        VariableFlags exclude = VariableFlags.None,
        VariableFlags include = VariableFlags.None)
    {
        Dictionary<string, Variable> new_variables = [];
        foreach (var (key, variable) in variables)
            new_variables[key] = variable.Exclude(exclude).Include(include);
        return new(new_variables, ghosts, ctx, bi, mod, fn, Identifier);
    }

    public Scope CloneArgumented()
        => Copy(exclude: VariableFlags.Jumpable);

    public Scope CloneBraced()
        => Copy();
}
