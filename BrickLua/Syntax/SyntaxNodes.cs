using System.Collections.Immutable;

namespace BrickLua.CodeAnalysis.Syntax;

public sealed record ChunkSyntax(BlockSyntax Body, in SequenceRange Location) : SyntaxNode(Location);

public abstract record StatementSyntax(in SequenceRange Location) : SyntaxNode(Location);

public sealed record AssignmentStatementSyntax(ImmutableArray<PrefixExpressionSyntax> Variables, 
    ImmutableArray<ExpressionSyntax> Values, in SequenceRange Location) : StatementSyntax(Location);

public sealed record BlockSyntax(ImmutableArray<StatementSyntax> Body, ReturnStatementSyntax? Return, in SequenceRange Location) :
    StatementSyntax(Location);

public sealed record BreakStatementSyntax(in SequenceRange Location) : StatementSyntax(Location);

public sealed record DoStatementSyntax(BlockSyntax Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record ElseClauseSyntax(BlockSyntax Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record ElseIfClauseSyntax(ExpressionSyntax Test, BlockSyntax Consequent, in SequenceRange Location) : StatementSyntax(Location);

public sealed record ForStatementSyntax(ImmutableArray<SyntaxToken> NameList, ImmutableArray<ExpressionSyntax> ExpressionList, 
    BlockSyntax Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record FunctionStatementSyntax(FunctionName Name, FunctionBody Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record FunctionName(ImmutableArray<SyntaxToken> DottedNames, SyntaxToken? FieldName);

public sealed record FunctionBody(ImmutableArray<SyntaxToken> ParameterNames, bool IsVararg, BlockSyntax Body);

public sealed record GotoStatementSyntax(SyntaxToken Label, in SequenceRange Location) : StatementSyntax(Location);

public record IfStatementSyntax(ExpressionSyntax Condition, BlockSyntax Consequent, ImmutableArray<ElseIfClauseSyntax> ElseIfClauses, 
    ElseClauseSyntax? ElseClause, in SequenceRange Location) : StatementSyntax(Location);

public sealed record LabelStatementSyntax(SyntaxToken Name, in SequenceRange Location) : StatementSyntax(Location);

public sealed record LocalDeclarationStatementSyntax(ImmutableArray<LocalVariableDeclaration> Declarations, ImmutableArray<ExpressionSyntax> Expressions, 
    in SequenceRange Location) : StatementSyntax(Location);

public sealed record LocalVariableDeclaration(SyntaxToken Name, SyntaxToken? Attribute);

public sealed record LocalFunctionStatementSyntax(SyntaxToken Name, FunctionBody Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record NumericalForStatementSyntax(SyntaxToken InitialValueIdentifier, ExpressionSyntax InitialValue, ExpressionSyntax Limit,
    ExpressionSyntax? Step, BlockSyntax Body, in SequenceRange Location) : StatementSyntax(Location);

public sealed record RepeatStatementSyntax(BlockSyntax Body, ExpressionSyntax Condition, in SequenceRange Location) : StatementSyntax(Location);

public sealed record ReturnStatementSyntax(ImmutableArray<ExpressionSyntax> ReturnValues, in SequenceRange Location) : StatementSyntax(Location);

public sealed record WhileStatementExpression(ExpressionSyntax Condition, BlockSyntax Body, in SequenceRange Location) : StatementSyntax(Location);

public abstract record ExpressionSyntax(in SequenceRange Location) : StatementSyntax(Location);

public sealed record BinaryExpressionSyntax(ExpressionSyntax Left, SyntaxKind Operator, ExpressionSyntax Right, in SequenceRange Location) : 
    ExpressionSyntax(Location);

public sealed record FunctionExpressionSyntax(FunctionBody Body, in SequenceRange Location) : ExpressionSyntax(Location);

public sealed record LiteralExpressionSyntax(SyntaxToken Value, in SequenceRange Location) : ExpressionSyntax(Location);

public sealed record TableConstructorExpressionSyntax(ImmutableArray<FieldAssignmentExpressionSyntax> FieldAssignments, in SequenceRange Location) :
    ExpressionSyntax(Location);

public sealed record FieldAssignmentExpressionSyntax(SyntaxNode Field, ExpressionSyntax? Value, in SequenceRange Location) : ExpressionSyntax(Location);

public sealed record UnaryExpressionSyntax(SyntaxKind Operator, ExpressionSyntax Operand, in SequenceRange Location) : ExpressionSyntax(Location);

public sealed record VarargExpressionSyntax(in SequenceRange Location) : ExpressionSyntax(Location);

public abstract record PrefixExpressionSyntax(in SequenceRange Location) : ExpressionSyntax(Location);

public sealed record CallExpressionSyntax(PrefixExpressionSyntax CalledExpression, SyntaxToken? Name, ImmutableArray<ExpressionSyntax> Arguments, 
    in SequenceRange Location) : PrefixExpressionSyntax(Location);

public sealed record DottedExpressionSyntax(ImmutableArray<PrefixExpressionSyntax> DottedExpressions, in SequenceRange Location) : 
    PrefixExpressionSyntax(Location);

public sealed record IndexExpressionSyntax(PrefixExpressionSyntax IndexedExpression, ExpressionSyntax IndexArgument, in SequenceRange Location) : 
    PrefixExpressionSyntax(Location);

public sealed record NameExpressionSyntax(SyntaxToken Name, in SequenceRange Location) : PrefixExpressionSyntax(Location);

public sealed record ParenthesizedExpressionSyntax(ExpressionSyntax Expression, in SequenceRange Location) : PrefixExpressionSyntax(Location);
