
using Wlow.Shared;

namespace Wlow.TypeResolving;

public enum VariableAbility
{
    None,
    Jump = 1 << 0,
    StoreRead = 1 << 1,
    Full = Jump | StoreRead
}

public readonly record struct Variable(Flg<VariableAbility> Ability, TypedValue Value)
{
    public Variable Include(VariableAbility ability) => new(Ability + ability, Value);
    public Variable Exclude(VariableAbility ability) => new(Ability - ability, Value);
}

public readonly struct Scope
{
    public readonly static Scope Empty = new(null!, Fictive: true);

    readonly Dictionary<string, Variable> Variables;
    readonly ResolveMetaType ErrorTypeTo;
    readonly Mut<bool> ErrorScope;

    public bool IsError => ErrorScope;

    public Scope() => throw new InvalidOperationException("Use Scope.Create to create a new scope");

    Scope(Dictionary<string, Variable> variables, bool Fictive = false)
    {
        if (Fictive)
        {
            Variables = null!;
            ErrorTypeTo = null!;
            ErrorScope = null!;
        }
        else
        {
            Variables = variables;
            ErrorTypeTo = ResolveMetaType.Create();
            ErrorScope = Mut.From(false);
        }
    }

    Scope(Dictionary<string, Variable> variables, Mut<bool> errorScope) : this(variables)
        => ErrorScope = errorScope;

    public static Scope Create() => new([]);

    public void FinalizeErrorType(IMetaType type)
        => ErrorTypeTo.Current = type;

    public IMetaType HandleError()
    {
        ErrorScope.Value = true;
        return ErrorTypeTo;
    }

    public TypedValue CreateArgument(Info info, TypeMutability mutability, string name, IMetaType type)
        => CreateConventionalVariable(info, VariableAbility.StoreRead, mutability, name, type);

    public TypedValue CreateVariable(Info info, TypeMutability mutability, string name, IMetaType type)
        => CreateConventionalVariable(info, VariableAbility.Full, mutability, name, type);

    TypedValue CreateConventionalVariable(Info info, VariableAbility ability, TypeMutability mutability, string name, IMetaType type)
    {
        if (!type.Convention << TypeConvention.InitVariable)
            throw CompilationException.Create(info, $"type {type} is not suitable to be a variable type");

        var res = new Variable(ability, new(mutability, type));
        if (!Variables.TryAdd(name, res))
        {
            throw CompilationException.Create(info, $"variable {name} is already defined");
        }

        return res.Value;
    }

    public void CreateLabel(Info info, string name)
    {
        if (!Variables.TryAdd(name, new(VariableAbility.Jump, default)))
        {
            throw CompilationException.Create(info, $"variable {name} is already defined");
        }
    }

    public TypedValue GetVariable(Info info, string name)
    {
        if (!Variables.TryGetValue(name, out var variable))
        {
            throw CompilationException.Create(info, $"variable {name} is not defined");
        }

        if (!variable.Ability << VariableAbility.StoreRead)
            throw CompilationException.Create(info, $"{name} is a label, not a variable");

        return variable.Value;
    }

    public void ValidateLabel(Info info, string name)
    {
        if (!Variables.TryGetValue(name, out var variable))
        {
            throw CompilationException.Create(info, $"variable {name} is not defined");
        }

        if (!variable.Ability << VariableAbility.Jump)
            throw CompilationException.Create(info, $"variable {name} is not suitable to be a jump label");
    }

    public Scope Copy(VariableAbility Include = VariableAbility.None, VariableAbility Exclude = VariableAbility.None)
        => new(
            new(Variables.Select(v => KeyValuePair.Create(v.Key, v.Value.Include(Include).Exclude(Exclude)))),
            ErrorScope
        );

    public Scope Isolated
        => Copy(Exclude: VariableAbility.Jump);

    public Scope New
        => Copy();

    public static Scope FictiveVariables(Dictionary<string, TypedValue?> fictiveVariables)
        => new(
            new(fictiveVariables.Select(v => KeyValuePair.Create(v.Key, new Variable(VariableAbility.StoreRead, v.Value ?? default)))),
            Mut.From(false)
        );
}
