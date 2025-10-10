
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct FunctionValueNode(
    Info Info,
    ImmutableArray<Pair<string, TypedValue>> Arguments,
    INode Body) : INode, INodeTypeResolved
{

    public readonly TypedValue ValueTypeInfo => new(TypeMutability.Const, Type);
    public readonly FunctionDeclaration Declaration => Type.Declaration;
    public readonly FunctionMetaType Type = new FunctionDeclaration(Info, Arguments, Body).CreateType();
    public INodeTypeResolved TypeFixation() => this;
    public INodeTypeResolved TypeResolve(Scope scope) => this;

    public override string ToString() => $"(fn {string.Join(", ", Arguments.Select(v => $"{v.val.Mutability.GetString()} {v.id} {v.val.Type.Name}"))} = {Body})";
}
