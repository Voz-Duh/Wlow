namespace Wlow.TypeResolving;

public static class UnsafeCast
{
    public static B Value<A, B>(A a)
        where A : unmanaged
        where B : unmanaged
    {
        unsafe
        {
            if (BitConverter.IsLittleEndian)
                return *(B*)&a;
            else
                return *(B*)&((byte*)&a)[sizeof(A) - sizeof(B)];
        }
    }

    public static void ByteIterate<T>(T a, Action<byte> action)
        where T : unmanaged
    {
        unsafe
        {
            var ptr = (byte*)(void*)&a;
            int i = 0, end = sizeof(T), step = 1;
            if (!BitConverter.IsLittleEndian)
            {
                i = end - 1;
                end = -1;
                step = -1;
            }
            for (; i != end; i += step)
            {
                action(*ptr++);
            }
        }
    }
}
