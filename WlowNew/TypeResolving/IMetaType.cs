using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial interface IMetaType
{
    ID TypeID { get; }
    string Name { get; }
    Mutability Mutability { get; }

    IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to);
    IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to);
    IMetaType TemplateCast(Scope ctx, Info info, IMetaType to);

    void Binary(BinaryTypeBuilder bin);
}
