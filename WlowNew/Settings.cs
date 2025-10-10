
using Wlow.Shared;

namespace Wlow;

public static class Settings
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static Platform TargetPlatform = Platform.AMD64;

    public static bool EnableTypeInferenceLog = false;

    public static int OptimizationLevel = 0;
#pragma warning restore CA2211 // Non-constant fields should not be visible
}