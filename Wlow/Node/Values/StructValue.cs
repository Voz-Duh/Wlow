using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct StructValue(Info info, Pair<string, IValue>[] fields, bool packed = false) : IValue
{
    public IMetaType Type(Scope sc) => new StructMeta([.. fields.Select(v => Pair.From(v.ident, v.value.Type(sc)))], packed);

    public LLVMValue Compile(Scope sc)
    {
        var compiled = ((Info info, string name, LLVMValue value)[])[.. fields.Select(v => (v.value.info, v.ident, v.value.Compile(sc)))];

        var values =
            compiled.Select(v =>
            {
                if (v.value.type.IsGeneric())
                    throw new CompileException(v.info, $"field {v.name} in structure has unstoreable type {v.value.type.Name(sc)}, try to specify type to avoid generics");
                return v.value.Get(v.info, sc);
            })
            .ToArray();

        return new(Type(sc), val: LLVMValueRef.CreateConstStruct(values, packed));
    }

    public override string ToString() => $"{(packed ? "packed" : "")} struct({ string.Join(", ", [.. fields.Select(v => $"field({v.value})={v.ident}")])})";
}