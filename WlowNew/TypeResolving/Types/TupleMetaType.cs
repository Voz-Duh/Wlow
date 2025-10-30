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
    readonly bool Fixated;
    public readonly TupleBase Base;

    TupleMetaType(TupleBase @base, bool fixated = false)
    {
        Base = @base;
        Fixated = fixated;
    }
    
    public static TupleMetaType Create(Info info, Scope ctx, ImmutableArray<IMetaType> types)
    {
        // check for callable
        if (types[0].Callable(ctx))
            return new((types[0], types[1..]));

        var builder = new BinaryTypeBuilder();
        var keybin = types[0].Binary(builder, info).Of(builder).Done();

        // check for homogeneous
        if (types.All(v =>
        {
            var builder = new BinaryTypeBuilder();
            var bin = types[0].Binary(builder, info).Of(builder).Done();
            return bin == keybin;
        })) return new((types.Length, types[0]));

        return new(types);
    }

    public static TupleMetaType CreateHomogeneous(int count, IMetaType type)
        => new((count, type));

    public override string ToString()
        => Base.Unwrap(
            basic => $"({string.Join(", ", basic)})",
            homogeneous => $"({homogeneous.Count} {homogeneous.Type})",
            callable => $"({callable.Function}, {string.Join(", ", callable.Args)})"
        );
    public bool IsKnown
        => Base.Unwrap(
            basic => basic.All(v => v.IsKnown),
            homogeneous => homogeneous.Type.IsKnown,
            callable => callable.Function.IsKnown && callable.Args.All(v => v.IsKnown)
        );
    public Opt<uint> ByteSize
        => Base.Unwrap(
            basic => basic.Aggregate(0u, (pre, val) => pre + val.ByteSize),
            homogeneous => (uint)homogeneous.Count * homogeneous.Type.ByteSize,
            callable => callable.Function.ByteSize + callable.Args.Aggregate(0u, (pre, val) => pre + val.ByteSize)
        );
    public TypeMutability Mutability => TypeMutability.Copy;
    public Flg<TypeConvention> Convention
        => Base.Unwrap(
            basic => basic.Aggregate(Flg.From(TypeConvention.Any), (pre, cur) => pre & cur.Convention),
            homogeneous => homogeneous.Type.Convention,
            callable => callable.Args.Aggregate(callable.Function.Convention, (pre, cur) => pre & cur.Convention)
        );

    public int Count
        => Base.Unwrap(
            basic => basic.Length,
            homogeneous => homogeneous.Count,
            callable => callable.Args.Length + 1 // +1 for the function itself
        );
    
    public IMetaType? UnwrapFn()
        => Fixated
        ? null
        : new TupleMetaType(
            Base.Unwrap<TupleBase>(
                basic => basic.Select(v => v.Unwrap()).ToImmutableArray(),
                homogeneous => (homogeneous.Count, homogeneous.Type.Unwrap()),
                callable => (callable.Function.Unwrap(), callable.Args.Select(v => v.Unwrap()).ToImmutableArray())
            ),
            true
        );

    public IMetaType AccessIndex(Scope ctx, Info info, int index)
    {
        if (index >= Count)
            throw CompilationException.Create(info, $"tuple index {index} is out of range of {this}");

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
            basic => throw CompilationExceptionList.NoIndexAddressation(info, self.ToString()),
            homogeneous => homogeneous.Type,
            callable => throw CompilationExceptionList.NoIndexAddressation(info, self.ToString())
        );
    }

    public IMetaType AccessName(Scope ctx, Info info, string name)
        => name switch {
            "len" => IntMetaType.Get32,
            _ => throw CompilationExceptionList.NoFieldSupport(info, ToString(), name)
        };

    public bool Callable => Base.Unwrap(
        basic => false,
        homogeneous => false,
        callable => true
    );

    public IFunctionDefinition Call(Scope ctx, Info info, ImmutableArray<(Info Info, TypedValue Value)> args)
    {
        var self = this;
        return Base.Unwrap(
            basic => throw CompilationExceptionList.Uncallable(info, self.ToString()),
            homogeneous => throw CompilationExceptionList.Uncallable(info, self.ToString()),
            callable => callable.Function.Call(ctx, info, [.. callable.Args.Select(v => (info, TypedValue.From(v))), .. args])
        );
    }

    public Nothing Binary(BinaryTypeBuilder bin, Info info) =>
        bin.Push(BinaryTypeRepr.TupleStart)
        .Of(Base).Unwrap(
            basic => basic.Select(v => v.Binary(bin, info)).Ignore(),
            homogeneous =>
                homogeneous.Count == -1
                ? bin.Push(BinaryTypeRepr.Unknown).Of(homogeneous.Type).Binary(bin, info)
                : homogeneous.Count.Repeat(_ => homogeneous.Type.Binary(bin, info)),
            callable => callable.Function.Binary(bin, info).Effect(callable.Args.Select(v => v.Binary(bin, info)))
        )
        .Of(bin).Push(BinaryTypeRepr.TupleEnd);

    public IMetaType ExplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType ImplicitCast(Scope ctx, Info info, IMetaType to)
        => TemplateCast(ctx, info, to, false);

    public IMetaType TemplateCast(Scope ctx, Info info, IMetaType to, bool repeat)
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
            },
            is_template: true,
            repeat: repeat
        );

    // public IFixType Fixed()
    // {
    //     var self = this;
    //     return Base.Unwrap(
    //         basic => new BasicTupleFixType([.. basic.Select(v => v.Fixate())]),
    //         homogeneous => new HomoTupleFixType(homogeneous.Count, homogeneous.Type.Fixate()),
    //         callable => new CallableTupleFixType(callable.Function.Fixate(), [.. callable.Args.Select(v => v.Fixate())])
    //     );
    // }
}
