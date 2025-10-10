using Wlow.Types;

namespace Wlow.Node;

public readonly record struct FunctionValue(Info info, Pair<string, IMetaType>[] arguments, IValue block) : IValue
{
    private readonly FunctionMeta RawType = new(
        [.. arguments.Select(v => v.value)],
        GenericMeta.Get,
        new FunctionDecl(info, arguments, block));

    public IMetaType Type(Scope sc) => RawType;

    public LLVMValue Compile(Scope sc) => new(RawType, function: RawType.Declaration);

    public override string ToString() => $"function({string.Join(", ", [.. arguments.Select(v => $"{v.value} {v.ident}")])} |> body({block}))";
}