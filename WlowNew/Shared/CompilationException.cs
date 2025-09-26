namespace Wlow.Shared;

[Serializable]
public class CompilationException(
    Info info,
    string message,
    Exception? inner = null)
    : Exception(info.Fmt(message), inner)
{
    public readonly Info Info = info;
    public readonly string BaseMessage = message;

    public static CompilationException Create(
        Info info,
        string message,
        Exception? inner = null)
        => new(info, message, inner);
}

public static class CompilationExceptionList
{
    public static CompilationException UnexpectedEnd(Info info) => CompilationException.Create(info, "expression is unexpectedly ended");
    public static CompilationException ExpressionInvalid(Info info) => CompilationException.Create(info, "invalid expression");
    public static CompilationException ExpressionContinue(Info info) => CompilationException.Create(info, "completed expression was unexpectedly continued, did you miss ';' or ';;'?");
    public static CompilationException ValueCannotBeEmpty(Info info) => CompilationException.Create(info, "value cannot be empty");
}
