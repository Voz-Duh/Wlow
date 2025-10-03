using Wlow.Parsing;

var tokens = Token.Tokenize(
@"
let Y = fn f = f' f;
Y' Y;
let I = fn v = v;
I' 5;
let K = fn a, b = a;
K' 5, 10;
let S = fn a, b, c = a' c, (b' c);
S' (fn x, y = x + y), (fn x = x * 2), 3

--:
let x = fn a, b =
    b + (a' b)?;
x' (fn a = a * 2), 2;
x' (fn a = if a == 1 = fail; else = a - 1), 2

let x = fn = fail;
x'

let fib = fn f, n =
    if n <= 1 = n;
    else = (f' f, n - 1) + (f' f, n - 2);

let fib_of_10 = fib' fib, 10
:--
"
);
Console.WriteLine(string.Join(", ", tokens));
var astRoot = ASTGen.Expression(tokens);
Console.WriteLine("---------- PARSED ----------");
Console.WriteLine(astRoot);
Console.WriteLine("---------- RESOLVED ----------");
Console.WriteLine(astRoot.TypeResolve(new()));
