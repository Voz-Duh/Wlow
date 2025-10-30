
namespace Wlow.Shared;

public enum Platform
{
    X86,
    AMD64,
    AArch32,
    AArch64,
    RV32,
    RV64
}

public static class PlatformSpecific
{
    public static uint PtrSize(this Platform platform)
        => platform switch
        {
            Platform.X86 or Platform.AArch32 or Platform.RV32 => 4,
            _ => 8
        };
}
