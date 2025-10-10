namespace Wlow.Node;

public readonly record struct FieldAccessor(Info info, IValue value, string name) : IValue
{
    public IMetaType Type(Scope sc)
    {
        var type = value.Type(sc);
        if (!type.HasFields)
            throw new CompileException(info, $"field access is not available at type {type.Name(sc)}, it must be used only for structure types, or types derived from structure");

        var res = type.FieldGet(sc, info, default, name, as_pointer: false, type_only: true);
        if (!res.HasValue)
            throw new CompileException(info, $"type {type.Name(sc)} has not field {name}");

        return res.Value.type;
    }

    public LLVMValue Compile(Scope sc)
    {
        var type = value.Type(sc);
        if (!type.HasFields)
            throw new CompileException(info, $"field access is not available at type {type.Name(sc)}, it must be used only for structure types, or types derived from structure");

        var val = value.Compile(sc);

        var res = type.FieldGet(sc, info, val.link ?? val.Get(value.info, sc), name, as_pointer: val.link.HasValue);
        if (!res.HasValue)
            throw new CompileException(info, $"type {type.Name(sc)} has not field {name}");

        return res.Value;
    }

    public override string ToString() => $"fld({value}).{name}";
}
