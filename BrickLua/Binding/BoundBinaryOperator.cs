using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed record BoundBinaryOperator(BoundBinaryOperatorKind Kind, SyntaxKind SyntaxKind)
{
    private static readonly BoundBinaryOperator[] operators =
    {
        new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalOr, SyntaxKind.Or),
        new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalAnd, SyntaxKind.And),
        new BoundBinaryOperator(BoundBinaryOperatorKind.LessThan, SyntaxKind.Less),
        new BoundBinaryOperator(BoundBinaryOperatorKind.LessThanOrEqualTo, SyntaxKind.LessEquals),
        new BoundBinaryOperator(BoundBinaryOperatorKind.GreaterThanOrEqualTo, SyntaxKind.GreaterEquals),
        new BoundBinaryOperator(BoundBinaryOperatorKind.NotEqualTo, SyntaxKind.TildeEquals),
        new BoundBinaryOperator(BoundBinaryOperatorKind.EqualTo, SyntaxKind.EqualsEquals),
        new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseOr, SyntaxKind.Pipe),
        new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseXor, SyntaxKind.Tilde),
        new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseAnd, SyntaxKind.Ampersand),
        new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftLeft, SyntaxKind.LessLess),
        new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftRight, SyntaxKind.GreaterGreater),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Concatenation, SyntaxKind.DotDot),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Addition, SyntaxKind.Plus),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Subtraction, SyntaxKind.Minus),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Multiplication, SyntaxKind.Asterisk),
        new BoundBinaryOperator(BoundBinaryOperatorKind.FloatDivision, SyntaxKind.Slash),
        new BoundBinaryOperator(BoundBinaryOperatorKind.FloorDivision, SyntaxKind.SlashSlash),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Modulus, SyntaxKind.Percent),
        new BoundBinaryOperator(BoundBinaryOperatorKind.Exponentiation, SyntaxKind.Caret),
    };

    public static BoundBinaryOperator? Bind(SyntaxKind syntaxKind) => operators.FirstOrDefault(x => x.SyntaxKind == syntaxKind);
}

internal enum BoundBinaryOperatorKind
{
    LogicalOr,
    LogicalAnd,
    LessThan,
    LessThanOrEqualTo,
    GreaterThanOrEqualTo,
    NotEqualTo,
    EqualTo,
    BitwiseOr,
    BitwiseXor,
    BitwiseAnd,
    ShiftLeft,
    ShiftRight,
    Concatenation,
    Addition,
    Subtraction,
    Multiplication,
    FloatDivision,
    FloorDivision,
    Modulus,
    Exponentiation
}
