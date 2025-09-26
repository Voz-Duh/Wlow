namespace Wlow.Shared;

public enum BinaryTypeRepr
{
    None,
    Never, Void,
    PlaceHolder,
    FunctionStart,
    FunctionEnd,
    Bool,
    Int8, Int16, Int32, Int64, Int128,
    UInt8, UInt16, UInt32, UInt64, UInt128,
    Float8, Float16, Float32, Float64, Float128,
}
