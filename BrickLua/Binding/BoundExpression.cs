using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal abstract record BoundNode;

internal sealed record BoundChunk(BoundBlock Body, ImmutableArray<Diagnostic> Diagnostics) : BoundNode;

internal sealed record BoundBlock(ImmutableArray<BoundStatement> Statements);

internal abstract record BoundExpression : BoundNode;

internal abstract record BoundVariableExpression : BoundExpression;
internal sealed record BoundIndexExpression(BoundExpression Receiver, BoundExpression IndexArgument) : BoundVariableExpression;
internal sealed record BoundNameExpression(LocalSymbol Variable) : BoundVariableExpression;

internal sealed record BoundCallExpression(BoundExpression Receiver, ImmutableArray<BoundExpression> Arguments) : BoundExpression;
internal sealed record BoundLiteralExpression(object Value) : BoundExpression;
internal sealed record BoundVarargExpression : BoundExpression;
internal sealed record BoundBinaryExpression(BoundExpression Left, BoundBinaryOperator Operator, BoundExpression Right) : BoundExpression;
internal sealed record BoundUnaryExpression(BoundUnaryOperator Operator, BoundExpression Operand) : BoundExpression;
internal sealed record BoundFunctionExpression(BoundBlock Body) : BoundExpression;
internal sealed record BoundErrorExpression : BoundExpression;

internal sealed record BoundTableConstructorExpression(ImmutableArray<BoundFieldAssignment> FieldAssignments) : BoundExpression;
internal sealed record BoundFieldAssignment(BoundExpression Key, BoundExpression Value);

internal abstract record BoundStatement : BoundNode;
internal sealed record BoundExpressionStatement(BoundExpression Expression) : BoundStatement;
internal sealed record BoundAssignmentStatement(ImmutableArray<BoundVariableExpression> Variables, ImmutableArray<BoundExpression> Expressions) : BoundStatement;
internal sealed record BoundIfStatement(BoundExpression Condition, BoundBlock Consequent, ImmutableArray<BoundElseIfClause> ElseIfClauses, BoundBlock? ElseClause) : BoundStatement;
internal sealed record BoundElseIfClause(BoundExpression Condition, BoundBlock Consequent);
internal sealed record BoundWhileStatement(BoundExpression Condition, BoundBlock Body, LabelSymbol BreakLabel) : BoundStatement;
internal sealed record BoundForStatement(LocalSymbol ControlVariable, ImmutableArray<BoundExpression> ExpressionList, BoundBlock Body, LabelSymbol BreakLabel) : BoundStatement;
internal sealed record BoundNumericalForStatement(BoundExpression InitialValue, BoundExpression Limit, BoundExpression Step, LocalSymbol IndexVariable, BoundBlock Body, LabelSymbol BreakLabel) : BoundStatement;
internal sealed record BoundGotoStatement(LabelSymbol Label) : BoundStatement;
internal sealed record BoundLabelStatement(LabelSymbol Label) : BoundStatement;
internal sealed record BoundDoStatement(BoundBlock Body) : BoundStatement;
internal sealed record BoundRepeatStatement(BoundExpression Condition, BoundBlock Body, LabelSymbol BreakLabel) : BoundStatement;
