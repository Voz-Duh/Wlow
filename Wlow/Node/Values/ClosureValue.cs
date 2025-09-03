using Wlow.Types;

namespace Wlow.Node;

public readonly record struct ClosureValue(Info info, FunctionValue value) : IValue
{
    public IMetaType Type(Scope sc) => ((FunctionMeta)value.Type(sc)).Declaration.Closure();

    public LLVMValue Compile(Scope sc)
    {
        var type = (FunctionMeta)Type(sc);
        return new(type, function: type.Declaration);
    }

    public override string ToString() => $"function({string.Join(", ", [.. arguments.Select(v => $"{v.Value} {v.Key}")])} |> body({block}))";
}