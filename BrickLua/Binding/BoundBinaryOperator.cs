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

using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding
{
    internal sealed record BoundBinaryOperator(BoundBinaryOperatorKind Kind, SyntaxKind SyntaxKind)
    {
        private static readonly BoundBinaryOperator[] operators =
        {
            new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalOr, SyntaxKind.Or),
            new BoundBinaryOperator(BoundBinaryOperatorKind.LogicalAnd, SyntaxKind.And),
            new BoundBinaryOperator(BoundBinaryOperatorKind.LessThan, SyntaxKind.Less),
            new BoundBinaryOperator(BoundBinaryOperatorKind.LessThanOrEqualTo, SyntaxKind.LessEquals),
            new BoundBinaryOperator(BoundBinaryOperatorKind.GreaterThanOrEqualTo, SyntaxKind.GreaterEquals),
            new BoundBinaryOperator(BoundBinaryOperatorKind.NotEqualTo, SyntaxKind.TildeEquals),
            new BoundBinaryOperator(BoundBinaryOperatorKind.EqualTo, SyntaxKind.EqualsEquals),
            new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseOr, SyntaxKind.Pipe),
            new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseXor, SyntaxKind.Tilde),
            new BoundBinaryOperator(BoundBinaryOperatorKind.BitwiseAnd, SyntaxKind.Ampersand),
            new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftLeft, SyntaxKind.LessLess),
            new BoundBinaryOperator(BoundBinaryOperatorKind.ShiftRight, SyntaxKind.GreaterGreater),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Concatenation, SyntaxKind.DotDot),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Addition, SyntaxKind.Plus),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Subtraction, SyntaxKind.Minus),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Multiplication, SyntaxKind.Asterisk),
            new BoundBinaryOperator(BoundBinaryOperatorKind.FloatDivision, SyntaxKind.Slash),
            new BoundBinaryOperator(BoundBinaryOperatorKind.FloorDivision, SyntaxKind.SlashSlash),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Modulus, SyntaxKind.Percent),
            new BoundBinaryOperator(BoundBinaryOperatorKind.Exponentiation, SyntaxKind.Caret),


        };

        public static BoundBinaryOperator? Bind(SyntaxKind syntaxKind)
        {
            foreach (var op in operators)
            {
                if (op.SyntaxKind == syntaxKind)
                    return op;
            }

            return null;
        }
    }

}