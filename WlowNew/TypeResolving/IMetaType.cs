using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial interface IMetaType
{
    string Name { get; }
    TypeMutability Mutability(Scope ctx);
    Flg<TypeConvention> Convention(Scope ctx);

    IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to);
    IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to);
    IMetaType TemplateCast(Scope ctx, Info info, IMetaType to);

    IMetaType AccessIndex(Scope ctx, Info info, int index)
        => throw CompilationExceptionList.NoFieldSupport(info, Name, index);
    IMetaType AccessName(Scope ctx, Info info, string name)
        => throw CompilationExceptionList.NoFieldSupport(info, Name, name);
    IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
        => throw CompilationExceptionList.NoIndexAddressation(info, Name);

    bool Callable(Scope ctx) => false;
    FunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
        => throw CompilationExceptionList.Uncallable(info, Name);

    Nothing Binary(BinaryTypeBuilder bin);

    IMetaType? FixateFn() => null;
}
