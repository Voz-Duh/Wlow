// using Swigged.LLVM;

// LLVM.InitializeCore(LLVM.GetGlobalPassRegistry());

// var ctx = LLVM.ContextCreate();

// var mod = LLVM.ModuleCreateWithNameInContext("name", ctx);

// var fn = LLVM.AddFunction(mod, "test",
//     LLVM.FunctionType(
//         LLVM.Int32Type(),
//         [LLVM.Int32Type(), LLVM.Int32Type()],
//         IsVarArg: false
//     ));
// var entry = LLVM.AppendBasicBlock(fn, "entry");

// var bi = LLVM.CreateBuilder();
// LLVM.PositionBuilderAtEnd(bi, entry);

// var add = LLVM.BuildAdd(
//     bi,
//     LLVM.GetParam(fn, 0),
//     LLVM.GetParam(fn, 1),
//     "add"
// );

// LLVM.BuildRet(bi, add);

// var strhandler = new MyString();

// if (!LLVM.VerifyModule(mod, VerifierFailureAction.PrintMessageAction, strhandler))
//     Console.WriteLine(strhandler);

// LLVM.LinkInMCJIT();
// LLVM.InitializeNativeTarget();
// LLVM.InitializeNativeAsmPrinter();
// LLVM.InitializeNativeAsmParser();
// LLVM.InitializeNativeDisassembler();

// var opts = new MCJITCompilerOptions();
// LLVM.InitializeMCJITCompilerOptions(opts, 1024);
// if (!LLVM.CreateJITCompilerForModule(out var jit, mod, 0, strhandler))
//     Console.WriteLine(strhandler);

// Console.WriteLine(LLVM.PrintModuleToString(mod));

// unsafe
// {
//     var prog = (delegate* unmanaged[Cdecl]<int, int, int>)LLVM.GetPointerToGlobal(jit, fn);
//     Console.WriteLine(prog(1, 4));
// }
using Wlow.Parsing;
using Wlow.TypeResolving;
using System.Diagnostics;

var sw = Stopwatch.StartNew();
var tokens = Token.Tokenize(
@"
-- let id = fn x=x;
-- let tuple = (1, );
-- id' tuple.0;
-- id' tuple.1;
-- id' tuple.0 + tuple.1
-- 
-- ------------
--
-- let Y = fn f = f' f;
-- Y' Y;
-- let I = fn v = v;
-- I' 5;
-- let K = fn a, b = a;
-- K' 5, 10;
-- let S = fn a, b, c = a' c, (b' c);
-- S' (fn x, y = x + y), (fn x = x * 2), 3
-- 
-- ------------
-- 
-- let x = fn a, b =
--     b + (a' b)?;
-- x' (fn a = a * 2), 2;
-- x' (fn a = if a == 1 = fail; else = a - 1), 2
-- 
-- ------------
-- 
-- let x = fn = fail;
-- x'
-- 
-- ------------
-- 
-- let fib = fn f, n =
--    if n <= 1 = n;
--    else = (f' f, n - 1) + (f' f, n - 2);
-- let fib_of_10 = fib' fib, 10
-- 
-- ------------
-- 
-- let z = 2i64;
-- let x = 2;
-- let y &(z) = x + 1; -- y type is &(z) which means y type is a type of z 
-- y + x
-- 
-- ------------
-- 
-- let id = fn x = x;
-- id' 6
-- 
-- ------------
-- 
-- let apply_twice = fn f, x = f' (f' x);
-- apply_twice' (fn x = x + 1), 5
-- 
-- ------------
-- 
-- let F =
--   fn S, x =
--     let I = fn O, y = O' O, y;
--     I' S, x;
-- F' F, 0
-- 
-- ------------
-- 
-- let x = fn a, b &(a) = a + b;
-- x' 3i64, 4
-- 
-- ------------
-- 
let x = fn a, b, c -> &(a) = b;
x' 3i64, 4, 56i8
"
);
sw.Stop();
Console.WriteLine($"Time: {sw.Elapsed}");
Console.WriteLine(string.Join(", ", tokens));
sw.Restart();
var astRoot = ASTGen.Expression(tokens);
sw.Stop();
Console.WriteLine($"Time: {sw.Elapsed}");
Console.WriteLine("---------- PARSED ----------");
sw.Restart();
Console.WriteLine(astRoot);
sw.Stop();
Console.WriteLine($"Time: {sw.Elapsed}");
Console.WriteLine("---------- RESOLVED ----------");
var resolved = astRoot.TypeResolve(Scope.Create());
Console.WriteLine(resolved);
Console.WriteLine("---------- FIXED ----------");
var fixated = resolved.TypeFixation();
Console.WriteLine(fixated);
