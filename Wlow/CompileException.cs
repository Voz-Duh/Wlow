
namespace Wlow;

[Serializable]
public class CompileException(Info info, string message, Exception inner=null) : Exception($"{info} {message}", inner)
{
    public readonly Info Info = info;
    public readonly string BaseMessage = message;
}

[Serializable]
public class StackedCompileException(IEnumerable<Info> stack, Info info, string message, Exception inner=null) : CompileException(
    info,
    string.Join("\n", [message, .. stack.Select(v => $"from {v}"), $"at {info}"]),
    inner)
{
    public readonly Info[] Stack = [.. stack];
}