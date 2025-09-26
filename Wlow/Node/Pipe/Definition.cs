using LLVMSharp.Interop;

namespace Wlow.Node;

public readonly record struct Definition(Info info, string name, IValue body)
{
    public void Compile(Scope sc)
    {
        var res = body.Compile(sc.CloneNoVariable());
        var link = default(LLVMValueRef);
        if (res.function is null)
        {
            if (res.type.IsGeneric())
                throw new CompileException(info, $"definition value has unstorable type {res.type.Name(sc)}, try to specify type to avoid generics");
            var llvm_type = res.type.Type(sc);
            link = sc.mod.AddGlobal(llvm_type, name);
            link.Linkage = LLVMLinkage.LLVMExternalLinkage;
            link.Initializer = LLVMConst.CreateUndef(llvm_type);
            sc.bi.BuildStore(res.Get(info, sc), link);
        }
        sc.global_variables.TryAdd(name, new(res.type, link: link, function: res.function));
    }

    public override string ToString() => $"{name} :: {body}";
}
