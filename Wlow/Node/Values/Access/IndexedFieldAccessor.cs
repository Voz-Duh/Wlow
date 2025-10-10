namespace Wlow.Node;

public readonly record struct IndexedFieldAccessor(Info info, IValue value, int index) : IValue
{
    public IMetaType Type(Scope sc)
    {
        var type = value.Type(sc);
        if (!type.HasIndexedFields)
            throw new CompileException(info, $"index access is not available at type {type.Name(sc)}, it must be used only for tuple types, or types derived from tuple");

        var res = type.IndexedFieldGet(sc, info, default, index, as_pointer: false, type_only: true);
        if (!res.HasValue)
            throw new CompileException(info, $"index {index} is out of range of type {type.Name(sc)} with {type.IndexedFieldsCount} elements count");

        return res.Value.type;
    }

    public LLVMValue Compile(Scope sc)
    {
        var type = value.Type(sc);
        if (!type.HasIndexedFields)
            throw new CompileException(info, $"index access is not available at type {type.Name(sc)}, it must be used only for tuple types, or types derived from tuple");

        var val = value.Compile(sc);

        var res = type.IndexedFieldGet(sc, info, val.link ?? val.Get(value.info, sc), index, as_pointer: val.link.HasValue);
        if (!res.HasValue)
            throw new CompileException(info, $"index {index} is out of range of type {type.Name(sc)} with {type.IndexedFieldsCount} elements count");

        return res.Value;
    }

    public override string ToString() => $"idx({value}).{index}";
}
