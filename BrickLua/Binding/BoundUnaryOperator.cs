using System.Collections.Frozen;

using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed record BoundUnaryOperator(BoundUnaryOperatorKind Kind)
{
    private static readonly FrozenDictionary<SyntaxKind, BoundUnaryOperator> operators =
        new Dictionary<SyntaxKind, BoundUnaryOperator>
        {
            { SyntaxKind.Not, new(BoundUnaryOperatorKind.LogicalNot) },
            { SyntaxKind.Hash, new(BoundUnaryOperatorKind.Length) },
            { SyntaxKind.Minus, new(BoundUnaryOperatorKind.Negation) },
            { SyntaxKind.Tilde, new(BoundUnaryOperatorKind.BitwiseNot) },
        }.ToFrozenDictionary();

    public static BoundUnaryOperator Bind(SyntaxKind syntaxKind)
    {
        return operators[syntaxKind];
    }
}

internal enum BoundUnaryOperatorKind
{
    Negation,
    Length,
    BitwiseNot,
    LogicalNot
}
