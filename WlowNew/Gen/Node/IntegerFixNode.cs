using System.Numerics;
using Wlow.TypeResolving;

namespace Wlow.Gen;

public readonly record struct IntegerFixNode(
    FixTypedValue ValueTypeInfo,
    BigInteger Value,
    IFixType Type) : IFixNode
{
}