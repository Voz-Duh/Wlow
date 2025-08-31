namespace Wlow.Node;

public readonly record struct IdentValue(Info info, string name) : IValue
{
    public IMetaType Type(Scope sc) =>
        sc.GetVariable(name, out Variable variable, true)
            ? variable.type
            : throw new CompileException(info, $"{name} does not exists");

    public LLVMValue Compile(Scope sc) =>
        sc.GetVariable(name, out Variable variable)
            ? (variable.function is not null
                ? new(variable.type, function: variable.function)
                : new(variable.type, link: variable.llvm)
            ) : throw new CompileException(info, $"{name} does not exists");

    public override string ToString() => $"ident({name})";
}
