using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct Jump(Info info, string name) : IValue
{
    public IMetaType Type(Scope sc) => VoidMeta.Get;

    public LLVMValue Compile(Scope sc)
    {
        bool has = sc.GetVariable(name, out Variable variable);
        if (!has)
        {
            var block = sc.Block();
            variable = new(
                VoidMeta.Get,
                default,
                block,
                VariableFlags.Label
            );
            sc.variables[name] = variable;

            sc.bi.BuildBr(block);
            sc.bi.PositionAtEnd(block);
        }
        else
        {
            sc.bi.BuildBr(variable.block);
        }
        unsafe {
            return new(VoidMeta.Get, is_jump: true);
        }
    }

    public override string ToString() => $"jump({name})";
}
