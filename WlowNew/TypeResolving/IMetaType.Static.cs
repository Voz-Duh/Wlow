using Wlow.Shared;

namespace Wlow.TypeResolving;

public partial interface IMetaType
{
    public static IMetaType SmartTypeSelect<T>(
        Scope ctx,
        Info info,
        T from,
        IMetaType to,
        Func<T, IMetaType, IMetaType?> cast,
        bool is_template = false,
        bool repeat = false)
        where T : IMetaType
    {
        IMetaType? result;

        if (to is PlaceHolderMetaType)
            return from;

        var wraped = to;
        ResolveMetaType? wraper = null;
        to = to.Unwrap();

        if (wraped is ResolveMetaType)
            wraper = wraped.PreUnwrap(ctx);

        var binToBuilder = new BinaryTypeBuilder();
        to.Binary(binToBuilder, info);
        var binTo = binToBuilder.Done();

        var binFromBuilder = new BinaryTypeBuilder();
        from.Binary(binFromBuilder, info);
        var binFrom = binFromBuilder.Done();

        if (binFrom == binTo)
        {
            result = from;
            goto PRC_RESULT;
        }

        if (!repeat)
        {
            if (!is_template)
            {
                try
                {
                    return from.TemplateCast(ctx, info, to, true);
                }
                catch { }
            }

            try
            {
                return to.TemplateCast(ctx, info, from, true);
            }
            catch { }
        }

        result = cast(from, to) ?? throw CastError(info, from, to);
        PRC_RESULT:
        wraper?.Current = result;

        return result;
    }

    public static CompilationException CastError(
        Info info,
        IMetaType from,
        IMetaType to)
        => new(info, $"type {from} cannot be casted to type {to}");

    public static CompilationException OperateError(
        string operationName,
        Info info,
        IMetaType from,
        IMetaType to)
        => new(info, $"{operationName} operator is not supported at type {from} with type {to}");

    public static CompilationException OperateError(
        string operationName,
        Info info,
        IMetaType type)
        => new(info, $"{operationName} operator is not supported for type {type}");

    public static OperateChain<T> Operate<T>(Scope context, Info info, T left, IMetaType right)
        where T : IMetaType
        => new(context, info, left, right, null);

    public readonly record struct OperateChain<T>
        where T : IMetaType
    {
        readonly Scope Context;
        readonly Info Info;
        readonly T Left;
        readonly IMetaType Right;
        readonly IMetaType? Result;

        internal OperateChain(Scope context, Info info, T left, IMetaType right, IMetaType? result)
        {
            Context = context;
            Info = info;
            Left = left;
            Right = right;
            Result = result;
        }

        private OperateChain<T> WithResult(IMetaType? Result) => new(Context, Info, Left, Right, Result);

        public OperateChain<T> Start()
        {
            try
            {
                return WithResult(Left.TemplateCast(Context, Info, Right));
            }
            catch { }

            try
            {
                return WithResult(Right.TemplateCast(Context, Info, Left));
            }
            catch { }

            return new(Context, Info, Left, Right.Unwrap(), Result);
        }

        public OperateChain<T> On<U>(Func<T, U, IMetaType?> Function)
            where U : IMetaType
        {
            if (Result is not null)
                return this;

            if (Right is U u)
                return WithResult(Function(Left, u));

            return this;
        }

        public IMetaType Done(string OperationName) => Result ?? throw OperateError(OperationName, Info, Left, Right);
    }

    public static class OpName
    {
        public new const string Equals = "equals";
        public const string NotEquals = "not equals";
        public const string Lower = "lower";
        public const string LowerEquals = "lower or equals";
        public const string Greater = "greater";
        public const string GreaterEquals = "greater or equals";
        public const string Add = "addition";
        public const string Sub = "subtraction";
        public const string Mul = "multiplication";
        public const string Div = "division";
        public const string Mod = "modulo";
        public const string Xor = "bitwise xor";
        public const string BitwiseOr = "bitwise or";
        public const string BitwiseAnd = "bitwise and";
        public const string LogicalOr = "logical or";
        public const string LogicalAnd = "logical and";
        public const string Shl = "left shift";
        public const string Shr = "right shift";
        public const string Rol = "left shift with roll";
        public const string Ror = "right shift with roll";
        public const string Ref = "reference";
        public const string Deref = "dereference";
        public const string Negate = "negation";
        public const string Plus = "plus";
        public const string Not = "logical not";
        public const string Inv = "bitwise inversion";
        public const string On = "on";
    }
}
