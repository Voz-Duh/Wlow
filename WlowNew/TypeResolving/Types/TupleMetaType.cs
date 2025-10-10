using System.Collections.Immutable;
using Wlow.Shared;

using TupleBase = Wlow.Shared.Rec3<
    // basic
    System.Collections.Immutable.ImmutableArray<Wlow.TypeResolving.IMetaType>,
    // homogeneous
    (int Count, Wlow.TypeResolving.IMetaType Type),
    // callable
    (Wlow.TypeResolving.IMetaType Function, System.Collections.Immutable.ImmutableArray<Wlow.TypeResolving.IMetaType> Args)
>;

namespace Wlow.TypeResolving;

public readonly partial record struct TupleMetaType : IMetaType
{
    public readonly TupleBase Base;

    TupleMetaType(TupleBase @base) => Base = @base;
    
    public static TupleMetaType Create(Scope ctx, ImmutableArray<IMetaType> types)
    {
        if (types.Length <= 1)
            throw new ArgumentException("Tuple must have at least two types", nameof(types));

        // check for callable
        if (types[0].Callable(ctx))
            return new((types[0], types[1..]));

        var builder = new BinaryTypeBuilder();
        var keybin = types[0].Binary(builder).Of(builder).Done();

        // check for homogeneous
        if (types.All(v =>
        {
            var builder = new BinaryTypeBuilder();
            var bin = types[0].Binary(builder).Of(builder).Done();
            return bin == keybin;
        })) return new((types.Length, types[0]));

        return new(types);
    }

    public static TupleMetaType CreateHomogeneous(int count, IMetaType type)
        => new((count, type));

    public string Name
        => Base.Unwrap(
            basic => $"({string.Join(", ", basic.Select(v => v.Name))})",
            homogeneous => $"({homogeneous.Count} {homogeneous.Type.Name})",
            callable => $"({string.Join(", ", [callable.Function.Name, .. callable.Args.Select(v => v.Name)])})"
        );
    public TypeMutability Mutability(Scope ctx) => TypeMutability.Copy;
    public Flg<TypeConvention> Convention(Scope ctx)
        => Base.Unwrap(
            basic => basic.Aggregate(Flg.From(TypeConvention.Any), (pre, cur) => pre & cur.Convention(ctx)),
            homogeneous => homogeneous.Type.Convention(ctx),
            callable => callable.Args.Aggregate(callable.Function.Convention(ctx), (pre, cur) => pre & cur.Convention(ctx))
        );

    public int Count
        => Base.Unwrap(
            basic => basic.Length,
            homogeneous => homogeneous.Count,
            callable => callable.Args.Length + 1 // +1 for the function itself
        );

    public IMetaType AccessIndex(Info info, int index)
    {
        if (index >= Count)
            throw CompilationException.Create(info, $"tuple index {index} is out of range of {Name}");

        return Base.Unwrap(
            basic => basic[index],
            homogeneous => homogeneous.Type,
            callable => index == 0 ? callable.Function : callable.Args[index - 1]
        );
    }

    public IMetaType IndexAddressation(Scope ctx, Info info, IMetaType index)
    {
        var self = this;
        return Base.Unwrap(
            basic => throw CompilationExceptionList.NoIndexAddressation(info, self.Name),
            homogeneous => homogeneous.Type,
            callable => throw CompilationExceptionList.NoIndexAddressation(info, self.Name)
        );
    }

    public IMetaType AccessName(Scope ctx, Info info, string name)
        => name switch {
            "len" => IntMetaType.Get32,
            _ => throw CompilationExceptionList.NoFieldSupport(info, Name, name)
        };

    public bool Callable => Base.Unwrap(
        basic => false,
        homogeneous => false,
        callable => true
    );

    public FunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
    {
        var self = this;
        return Base.Unwrap(
            basic => throw CompilationExceptionList.Uncallable(info, self.Name),
            homogeneous => throw CompilationExceptionList.Uncallable(info, self.Name),
            callable => callable.Function.Call(ctx, info, [.. callable.Args.Select(v => (info, TypedValue.From(ctx, v))), .. args])
        );
    }

    public Nothing Binary(BinaryTypeBuilder bin) =>
        bin.Push(BinaryTypeRepr.TupleStart)
        .Of(Base).Unwrap(
            basic => basic.Select(v => v.Binary(bin)).Ignore(),
            homogeneous =>
                homogeneous.Count == -1
                ? bin.Push(BinaryTypeRepr.Unknown).Of(homogeneous.Type).Binary(bin)
                : homogeneous.Count.Repeat(_ => homogeneous.Type.Binary(bin)),
            callable => callable.Function.Binary(bin).Effect(callable.Args.Select(v => v.Binary(bin)))
        )
        .Of(bin).Push(BinaryTypeRepr.TupleEnd);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => ImplicitCast(ctx, info, to);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to)
        => IMetaType.SmartTypeSelect(
            ctx, info,
            this, to,
            (from, to) =>
            {
                if (to is not TupleMetaType other)
                    return null;

                if (from.Count != other.Count)
                {
                    // the size of homogeneous must be defined
                    if (!(
                        (from.Count == -1 && other.Count != -1)
                        ||
                        (from.Count != -1 && other.Count == -1)
                    ))
                        return null;
                }

                var (ok, mix) = from.Base.Mix(other.Base);
                if (!ok)
                    return null;

                var newBase =
                    mix.Unwrap<TupleBase>(
                        (a, b) =>
                            a
                            .Select((a, i) =>
                            {
                                try
                                {
                                    return a.TemplateCast(ctx, info, b[i]);
                                }
                                catch (CompilationException e)
                                {
                                    throw CompilationException.Create(e.Info, $"at element {i + 1}: {e.BaseMessage}");
                                }
                            })
                            .ToImmutableArray(),
                        (a, b) => (a.Count, a.Type.TemplateCast(ctx, info, b.Type)),
                        (a, b) => (
                            ((Func<IMetaType>)(() =>
                            {
                                try
                                {
                                    return a.Function.TemplateCast(ctx, info, b.Function);
                                }
                                catch (CompilationException e)
                                {
                                    throw CompilationException.Create(e.Info, $"at element 1: {e.BaseMessage}");
                                }
                            }))(),
                            a.Args
                            .Select((a, i) =>
                            {
                                try
                                {
                                    return a.TemplateCast(ctx, info, b.Args[i]);
                                }
                                catch (CompilationException e)
                                {
                                    throw CompilationException.Create(e.Info, $"at element {i + 2}: {e.BaseMessage}");
                                }
                            })
                            .ToImmutableArray()
                        )
                    );

                return new TupleMetaType(newBase);
            }
        );
}
