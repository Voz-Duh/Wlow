
namespace Wlow.TypeResolving;

public enum TypeConvention
{
    None,
    /// <summary>
    /// can be used as return type
    /// </summary>
    Return = 1 << 0,
    /// <summary>
    /// can be used to initialize variable
    /// </summary>
    InitVariable = 1 << 1,
    /// <summary>
    /// can be used in set operation (a = b)
    /// </summary>
    Set = 1 << 2,
    /// <summary>
    /// can be used in any context
    /// </summary>
    Any = Return | InitVariable | Set
}
