using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal abstract record BoundNode;

internal sealed record BoundChunk(BoundBlock Body) : BoundNode;

internal sealed record BoundBlock(ImmutableArray<BoundStatement> Statements) : BoundNode;

internal abstract record BoundStatement : BoundNode;

internal sealed record BoundExpressionStatement(BoundExpression Expression) : BoundStatement;

internal sealed record BoundAssignmentStatement(ImmutableArray<BoundVariableExpression> Variables, ImmutableArray<BoundExpression> Expressions) : BoundStatement;

internal abstract record BoundVariableExpression : BoundExpression;

internal sealed record BoundIndexExpression(BoundExpression Receiver, BoundExpression IndexArgument) : BoundVariableExpression;

internal sealed record BoundNameExpression(VariableSymbol Variable) : BoundVariableExpression;

internal sealed record BoundCallExpression(BoundExpression Receiver, ImmutableArray<BoundExpression> Arguments) : BoundExpression;

internal abstract record BoundExpression : BoundNode;

internal sealed record BoundLiteralExpression(object Value) : BoundExpression;

internal sealed record BoundBinaryExpression(BoundExpression Left, BoundBinaryOperator Operator, BoundExpression Right) : BoundExpression;

internal sealed record BoundUnaryExpression(BoundUnaryOperator Operator, BoundExpression Operand) : BoundExpression;

