using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal abstract record BoundNode;

internal abstract record BoundExpression : BoundNode;

internal sealed record BoundLiteralExpression(object Value) : BoundExpression;

internal sealed record BoundBinaryExpression(BoundExpression Left, BoundBinaryOperator Operator, BoundExpression Right) : BoundExpression;

internal sealed record BoundUnaryExpression(BoundUnaryOperator Operator, BoundExpression Operand) : BoundExpression;

internal sealed record BoundVariableExpression(VariableSymbol Variable) : BoundExpression;
