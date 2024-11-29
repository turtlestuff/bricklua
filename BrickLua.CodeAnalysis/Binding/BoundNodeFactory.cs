namespace BrickLua.CodeAnalysis.Binding;
using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

internal static class BoundNodeFactory
{
    public static BoundChunk Chunk(BoundBlock Body, ImmutableArray<Diagnostic> Diagnostics)
        => new(Body, Diagnostics);

    public static BoundBlock Block(ImmutableArray<BoundStatement> Statements)
        => new(Statements);

    public static BoundIndexExpression Index(BoundExpression Receiver, BoundExpression IndexArgument)
        => new(Receiver, IndexArgument);

    public static BoundNameExpression Name(LocalSymbol Variable)
        => new(Variable);

    public static BoundCallExpression Call(BoundExpression Receiver, ImmutableArray<BoundExpression> Arguments)
        => new(Receiver, Arguments);

    public static BoundLiteralExpression Literal(object? Value)
        => new(Value);

    public static BoundBinaryExpression Binary(BoundExpression Left, BoundBinaryOperator Operator, BoundExpression Right)
        => new(Left, Operator, Right);

    public static BoundUnaryExpression Unary(BoundUnaryOperator Operator, BoundExpression Operand)
        => new(Operator, Operand);

    public static BoundFunctionExpression Function(BoundBlock Body)
        => new( Body);

    public static BoundTableConstructorExpression TableConstructor(ImmutableArray<BoundFieldAssignment> FieldAssignments)
        => new(FieldAssignments);

    public static BoundFieldAssignment FieldAssignment(BoundExpression Key, BoundExpression Value)
        => new(Key, Value);

    public static BoundExpressionStatement ExpressionStatement(BoundExpression Expression)
        => new(Expression);

    public static BoundAssignmentStatement Assignment(ImmutableArray<BoundVariableExpression> Variables, ImmutableArray<BoundExpression> Expressions)
        => new(Variables, Expressions);

    public static BoundIfStatement If(BoundExpression Condition, BoundBlock Consequent, ImmutableArray<BoundElseIfClause> ElseIfClauses, BoundBlock? ElseClause)
        => new(Condition, Consequent, ElseIfClauses, ElseClause);

    public static BoundElseIfClause ElseIfClause(BoundExpression Condition, BoundBlock Consequent)
        => new(Condition, Consequent);

    public static BoundWhileStatement While(BoundExpression Condition, BoundBlock Body, LabelSymbol BreakLabel)
        => new(Condition, Body, BreakLabel);

    public static BoundForStatement For(ImmutableArray<BoundNameExpression> Variables, ImmutableArray<BoundExpression> ExpressionList, BoundBlock Body, LabelSymbol BreakLabel)
        => new(Variables, ExpressionList, Body, BreakLabel);

    public static BoundNumericalForStatement NumericalFor(BoundExpression InitialValue, BoundExpression Limit, BoundExpression Step, LocalSymbol IndexVariable, BoundBlock Body, LabelSymbol BreakLabel)
        => new(InitialValue, Limit, Step, IndexVariable, Body, BreakLabel);

    public static BoundGotoStatement Goto(LabelSymbol Label)
        => new(Label);

    public static BoundConditionalGotoStatement ConditionalGoto(LabelSymbol Label, BoundExpression Condition, bool JumpIfTrue = true)
        => new(Label, Condition, JumpIfTrue);

    public static BoundConditionalGotoStatement GotoTrue(LabelSymbol Label, BoundExpression Condition)
        => ConditionalGoto(Label, Condition, true);

    public static BoundConditionalGotoStatement GotoFalse(LabelSymbol Label, BoundExpression Condition)
        => ConditionalGoto(Label, Condition, false);

    public static BoundLabelStatement Label(LabelSymbol Label)
        => new(Label);

    public static BoundDoStatement Do(BoundBlock Body)
        => new(Body);

    public static BoundRepeatStatement Repeat(BoundExpression Condition, BoundBlock Body, LabelSymbol BreakLabel)
        => new(Condition, Body, BreakLabel);

    public static BoundBinaryExpression LogicalOr(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalOr), right);

    public static BoundBinaryExpression LogicalAnd(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalAnd), right);

    public static BoundBinaryExpression LessThan(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.LessThan), right);

    public static BoundBinaryExpression LessThanOrEqualTo(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.LessThanOrEqualTo), right);

    public static BoundBinaryExpression GreaterThan(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.GreaterThan), right);

    public static BoundBinaryExpression GreaterThanOrEqualTo(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.GreaterThanOrEqualTo), right);

    public static BoundBinaryExpression NotEqualTo(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.NotEqualTo), right);

    public static BoundBinaryExpression EqualTo(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.EqualTo), right);

    public static BoundBinaryExpression BitwiseOr(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseOr), right);

    public static BoundBinaryExpression BitwiseXor(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseXor), right);

    public static BoundBinaryExpression BitwiseAnd(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseAnd), right);

    public static BoundBinaryExpression ShiftLeft(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftLeft), right);

    public static BoundBinaryExpression ShiftRight(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftRight), right);

    public static BoundBinaryExpression Concatenation(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Concatenation), right);

    public static BoundBinaryExpression Addition(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Addition), right);

    public static BoundBinaryExpression Subtraction(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Subtraction), right);

    public static BoundBinaryExpression Multiplication(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Multiplication), right);

    public static BoundBinaryExpression FloatDivision(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.FloatDivision), right);

    public static BoundBinaryExpression FloorDivision(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.FloorDivision), right);

    public static BoundBinaryExpression Modulus(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Modulus), right);

    public static BoundBinaryExpression Exponentiation(BoundExpression left, BoundExpression right)
        => Binary(left, new BoundBinaryOperator(BoundBinaryOperatorKind.Exponentiation), right);
}
