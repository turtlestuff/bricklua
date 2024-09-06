using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;
using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class Binder
{
    private BoundScope scope;

    private readonly DiagnosticBag diagnostics;// = new();

    private Binder(BoundScope? parentScope)
    {
        scope = new BoundScope(parentScope);
    }

    public static BoundChunk BindChunk(ChunkSyntax chunk)
    {
        var binder = new Binder(null);
        var block = binder.BindBlock(chunk.Body);
        return new(block);
    }

    private BoundBlock BindBlock(BlockSyntax block)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        scope = new BoundScope(scope);

        foreach (var statement in block.Body)
        {
            statements.Add(BindStatement(statement));
        }

        scope = scope.Parent!;

        return new(statements.DrainToImmutable());
    }

    private BoundStatement BindStatement(StatementSyntax statement) => statement switch
    {
        ExpressionStatementSyntax e => BindExpressionStatement(e),
    };

    private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax e)
        => new(BindExpression(e.Expression));

    public BoundExpression BindExpression(ExpressionSyntax syntax) => syntax switch
    {
        ParenthesizedExpressionSyntax p => BindParenthesizedExpression(p),
        LiteralExpressionSyntax s => BindLiteralExpression(s),
        NameExpressionSyntax n => BindNameExpression(n),
        UnaryExpressionSyntax u => BindUnaryExpression(u),
        BinaryExpressionSyntax b => BindBinaryExpression(b),
        _ => throw new Exception($"Unexpected syntax {syntax.GetType().Name}"),
    };

    private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
    {
        return BindExpression(syntax.Expression);
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var value = syntax.Value.Value ?? 0;
        return new BoundLiteralExpression(value);
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        if (BindVariableReference(syntax.Name) is VariableSymbol variable)
        {
            return new BoundVariableExpression(variable);
        }

        var env = scope.Lookup("_ENV");
        return new BoundIndexExpression(new BoundVariableExpression(env), new BoundLiteralExpression(syntax.Name));
    }

    private VariableSymbol? BindVariableReference(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;

        if (scope.TryLookup(name, out var symbol))
        {
            return symbol;
        }
        else 
        {
            // Diagnostic
            return null;
        }
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);
        var boundOperator = BoundUnaryOperator.Bind(syntax.Operator);

        if (boundOperator is null)
        {
            // Diagnostic
            return boundOperand;
        }

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
    {
        var boundLeft = BindExpression(syntax.Left);
        var boundRight = BindExpression(syntax.Right);
        var boundOperator = BoundBinaryOperator.Bind(syntax.Operator);

        if (boundOperator is null)
        {
            // Diagnostic
            return boundLeft;
        }

        return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
    }
}
