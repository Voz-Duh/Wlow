using System.Diagnostics.CodeAnalysis;

namespace Wlow.Shared;

public readonly struct Nothing
{
    public static readonly Nothing Value = default;
    public static Nothing From(Action _)
    {
        _();
        return Value;
    }
}

public readonly struct Opt<T>
{
    public readonly T Value = default!;
    public readonly bool Has = false;

    Opt(T value, bool has)
    {
        Value = value;
        Has = has;
    }

    public bool Unwrap([MaybeNullWhen(false)] out T result)
    {
        result = Value;
        return Has;
    }

    public R Unwrap<R>(R Default, Func<T, R> result)
    {
        if (!Has) return Default;
        return result(Value);
    }

    public static Opt<T> Hasnt() => new(default!, false);
    public static Opt<T> From(T Value) => new(Value, true);

    public static implicit operator T(Opt<T> self) => self.Value;
    public static implicit operator Opt<T>(T self) => new(self, true);
}

public readonly struct Or<T1, T2>
{
    readonly Opt<T1> v1;
    readonly T2 v2;

    Or(Opt<T1> v1, T2 v2)
    {
        this.v1 = v1;
        this.v2 = v2;
    }

    public R Unwrap<R>(Func<T1, R> res1, Func<T2, R> res2)
        => v1.Unwrap(out var v) ? res1(v) : res2(v2);

    public bool UnwrapValue1([MaybeNullWhen(false)] out T1 result)
        => v1.Unwrap(out result);

    public bool UnwrapValue2([MaybeNullWhen(false)] out T2 result)
    {
        result = v2;
        return !v1.Has;
    }

    public static Or<T1, T2> Create(T1 val) => new(val, default!);
    public static Or<T1, T2> Create(T2 val) => new(default, val);

    public static implicit operator Or<T1, T2>(T1 val) => Create(val);
    public static implicit operator Or<T1, T2>(T2 val) => Create(val);
}

public readonly record struct Pair<TId, TVal>(TId id, TVal val);

public static class Pair
{
    public static Pair<TId, TVal> From<TId, TVal>(TId id, TVal val)
        => new(id, val);
}

public class Mut<T>
{
    public T Value;

    Mut(T value) => Value = value;

    public static implicit operator T(Mut<T> v) => v.Value;
    public static implicit operator Mut<T>(T v) => Create(v);

    public static Mut<T> Create(T Value) => new(Value);
}

public static class Mut
{
    public static Mut<T> From<T>(T value) => Mut<T>.Create(value);
}

public readonly struct Monad<T>
{
    readonly Stack<Func<T, T>> Effects;

    public Monad() => Effects = [];

    public T this[T value]
    {
        get
        {
            while (Effects.TryPop(out var effect))
                value = effect(value);
            return value;
        }
    }

    public static Monad<T> operator >>>(Monad<T> monad, Func<T, T> effect)
    {
        monad.Effects.Push(effect);
        return monad;
    }
}

public readonly struct DMutex<T>
{
    readonly Mutex Mutex = new();
    readonly Mut<T> Value;

    public DMutex(T value) => Value = value;
    public DMutex() => Value = Mut.From(default(T)!);

    public Access Request()
    {
        Mutex.WaitOne();
        return new(Value, Mutex);
    }

    /// <summary>
    /// Use only on mutexes for unmanaged values 
    /// </summary>
    public T RequestValue()
    {
        Mutex.WaitOne();
        var v = Value;
        Mutex.ReleaseMutex();
        return v;
    }

    public class Access(Mut<T> value, Mutex mutex)
    {
        readonly Mutex Mutex = mutex;
        readonly Mut<T> Data = value;
        bool Valid = true;

        ~Access()
        {
            if (Valid) throw new AggregateException("access token was not doned");
        }

        public T Value
        {
            get
            {
                if (!Valid) throw new AggregateException("access token is outdated");
                return Data;
            }
            set
            {
                if (!Valid) throw new AggregateException("access token is outdated");
                Data.Value = value;
            }
        }

        public Access Effect(Func<T, T> Function)
        {
            Value = Function(Value);
            return this;
        }

        public R EffectResult<R>(Func<T, R> Function)
        {
            var res = Function(Value);
            Done();
            return res;
        }

        public T Done()
        {
            var v = Value;
            Valid = false;
            Mutex.ReleaseMutex();
            return v;
        }
    }
}

public static class DMutex
{
    public static DMutex<T> From<T>(T Value) => new(Value);
}

public static class ExpTyHelper
{
    public static T Unwrap<T>(this Or<T, T> value) => value.Unwrap(v => v, v => v);

    public static Nothing Ignore<T>(this T _) => Nothing.Value;
    public static U Return<T, U>(this T _, U result) => result;
    public static T Effect<T, U>(this T result, U _) => result;
}
