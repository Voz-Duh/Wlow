
namespace Wlow.Shared;

public enum Platform
{
    X86,
    AMD64,
}

public static class PlatformSpecific
{
    public static long PtrSize(this Platform platform)
        => platform switch
        {
            _ => 8
        };
}
