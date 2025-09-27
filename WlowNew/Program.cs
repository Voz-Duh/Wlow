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
"
);
Console.WriteLine(string.Join(", ", tokens));
var astRoot = ASTGen.Expression(tokens);
Console.WriteLine("---------- PARSED ----------");
Console.WriteLine(astRoot);
Console.WriteLine("---------- RESOLVED ----------");
Console.WriteLine(astRoot.TypeResolve(new()));
