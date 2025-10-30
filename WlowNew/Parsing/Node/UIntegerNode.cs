using System.Numerics;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct UIntegerNode(Info Info, BigInteger Value, UIntMetaType Type) : INode, INodeTypeResolved
{
    public TypedValue ValueTypeInfo => new(TypeMutability.Copy, Type);
    public INodeTypeResolved TypeFixation() => this;
    public INodeTypeResolved TypeResolve(Scope scope) => this;

    public override string ToString() => $"{Value}{Type}";
}
