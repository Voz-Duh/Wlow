
using System.Collections.Immutable;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct FunctionValueNode(
    Info Info,
    ImmutableArray<Pair<string, TypedValueAnnot>> Arguments,
    TypeAnnot Result,
    INode Body) : INode
{
    public INodeTypeResolved TypeResolve(Scope scope)
    {
        var declaration = new FunctionDeclaration(Info, Arguments, Result, Body);
        return new FunctionValueResolvedNode(
            declaration,
            Arguments,
            TypedValue.From(declaration.CreateType(scope)),
            Body
        );
    }

    public override string ToString() => $"(fn {string.Join(", ", Arguments.Select(v => $"{v.val.Mutability.GetString()} {v.id} {v.val.Type}"))} -> {Result} = {Body})";
}

public readonly record struct FunctionValueResolvedNode(
    FunctionDeclaration declaration,
    ImmutableArray<Pair<string, TypedValueAnnot>> Arguments,
    TypedValue ValueTypeInfo,
    INode Body) : INodeTypeResolved
{

    public INodeTypeResolved TypeFixation() => this;

    public override string ToString() => $"(fn {string.Join(", ", Arguments.Select(v => $"{v.val.Mutability.GetString()} {v.id} {v.val.Type}"))} = {Body})";
}
