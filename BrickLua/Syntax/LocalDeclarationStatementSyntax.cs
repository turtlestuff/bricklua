﻿//  
//  Copyright (C) 2020 John Tur
//  
//  This file is part of BrickLua, a high-performance Lua 5.4 implementation in C#.
//  
//  BrickLua is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//  
//  BrickLua is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//  
//  You should have received a copy of the GNU Lesser General Public License
//  along with BrickLua.  If not, see <https://www.gnu.org/licenses/>.
//

using System.Collections.Immutable;

namespace BrickLua.Syntax
{
    public sealed class LocalDeclarationStatementSyntax : StatementSyntax
    {
        public LocalDeclarationStatementSyntax(ImmutableArray<LocalVariableDeclaration> declarations, ImmutableArray<ExpressionSyntax> expressions, in SequenceRange location) : base(location)
        {
            Declarations = declarations;
            Expressions = expressions;
        }

        public ImmutableArray<LocalVariableDeclaration> Declarations { get; }
        public ImmutableArray<ExpressionSyntax> Expressions { get; }
    }

    public class LocalVariableDeclaration
    {
        public LocalVariableDeclaration(SyntaxToken name, SyntaxToken attribute)
        {
            Name = name;
            Attribute = attribute;
        }

        public SyntaxToken Name { get; }
        public SyntaxToken Attribute { get; }
    }
}