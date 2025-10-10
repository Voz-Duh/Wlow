namespace Wlow.Shared;

public readonly record struct Info(int Column, int Line, string LineText)
{
    public string Fmt(string name, bool Before = false)
        => Before
        ? $"{name} ({Line}:{Column})\n{LineText}\n{new string(' ', Column - 1)}^"
        : $"({Line}:{Column}) {name}\n{LineText}\n{new string(' ', Column - 1)}^";
    public override string ToString() => throw new NotSupportedException();
}
