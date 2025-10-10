using System.Diagnostics.CodeAnalysis;
using System.Numerics;

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
    readonly bool is1;
    readonly T1 v1;
    readonly T2 v2;

    Or(bool is1, T1 v1, T2 v2)
    {
        this.is1 = is1;
        this.v1 = v1;
        this.v2 = v2;
    }

    public R Unwrap<R>(Func<T1, R> res1, Func<T2, R> res2)
        => is1 ? res1(v1) : res2(v2);

    public bool UnwrapInline([MaybeNullWhen(false)] out T1 res1, [MaybeNullWhen(true)] out T2 res2)
        => (res2 = v2, res1 = v1).Return(is1);

    public static Or<T1, T2> Create(T1 val) => new(true, val, default!);
    public static Or<T1, T2> Create(T2 val) => new(false, default!, val);

    public static implicit operator Or<T1, T2>(T1 val) => Create(val);
    public static implicit operator Or<T1, T2>(T2 val) => Create(val);
}

public readonly struct Rec3<T1, T2, T3>
{
    readonly byte vid;
    readonly T1 v1;
    readonly T2 v2;
    readonly T3 v3;

    Rec3(byte vid, T1 v1, T2 v2, T3 v3)
    {
        this.vid = vid;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public R Unwrap<R>(Func<T1, R> res1, Func<T2, R> res2, Func<T3, R> res3)
        => vid switch
        {
            1 => res1(v1),
            2 => res2(v2),
            3 => res3(v3),
            _ => throw new InvalidOperationException("invalid Rec3 state")
        };

    public (bool ok, MixRec3<T1, T2, T3> res) Mix(Rec3<T1, T2, T3> other)
        => MixRec3<T1, T2, T3>.Create(this, other);

    public static Rec3<T1, T2, T3> Create(T1 val) => new(1, val, default!, default!);
    public static Rec3<T1, T2, T3> Create(T2 val) => new(2, default!, val, default!);
    public static Rec3<T1, T2, T3> Create(T3 val) => new(3, default!, default!, val);

    public static implicit operator Rec3<T1, T2, T3>(T1 val) => Create(val);
    public static implicit operator Rec3<T1, T2, T3>(T2 val) => Create(val);
    public static implicit operator Rec3<T1, T2, T3>(T3 val) => Create(val);
    
    public static (byte vid, T1 v1, T2 v2, T3 v3) UnsafeDestruct(Rec3<T1, T2, T3> rec)
        => (rec.vid, rec.v1, rec.v2, rec.v3);
}


public readonly struct MixRec3<T1, T2, T3>
{
    readonly byte vid;
    readonly T1 v11;
    readonly T1 v21;
    readonly T2 v12;
    readonly T2 v22;
    readonly T3 v13;
    readonly T3 v23;

    MixRec3(byte vid, T1 v11, T1 v21, T2 v12, T2 v22, T3 v13, T3 v23)
    {
        this.vid = vid;
        this.v11 = v11;
        this.v12 = v12;
        this.v13 = v13;
        this.v21 = v21;
        this.v22 = v22;
        this.v23 = v23;
    }

    public R Unwrap<R>(Func<T1, T1, R> res1, Func<T2, T2, R> res2, Func<T3, T3, R> res3)
        => vid switch
        {
            1 => res1(v11, v21),
            2 => res2(v12, v22),
            3 => res3(v13, v23),
            _ => throw new InvalidOperationException("invalid Rec3 state")
        };

    public static (bool ok, MixRec3<T1, T2, T3> res) Create(Rec3<T1, T2, T3> a, Rec3<T1, T2, T3> b)
    {
        var (a_vid, a_v1, a_v2, a_v3) = Rec3<T1, T2, T3>.UnsafeDestruct(a);
        var (b_vid, b_v1, b_v2, b_v3) = Rec3<T1, T2, T3>.UnsafeDestruct(b);
        if (a_vid != b_vid) return (false, default);
        return a_vid switch {
            1 => (true, new(1, a_v1, b_v1, default!, default!, default!, default!)),
            2 => (true, new(2, default!, default!, a_v2, b_v2, default!, default!)),
            3 => (true, new(3, default!, default!, default!, default!, a_v3, b_v3)),
            _ => throw new InvalidOperationException("invalid Rec3 state")
        };
    }
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

public readonly struct Flg<T>
    where T : unmanaged
{
    readonly T Flags;

    Flg(T flags)
        => Flags = flags;

    public static Flg<T> operator |(Flg<T> a, Flg<T> b)
        => (Flg<T>)((long)a | (long)b);
    public static Flg<T> operator &(Flg<T> a, Flg<T> b)
        => (Flg<T>)((long)a & (long)b);
    public static Flg<T> operator ~(Flg<T> a)
        => (Flg<T>)~(long)a;

    public static Flg<T> operator +(Flg<T> a, Flg<T> b)
        => a | b;
    public static Flg<T> operator -(Flg<T> a, Flg<T> b)
        => a & ~b;

    public static Flg<T> operator !(Flg<T> a)
        => ~a;
    public static bool operator <<(Flg<T> a, Flg<T> b)
        => (b & a).Flags.Equals(b.Flags);
    public static bool operator <<(Flg<T> a, T b)
        => a << new Flg<T>(b);

    public static implicit operator Flg<T>(T flags)
        => new(flags);
    public static implicit operator T(Flg<T> flg)
        => flg.Flags;
    public static explicit operator long(Flg<T> flg)
    {
        unsafe
        {
            return UnsafeCast.Value<T, long>(flg) & (sizeof(T) >= 8 ? -1L : (1L << (sizeof(T) * 8)) - 1);
        }
    }
    public static explicit operator Flg<T>(long flg)
        => UnsafeCast.Value<long, T>(flg);

    public override string ToString() => Flags.ToString()!;
}

public static class Flg
{
    public static Flg<T> From<T>(T flags)
        where T : unmanaged, Enum
        => flags;
}

public static class ExpTyHelper
{
    public static T Unwrap<T>(this Or<T, T> value) => value.Unwrap(v => v, v => v);

    public static Nothing Ignore<T>(this T _) => Nothing.Value;
    public static U Of<T, U>(this T _, U result) => result;
    public static U Return<T, U>(this T _, U result) => result;
    public static T Effect<T, U>(this T result, U _) => result;
    public static U Map<T, U>(this T result, Func<T, U> func) => func(result);
    public static Nothing Repeat<T>(this T count, Action<T> action)
        where T : INumber<T>
        => count.Repeat(T.AdditiveIdentity, action);
    public static Nothing Repeat<T>(this T count, T start, Action<T> action)
        where T : INumber<T>
    {
        for (T i = start; i < count; i++) action(i);
        return Nothing.Value;
    }
    public static R Unwrap<R>(this bool on, Func<R> OnTrue, Func<R> OnFalse) => on ? OnTrue() : OnFalse();
}
