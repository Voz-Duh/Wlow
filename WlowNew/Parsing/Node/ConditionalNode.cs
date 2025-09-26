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
        var _else = Else.TypeResolve(scope.New);

        bool ifRoot;
        IMetaType type;
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

        return new ConditionalNodeTypeResolved(Info, ifRoot, TypedValue.From(type), cond, _if, _else);
    }

    public override string ToString() => $"if {Cond} = {If} else = {Else}";
}

public readonly record struct ConditionalNodeTypeResolved(
    Info Info,
    bool IfRoot,
    TypedValue ValueTypeInfo,
    INodeTypeResolved Cond,
    INodeTypeResolved If,
    INodeTypeResolved Else) : INodeTypeResolved
{
    public override string ToString() => $"if {Cond} = {If} else = {Else}";
}
