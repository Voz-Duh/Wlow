using System.Numerics;
using Wlow.Shared;
using Wlow.TypeResolving;

namespace Wlow.Parsing;

public readonly record struct IntegerNode(Info Info, BigInteger Value, IntMetaType Type) : INode, INodeTypeResolved
{
    public TypedValue ValueTypeInfo => new(TypeMutability.Copy, Type);
    public INodeTypeResolved TypeFixation() => this;
    public INodeTypeResolved TypeResolve(Scope scope) => this;

    public override string ToString() => $"{Value}{Type.Name}";
}
