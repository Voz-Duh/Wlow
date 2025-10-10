namespace Wlow;

public interface IValue
{
    Info info { get; }

    IMetaType Type(Scope sc);
    LLVMValue Compile(Scope sc);
}
