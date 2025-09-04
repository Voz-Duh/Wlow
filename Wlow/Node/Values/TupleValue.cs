using LLVMSharp.Interop;
using Wlow.Types;

namespace Wlow.Node;

public readonly record struct TupleValue(Info info, IValue[] elements, bool packed = false) : IValue
{
    public IMetaType Type(Scope sc) => new TupleMeta([.. elements.Select(v => v.Type(sc))], packed);

    public LLVMValue Compile(Scope sc)
    {
        var compiled = ((Info info, LLVMValue value)[])[.. elements.Select(v => (v.info, v.Compile(sc)))];

        var values =
            compiled.Select((v, i) =>
            {
                if (v.value.type.IsGeneric())
                    throw new CompileException(v.info, $"value at element {i} in tuple has unstoreable type {v.value.type.Name(sc)}, try to specify type to avoid generics");
                return v.value.Get(v.info, sc);
            })
            .ToArray();

        return new(Type(sc), val: LLVMValueRef.CreateConstStruct(values, packed));
    }

    public override string ToString() => $"{(packed ? "packed" : "")} tuple({string.Join(", ", [.. elements.Select(v => $"{v}")])})";
}