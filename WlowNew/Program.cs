using Wlow.Parsing;

var tokens = Token.Tokenize(
@"
let fib = fn f, n =
    if n <= 1 = n;
    else = (f' f, n - 1) + (f' f, n - 2);

let fib_of_10 = fib' fib, 10
"
);
Console.WriteLine(string.Join(", ", tokens));
var astRoot = ASTGen.Expression(tokens);
Console.WriteLine(astRoot);
Console.WriteLine(astRoot.TypeResolve(new()));
