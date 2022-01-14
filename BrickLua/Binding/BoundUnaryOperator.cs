using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed record BoundUnaryOperator(BoundUnaryOperatorKind Kind, SyntaxKind SyntaxKind)
{
    private static readonly BoundUnaryOperator[] operators =
    {
            new BoundUnaryOperator(BoundUnaryOperatorKind.LogicalNot, SyntaxKind.Not),
            new BoundUnaryOperator(BoundUnaryOperatorKind.Length, SyntaxKind.Hash),
            new BoundUnaryOperator(BoundUnaryOperatorKind.Negation, SyntaxKind.Minus),
            new BoundUnaryOperator(BoundUnaryOperatorKind.BitwiseNot, SyntaxKind.Tilde),
        };

    public static BoundUnaryOperator? Bind(SyntaxKind syntaxKind)
    {
        foreach (var op in operators)
        {
            if (op.SyntaxKind == syntaxKind)
                return op;
        }

        return null;
    }
}

internal enum BoundUnaryOperatorKind
{
    Negation,
    Length,
    BitwiseNot,
    LogicalNot
}
