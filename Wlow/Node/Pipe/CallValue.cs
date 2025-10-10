using Wlow.Types;

namespace Wlow.Node;

public readonly record struct CallValue(Info info, IValue value, IValue[] args) : IValue
{
    private readonly GenericLinkMeta linkMeta = new();

    public IMetaType Type(Scope sc)
    {
        var meta = value.Type(sc);
        if (meta.IsNot<FunctionMeta>(out var func))
            throw new CompileException(value.info, $"trying to call {meta.Name(sc)}");

        var res = func.Declaration.CallFunctionType(sc, info, [.. args.Select(v => (v.info, v.Type(sc)))]).result;
        if (res.Is<GenericMeta>())
        {
            linkMeta.CurrentType = GenericMeta.Get;
            return linkMeta;
        }
        linkMeta.CurrentType = res;
        return res;
    }

    public LLVMValue Compile(Scope sc)
    {
        var func = value.Compile(sc);
        if (func.function == null)
            throw new CompileException(value.info, $"trying to call {func.type.Name(sc)}");

        var res = func.function.Call(sc, info, [.. args.Select(v => (v.info, v.Compile(sc)))]);
        if (linkMeta.CurrentType.Is<GenericMeta>()
            || linkMeta.CurrentType.Is<VoidMeta>())
            return res;
        return new(linkMeta.CurrentType, res.type.ImplicitCast(sc, info, res.Get(info, sc), linkMeta.CurrentType, false));
    }

    public override string ToString() => $"call({value})' args{args.FmtString()}";
}
