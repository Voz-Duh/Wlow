using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct PlaceHolderMetaType : IMetaType
{
    public readonly static PlaceHolderMetaType Get = new();

    public override string ToString() => "?";
    public bool IsKnown => false;
    public Opt<uint> ByteSize => Opt<uint>.Hasnt();
    public TypeMutability Mutability => TypeMutability.PlaceHolder;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin, Info info)
        => bin.Push((byte)BinaryTypeRepr.PlaceHolder);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => to;

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => to;

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => to;

    public IMetaType OperationRef(Scope _, Info __) => this;
    public IMetaType OperationDeref(Scope _, Info __) => this;
    public IMetaType OperationNegate(Scope _, Info __) => this;
    public IMetaType OperationPlus(Scope _, Info __) => this;
    public IMetaType OperationNot(Scope _, Info __) => this;
    public IMetaType OperationInv(Scope _, Info __) => this;
}
