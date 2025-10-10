using Wlow.Shared;

namespace Wlow.TypeResolving;

public static class IMetaTypeHelper
{
    public static IMetaType Fixate<T>(this T self)
        where T : IMetaType
        => self.Unwrap();

    public static IMetaType Unwrap<T>(this T self)
        where T : IMetaType
    {
        IMetaType type = self;
        while (true)
        {
            var next = type.FixateFn();
            if (next is null)
                break;
            type = next;
        }
        return type;
        // IMetaType type = self;
        // while (type is ResolveMetaType link) type = link.Current;
        // return type;
    }

    public static ResolveMetaType PreUnwrap<T>(this T self, Scope ctx)
        where T : IMetaType
    {
        ResolveMetaType? lastResolve = null;
        IMetaType type = self;
        while (true)
        {
            var next = type;
            if (next is TypeOfMetaType typeOf)
                next = typeOf.Current(ctx);
            if (next is not ResolveMetaType resolvedNext)
                return lastResolve ?? throw new AggregateException("type is not ResolveMetaType");

            lastResolve = resolvedNext;
            type = resolvedNext.Current;
        }
    }
}
