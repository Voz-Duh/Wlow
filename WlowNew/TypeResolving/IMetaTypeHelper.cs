namespace Wlow.TypeResolving;

public static class IMetaTypeHelper
{
    public static IMetaType Unwrap<T>(this T self)
        where T : IMetaType
    {
        IMetaType type = self;
        while (type is ResolveMetaType link) type = link.Current;
        return type;
    }

    public static ResolveMetaType PreUnwrap<T>(this T self)
        where T : IMetaType
    {
        if (self is not ResolveMetaType resolved)
            throw new NotSupportedException(self.GetType().ToString());

        var type = resolved;
        while (true)
        {
            var next = type.Current;
            if (next is not ResolveMetaType resolvedNext)
                return type;
            type = resolvedNext;
        }
    }
}
