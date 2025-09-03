namespace Wlow;

public enum ClosureType
{
    None,
    Has,
    Invisible
} 

public interface IValue
{
    Info info { get; }

    IMetaType Type(Scope sc);
    LLVMValue Compile(Scope sc);
    void Closure(Scope sc, Dictionary<string, ClosureType> registered);
}
