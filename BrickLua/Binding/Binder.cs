using BrickLua.CodeAnalysis.Symbols;
using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class Binder
{
    private readonly Dictionary<VariableSymbol, object> variables;
    private readonly DiagnosticBag diagnostics;

    public Binder(Dictionary<VariableSymbol, object> variables)
    {
        this.variables = variables;
    }

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
        var name = syntax.Name.Value!.ToString();

        var variable = variables.Keys.FirstOrDefault(k => k.Name == name);
        if (variable is null)
        {
            // Look somewhere else
            return new BoundLiteralExpression(0);
        }

        return new BoundVariableExpression(variable);
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
