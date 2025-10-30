using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct VoidMetaType : IMetaType
{
    public static readonly VoidMetaType Get = default;

    public override string ToString() => "void";
    public bool IsKnown => true;
    public Opt<uint> ByteSize => 0;
    public TypeMutability Mutability => TypeMutability.Copy;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin, Info info) => bin.Push(BinaryTypeRepr.Never);
}

