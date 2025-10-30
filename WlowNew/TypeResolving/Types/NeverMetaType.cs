using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct NeverMetaType : IMetaType
{
    public static readonly NeverMetaType Get = default;

    public override string ToString() => "never";
    public bool IsKnown => true;
    public Opt<uint> ByteSize => 0;
    public TypeMutability Mutability => TypeMutability.Const;
    public Flg<TypeConvention> Convention => TypeConvention.Return;

    public Nothing Binary(BinaryTypeBuilder bin, Info info) => bin.Push(BinaryTypeRepr.Never);
}

