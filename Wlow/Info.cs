namespace Wlow;

public readonly record struct Info(int column, int line)
{
    public static readonly Info One = new(1, 1);
    public override string ToString() => $"({line}:{column})";
}
