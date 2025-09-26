using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.TypeResolving;

[Serializable]
public class StackedCompilationException(
    ImmutableArray<Info> stack,
    Info info,
    string message,
    Exception? inner = null)
    : CompilationException(
    info,
    string.Join("\n", [message, .. stack.Select(v => v.Fmt("from", Before: true)), info.Fmt("at", Before: true)]),
    inner)
{
    public readonly ImmutableArray<Info> Stack = stack;
}
