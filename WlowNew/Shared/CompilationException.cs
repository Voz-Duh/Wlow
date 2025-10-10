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
    public static CompilationException Expected(Info info, string name) => CompilationException.Create(info, $"{name} was expected here");
    public static CompilationException Uncallable(Info info, string name) => CompilationException.Create(info, $"trying to call type {name} which is not callable");
    public static CompilationException NoIndexAddressation(Info info, string name) => CompilationException.Create(info, $"{name} type does not support index addressation");
    public static CompilationException NoFieldSupport<T>(Info info, string name, T field) => CompilationException.Create(info, $"{name} type does not support '{field}' field access");
}
