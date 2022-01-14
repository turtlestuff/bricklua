//
//  Copyright (C) 2020 John Tur
//  
//  This file is part of BrickLua, a simple Lua 5.4 CIL compiler.
//  
//  BrickLua is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//  
//  BrickLua is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY, without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//  
//  You should have received a copy of the GNU Lesser General Public License
//  along with BrickLua.  If not, see <https://www.gnu.org/licenses/>.
//

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
