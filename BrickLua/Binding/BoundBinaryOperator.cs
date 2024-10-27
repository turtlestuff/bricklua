using System.Collections.Frozen;

using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed record BoundBinaryOperator(BoundBinaryOperatorKind Kind)
{
    private static readonly FrozenDictionary<SyntaxKind, BoundBinaryOperator> operators =
        new Dictionary<SyntaxKind, BoundBinaryOperator>
        {
            { SyntaxKind.Or, new(BoundBinaryOperatorKind.LogicalOr) },
            { SyntaxKind.And, new(BoundBinaryOperatorKind.LogicalAnd) },
            { SyntaxKind.Less, new(BoundBinaryOperatorKind.LessThan) },
            { SyntaxKind.LessEquals, new(BoundBinaryOperatorKind.LessThanOrEqualTo) },
            { SyntaxKind.Greater, new(BoundBinaryOperatorKind.GreaterThan) },
            { SyntaxKind.GreaterEquals, new(BoundBinaryOperatorKind.GreaterThanOrEqualTo) },
            { SyntaxKind.TildeEquals, new(BoundBinaryOperatorKind.NotEqualTo) },
            { SyntaxKind.EqualsEquals, new(BoundBinaryOperatorKind.EqualTo) },
            { SyntaxKind.Pipe, new(BoundBinaryOperatorKind.BitwiseOr) },
            { SyntaxKind.Tilde, new(BoundBinaryOperatorKind.BitwiseXor) },
            { SyntaxKind.Ampersand, new(BoundBinaryOperatorKind.BitwiseAnd) },
            { SyntaxKind.LessLess, new(BoundBinaryOperatorKind.ShiftLeft) },
            { SyntaxKind.GreaterGreater, new(BoundBinaryOperatorKind.ShiftRight) },
            { SyntaxKind.DotDot, new(BoundBinaryOperatorKind.Concatenation) },
            { SyntaxKind.Plus, new(BoundBinaryOperatorKind.Addition) },
            { SyntaxKind.Minus, new(BoundBinaryOperatorKind.Subtraction) },
            { SyntaxKind.Asterisk, new(BoundBinaryOperatorKind.Multiplication) },
            { SyntaxKind.Slash, new(BoundBinaryOperatorKind.FloatDivision) },
            { SyntaxKind.SlashSlash, new(BoundBinaryOperatorKind.FloorDivision) },
            { SyntaxKind.Percent, new(BoundBinaryOperatorKind.Modulus) },
            { SyntaxKind.Caret, new(BoundBinaryOperatorKind.Exponentiation) },
        }.ToFrozenDictionary();

    public static BoundBinaryOperator Bind(SyntaxKind syntaxKind) => operators[syntaxKind];
}

internal enum BoundBinaryOperatorKind
{
    LogicalOr,
    LogicalAnd,
    LessThan,
    LessThanOrEqualTo,
    GreaterThan,
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
