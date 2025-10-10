using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Wlow.Shared;

namespace Wlow.TypeResolving;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 32)]
public readonly struct ID
{
    static readonly DMutex<ID> IdentifierGenerator = DMutex.From(ID.NegOne);
    public static ID Unqiue => IdentifierGenerator.Request().Effect(v => v.Inc()).Done();

    [FieldOffset(0)]
    readonly ulong a;
    [FieldOffset(8)]
    readonly ulong b;
    [FieldOffset(16)]
    readonly ulong c;
    [FieldOffset(24)]
    readonly ulong d;

    ID(ulong a, ulong b, ulong c, ulong d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    public static ID Create(ulong a, ulong b, ulong c, ulong d)
        => BitConverter.IsLittleEndian ? new(a, b, c, d) : new(d, c, b, a);

    public static readonly ID One = Create(1, 0, 0, 0);
    public static readonly ID NegOne = Create(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
    public static readonly ID Zero = new(0, 0, 0, 0);

    public ID Inc()
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong IncrementExceptedOverflow(ulong value, ulong lower, ulong lastlower)
            => value + (lastlower != 0 & lower == 0 ? 1ul : 0ul);

        ulong a, b, c, d;
        if (BitConverter.IsLittleEndian)
        {
            a = this.a + 1;
            b = IncrementExceptedOverflow(this.b, a, this.a);
            c = IncrementExceptedOverflow(this.c, b, this.b);
            d = IncrementExceptedOverflow(this.d, c, this.c);
        }
        else
        {
            a = this.d + 1;
            b = IncrementExceptedOverflow(this.c, a, this.d);
            c = IncrementExceptedOverflow(this.b, b, this.c);
            d = IncrementExceptedOverflow(this.a, c, this.b);
        }

        return new(a, b, c, d);
    }

    public override string ToString() => BitConverter.IsLittleEndian ? $"{a}_{b}_{c}_{d}" : $"{d}_{c}_{b}_{a}";
}
