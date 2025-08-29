using Wlow.Types;

namespace Wlow.Node;

public readonly record struct FunctionValue(Info info, Dictionary<string, IMetaType> arguments, IValue block) : IValue
{
    private readonly FunctionMeta RawType = new([.. arguments.Select(v => v.Value)], GenericMeta.Get);

    public IMetaType Type(Scope sc) => RawType;

    public LLVMValue Compile(Scope sc)
    {
        var decl = new FunctionDecl(info, arguments, block);
        return new(RawType.IncludeDeclaration(decl), function: decl);
    }

    public override string ToString() => $"function({string.Join(", ", [.. arguments.Select(v => $"{v.Value} {v.Key}")])} |> body({block}))";
}