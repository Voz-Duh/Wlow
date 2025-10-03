using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct PlaceHolderMetaType : IMetaType
{
    public readonly static PlaceHolderMetaType Get = new();

    public string Name => "?";
    public ID TypeID
        => throw new NotSupportedException("NNE: place holder using error");
    public Mutability Mutability
        => Mutability.PlaceHolder;

    public void Binary(BinaryTypeBuilder bin)
        => bin.Push((byte)BinaryTypeRepr.PlaceHolder);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => throw new NotSupportedException("NNE: place holder using error");

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => throw new NotSupportedException("NNE: place holder using error");

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => to;

    public IMetaType OperationRef(Scope _, Info __) => this;
    public IMetaType OperationDeref(Scope _, Info __) => this;
    public IMetaType OperationNegate(Scope _, Info __) => this;
    public IMetaType OperationPlus(Scope _, Info __) => this;
    public IMetaType OperationNot(Scope _, Info __) => this;
    public IMetaType OperationInv(Scope _, Info __) => this;
    public IMetaType OperationOn(Scope _, Info __) => this;
}
