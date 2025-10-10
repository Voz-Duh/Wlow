namespace Wlow;

public static class ArrayHelper
{
    public static U[] ConvertAll<T, U>(this T[] array, Func<T, U> converter)
    {
        U[] result = new U[array.Length];
        for (int i = 0; i < array.Length; i++)
            result[i] = converter(array[i]);
        return result;
    }

    public static string FmtString<T>(this T[] array)
    {
        return "[" + string.Join(", ", array.ConvertAll(v => v.ToString())) + "]";
    }
}
