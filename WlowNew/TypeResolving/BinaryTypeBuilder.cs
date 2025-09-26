using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Wlow.Shared;

namespace Wlow.TypeResolving;

/// <summary>
/// Used to build binary type
/// </summary>
public readonly ref struct BinaryTypeBuilder()
{
    static readonly ConcurrentStack<List<byte>> freeStack = new();

    // using of TryPop to atomic check & get in one
    readonly List<byte> repr = freeStack.TryPop(out var result) ? result : [];

    public void Push(BinaryTypeRepr elem) => repr.Add((byte)elem);
    public void Push(byte elem) => repr.Add(elem);
    public void Push(ID elem)
    {
        var repr = this.repr;
        UnsafeCast.ByteIterate(elem, repr.Add);
    }

    /// <summary>
    /// Finallize representation building and 
    /// </summary>
    /// <returns>String representation of type</returns>
    public BinaryType Done()
    {
        unsafe
        {
            if ((repr.Count & 1) == 1) repr.Add(0);
            string res;
            var locrepr = repr;
            fixed (byte* bytes = CollectionsMarshal.AsSpan(repr))
                res = new((char*)bytes, 0, repr.Count / 2);
            // clear before pushing to avoid clearing of already owned list
            repr.Clear();
            freeStack.Push(repr);
            return new(res);
        }
    }
}
