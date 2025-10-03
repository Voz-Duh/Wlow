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

    IMetaType AccessIndex(Info info, int index) => throw CompilationException.Create(info, $"{Name} type does not support '{index}' field access");
    IMetaType AccessName(Info info, string name) => throw CompilationException.Create(info, $"{Name} type does not support '{name}' field access");

    void Binary(BinaryTypeBuilder bin);
}
