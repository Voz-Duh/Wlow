using Wlow.Shared;

namespace Wlow.TypeResolving;

public readonly partial struct BoolMetaType : IMetaType
{
    public static readonly BoolMetaType Get = default;


    public override string ToString() => "bool";
    public bool IsKnown => true;
    public Opt<uint> ByteSize => 1;
    public TypeMutability Mutability => TypeMutability.Copy;
    public Flg<TypeConvention> Convention => TypeConvention.Any;

    public Nothing Binary(BinaryTypeBuilder bin, Info info) => bin.Push(BinaryTypeRepr.Bool);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => IMetaType.SmartTypeSelect(
            ctx, info,
            this, to,
            (from, to) =>
                to is BoolMetaType
                ? to
                : null
        );

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
        => IMetaType.SmartTypeSelect(ctx, info, this, to, (_, _) => null, is_template: true, repeat: repeat);

    public IMetaType OperationEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<BoolMetaType>((a, b) => a)
            .Done(IMetaType.OpName.Equals);

    public IMetaType OperationNotEquals(Scope ctx, Info info, IMetaType right)
        => IMetaType.Operate(ctx, info, this, right)
            .Start()
            .On<BoolMetaType>((a, b) => a)
            .Done(IMetaType.OpName.NotEquals);

    public IMetaType OperationNot(Scope ctx, Info info) => this;
}

