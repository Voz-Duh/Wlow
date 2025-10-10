using Wlow.Shared;

namespace Wlow.TypeResolving;

/// <summary>
/// Used as type binary representation
/// </summary>
public readonly struct BinaryType(string repr)
{
    readonly string repr = repr;

    public override bool Equals(object? obj)
        => obj is BinaryType binary
        ? this == binary
        : repr.Equals(obj);

    public override int GetHashCode() => repr.GetHashCode();
    public static bool operator ==(BinaryType a, BinaryType b) => a.repr == b.repr;
    public static bool operator !=(BinaryType a, BinaryType b) => a.repr != b.repr;
    public override string ToString()
    {
        unsafe
        {
            Span<byte> bytes;
            fixed (char* chars = repr)
                bytes = new((byte*)chars, repr.Length * 2);
            string[] names = new string[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                names[i] = ((BinaryTypeRepr)bytes[i]).ToString();
            return string.Join(", ", names);
        }
    }
}
