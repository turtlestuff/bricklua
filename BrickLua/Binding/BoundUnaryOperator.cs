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
}