using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct ConditionalNode(
    Info Info,
    INode Cond,
    INode If,
    INode Else) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var cond = Cond.TypeResolve(scope.Isolated);
        cond.ValueTypeInfo.Type.ImplicitCast(scope, Cond.Info, BoolMetaType.Get);

        var _if = If.TypeResolve(scope.New);
        var ifNever = _if.ValueTypeInfo.Type.Unwrap() is NeverMetaType;
        var _else = Else.TypeResolve(scope.New);
        var elseNever = _else.ValueTypeInfo.Type.Unwrap() is NeverMetaType;

        bool ifRoot;
        IMetaType type;
        if (ifNever || elseNever)
        {
            ifRoot = elseNever;
            type = elseNever ? _if.ValueTypeInfo.Type : _else.ValueTypeInfo.Type;
        }
        else
        {
            try
            {
                ifRoot = true;
                type = _else.ValueTypeInfo.Type.ImplicitCast(scope, Else.Info, _if.ValueTypeInfo.Type);
            }
            catch
            {
                ifRoot = false;
                type = _if.ValueTypeInfo.Type.ImplicitCast(scope, Else.Info, _else.ValueTypeInfo.Type);
            }
        }

        return new ConditionalNodeTypeResolved(Info, ifRoot, TypedValue.From(scope, type), cond, ifNever, _if, elseNever, _else);
    }

    public override string ToString() => $"if {Cond} = {If}; else = {Else}";
}

public readonly record struct ConditionalNodeTypeResolved(
    Info Info,
    bool IfRoot,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Cond,
    bool IfIsNever,
    INodeTypeResolved If,
    bool ElseIsNever,
    INodeTypeResolved Else) : INodeTypeResolved
{
    public INodeTypeResolved TypeFixation()
        => new ConditionalNodeTypeResolved(Info, IfRoot, ValueTypeInfo.Fixate(), Cond.TypeFixation(), IfIsNever, If.TypeFixation(), ElseIsNever, Else.TypeFixation());
    public override string ToString() => $"{(IfIsNever ? "--: never :-- " : "")}if {Cond} = {If}; {(ElseIsNever ? "--: never :-- " : "")}else = {Else}";
}
