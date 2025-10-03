using System.Collections.Immutable;
using Wlow.Shared;

namespace Wlow.Parsing;

public partial class ASTGen
{
/*
Arithmetic operators: 
    Unary:
    +                           is  0 + x
                                syntax +x
    -    negation               is  0 - x
                                syntax -x
    ~    bitwise complement     is -1 ~ x
                                syntax ~x
    |    absolute               is "if x < 0 = -x else = x" // maybe add, not sure about
                                syntax |x

    Binary:
    +       sum                 integers, floats
                                syntax a + b
    -       subtraction         integers, floats
                                syntax a - b
    *       multiplication      integers, floats
                                syntax a * b
    /       division            integers, floats
                                syntax a / b
    %       modulo (truncated)  integers
                                syntax a % b

    |       bitwise or          integers
                                syntax a | b
    ~       bitwise xor         integers
                                syntax a ~ b
    &       bitwise and         integers
                                syntax a & b
    <<      left shift          integer << integer >= 0
                                syntax a << b
    >>      right shift         integer >> integer >= 0
                                syntax a >> b
    <<<     left roll           integer <<< integer >= 0
                                syntax a <<< b
    >>>     right roll          integer >>> integer >= 0
                                syntax a >>> b

Comparison operators:
    ==      equal
            syntax a == b
    !=      not equal
            syntax a != b
    <       less
            syntax a < b
    <=      less or equal
            syntax a <= b
    >       greater
            syntax a > b
    >=      greater or equal
            syntax a >= b

Logical operators:
    &&      conditional AND    a && b  is "b if a else false"
    ||      conditional OR     a || b  is "true if a else b"
    !       NOT                !a      is "false if a else true"

Compounds of binary operator and assign:
    +=       sum and assign                   a += b is a = a + b
    -=       subtraction and assign           a -= b is a = a - b
    *=       multiplication and assign        a *= b is a = a * b
    /=       division and assign              a /= b is a = a / b
    %=       modulo (truncated) and assign    a %= b is a = a % b

    |=       bitwise or and assign            a |= b is a = a | b
    ~=       bitwise xor and assign           a ~= b is a = a ~ b
    &=       bitwise and and assign           a &= b is a = a & b
    <<=      left shift and assign            a <<= b is a = a << b
    >>=      right shift and assign           a >>= b is a = a >> b

Address operators:
    &      reference      &a is "reference address to a"
    *      dereference    *a is "dereference address in a"

Handling operators:
    ?      handle higher            a? is "handle a error higher, result is a value"
                                    can be used on nullable pointers, error of nullable pointer is "null reference"
    !      ignore error             a! is "panic if a is an error, result is a value"
                                    can be used on nullable pointers, error of nullable pointer is "null reference"

Ternary operators:
    if a = b else = c
    on name_b in a = b else name_c = c

Precedence      Operator
    7           Unary operators
    6           *   /   %   &  <<   >>  <<<  >>>
    5           +   -   |   ~
    4           ==  !=  <   >  <=  >=
    3           &&
    2           ||
    1           if  on
*/
    
    public static INode Expression(ImmutableArray<Token> Tokens)
    {
        // add return of void if length == 0
        var toks = ManualTokens.Create(Tokens[^1], Tokens.AsSpan());
        return Expression(ref toks, FullScoped: true);
    }

    static INode Expression(ref ManualTokens toks, bool FullScoped = false, bool CommaEnd = false, bool EatDelimiter = true)
    {
        INode node;
        var steps = new List<INode>();
        do
        {
            (var nodeContinue, node) = ExpressionUndelimited(ref toks);
            bool delimiter = false;
            bool _continue = toks.Switch(
                // no delimiter = uncontinue
                Else: (ref _, _) => false,
                // no delimiter, but some token = err
                Default: (ref _, tok) => throw CompilationExceptionList.ExpressionContinue(tok.info),
                // basic delimiter = node continue rule
                (TokenType.Delimiter, (ref _, _) => {
                    delimiter = true;
                    return FullScoped || nodeContinue;
                }),
                // continue delimiter = continue
                (TokenType.ContinueDelimiter, (ref _, _) => true),
                // comma = uncontinue
                (CommaEnd ? TokenType.Comma : TokenType.NaT, (ref _, _) => {
                    delimiter = true;
                    return false;
                })
            );
            if (!_continue)
            {
                if (delimiter && EatDelimiter) toks.StepBack();
                break;
            }
            steps.Add(node);
        }
        while (true);

        return
            steps.Count == 0
            ? node
            : new DelimitedStepsNode([.. steps], node);
    }
}
