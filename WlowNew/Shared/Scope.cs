using Wlow.Parsing;
using Wlow.TypeResolving;

namespace Wlow.Shared;

[Flags]
public enum VariableAbility
{
    None,
    Jump,
    StoreRead,
    Full = Jump | StoreRead
}

public readonly record struct Variable(VariableAbility Ability, TypedValue Value)
{
    public Variable Include(VariableAbility ability) => new(Ability | ability, Value);
    public Variable Exclude(VariableAbility ability) => new(Ability & ~ability, Value);
}

public readonly struct Scope
{
    readonly Dictionary<string, Variable> Variables;
    readonly ResolveMetaType ErrorTypeTo = new();
    readonly Mut<bool> ErrorScope = Mut.From(false);
    public bool IsError => ErrorScope.Value;

    public Scope()
    {
        Variables = [];
    }

    Scope(Dictionary<string, Variable> variables, Mut<bool> errorScope)
    {
        Variables = variables;
        ErrorScope = errorScope;
    }

    public void FinalizeErrorType(IMetaType type)
        => ErrorTypeTo.Current = type;

    public IMetaType HandleError()
    {
        ErrorScope.Value = true;
        return ErrorTypeTo;
    }

    public TypedValue CreateVariable(Info info, Mutability mutability, string name, IMetaType type)
    {
        var res = new Variable(VariableAbility.Full, new(mutability, type));
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
            throw CompilationException.Create(info, $"variable {name} was not been defined");
        }

        if (!variable.Ability.HasFlag(VariableAbility.StoreRead))
            throw CompilationException.Create(info, $"{name} is not a variable with value");

        return variable.Value;
    }

    public void ValidateLabel(Info info, string name)
    {
        if (!Variables.TryGetValue(name, out var variable))
        {
            throw CompilationException.Create(info, $"variable {name} was not been defined");
        }

        if (!variable.Ability.HasFlag(VariableAbility.Jump))
            throw CompilationException.Create(info, $"variable {name} is cannot be used to jump here");
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

    // public Scope Variables(Dictionary<string, ITypedValue> variables)
    // {
    //     Console.WriteLine(string.Join("\n", variables.Select(v => $"{v.Key} {v.Value.Type.Name}")));
    //     return default;
    // }
}
