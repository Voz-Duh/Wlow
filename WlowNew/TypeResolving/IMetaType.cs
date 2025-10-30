using System.Collections.Immutable;
using Wlow.Gen;
using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial interface IMetaType
{
    /// <summary>
    /// String representation of type
    /// </summary>
    string ToString();

    /// <summary>
    /// if size value doesn't exits it must be used to specify error (sizeof) or to skip type as an argument (calling)
    /// </summary>
    bool IsKnown { get; }

    /// <summary>
    /// if size value doesn't exits it must be used to specify error (sizeof) or to skip type as an argument (calling)
    /// </summary>
    Opt<uint> ByteSize { get; }

    /// <summary>
    /// Mutability of type
    /// TODO use convention instead
    /// </summary>
    TypeMutability Mutability { get; }
    
    /// <summary>
    /// Convention (possibilities) of type
    /// </summary>
    Flg<TypeConvention> Convention { get; }

    /// <summary>
    /// Implicit cast, use in something like:
    /// <code>
    /// let x i64 = 5i32;
    /// </code>
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="to">Cast to that type</param>
    /// <returns>Result type, sometimes it will not be <paramref name="to"/>, for example, cast like: (i32, i32) -> (i32, ?) will return (i32, i32)</returns>
    /// <exception cref="CompilationException">Invalid cast option</exception>
    IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to);

    /// <summary>
    /// Explicit cast, use in something like:
    /// <code>
    /// let x = 5i32 -> i64;
    /// </code>
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="to">Cast to that type</param>
    /// <returns>Result type, sometimes it will not be <paramref name="to"/>, for example, cast like: (i32, i32) -> (i32, ?) will return (i32, i32)</returns>
    /// <exception cref="CompilationException">Invalid cast option</exception>
    IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to);

    /// <summary>
    /// Template cast, use in something like:
    /// <code>
    /// type A = ?;
    /// let x = A 5;
    /// </code>
    /// Basically, can be replaced with implicit cast, but ignore that option
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="to">Cast to that type</param>
    /// <returns>Result type, sometimes it will not be <paramref name="to"/>, for example, cast like: (i32, i32) -> (i32, ?) will return (i32, i32)</returns>
    /// <exception cref="CompilationException">Invalid cast option</exception>
    IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat = false);

    /// <summary>
    /// Static access like:
    /// <code>value.0</code>
    /// Tuple-like access
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="index">Index of field</param>
    /// <returns>Type of field</returns>
    /// <exception cref="CompilationException">Tuple-like access is not possible for that type; Index is out of bounds</exception>
    IMetaType AccessIndex(Scope ctx, Info info, int index)
        => throw CompilationExceptionList.NoFieldSupport(info, ToString(), index);

    /// <summary>
    /// Static access like:
    /// <code>value.field</code>
    /// Struct-like access
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="name">Name of field</param>
    /// <returns>Type of field</returns>
    /// <exception cref="CompilationException">Struct-like access is not possible for that type; Name doesn't exists</exception>
    IMetaType AccessName(Scope ctx, Info info, string name)
        => throw CompilationExceptionList.NoFieldSupport(info, ToString(), name);

    /// <summary>
    /// Runtime access like:
    /// <code>value[0]</code>
    /// Array-like access
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="index">Type of value used to index</param>
    /// <returns>Type of addressation</returns>
    /// <exception cref="CompilationException">Array-like access is not possible for that type</exception>
    IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
        => throw CompilationExceptionList.NoIndexAddressation(info, ToString());

    /// <summary>
    /// Check if type is possible to be called
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <returns>Is it callable</returns>
    bool Callable(Scope ctx) => false;

    /// <summary>
    /// Call type with arguments
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="info">Info for errors</param>
    /// <param name="args">Arguments to call with info for errors</param>
    /// <returns>Function definition of calling, will be used later to generate code</returns>
    IFunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
        => throw CompilationExceptionList.Uncallable(info, ToString());

    /// <summary>
    /// Used to get binary representation of type
    /// </summary>
    /// <param name="bin">Binary builder</param>
    /// <param name="ctx">Context of building</param>
    Nothing Binary(BinaryTypeBuilder bin, Info info);

    /// <summary>
    /// Used to stack-optimized unwrap of types
    /// </summary>
    /// <returns>Unwrapped type; <c>null</c> by default used if unwrapping is not needed for type</returns>
    IMetaType? UnwrapFn() => null;

    /// <summary>
    /// Used to stack-optimized unweak of types
    /// </summary>
    /// <returns>Unweak type; <c>null</c> by default used if unweaking is not needed for type</returns>
    IMetaType? UnweakFn() => null;

    /// <summary>
    /// Must be used if type is possible to be user notated
    /// </summary>
    /// <param name="ctx">Context</param>
    IMetaType Expand(Scope ctx) => this;

    /// <summary>
    /// Used to create compilable type reference, aka Fixed Type
    /// </summary>
    /// <returns>Compilable type reference</returns>
    // TODO IFixType Fixed();
}
