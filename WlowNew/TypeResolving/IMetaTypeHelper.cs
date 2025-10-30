namespace Wlow.TypeResolving;

public static class IMetaTypeHelper
{
    public static IMetaType/*TODO IFixatedType*/ Fixate<T>(this T self)
        where T : IMetaType
        => self.Unwrap();//TODO .Fixed();

    public static IMetaType Unwrap<T>(this T self)
        where T : IMetaType
    {
        IMetaType type = self;
        while (true)
        {
            var next = type.UnwrapFn();
            if (next is null)
                break;
            type = next;
        }
        return type;
    }

    public static IMetaType Unweak<T>(this T self)
        where T : IMetaType
    {
        IMetaType type = self;
        while (true)
        {
            var next = type.UnweakFn();
            if (next is null)
                break;
            type = next;
        }
        return type;
    }

    public static ResolveMetaType PreUnwrap<T>(this T self, Scope ctx)
        where T : IMetaType
    {
        ResolveMetaType? lastResolve = null;
        IMetaType type = self;
        while (true)
        {
            var next = type;
            if (next is not ResolveMetaType resolvedNext)
                return lastResolve ?? throw new AggregateException("type is not ResolveMetaType");

            lastResolve = resolvedNext;
            type = resolvedNext.Current;
        }
    }
}
