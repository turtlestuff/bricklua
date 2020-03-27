//  
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

using System.Runtime.CompilerServices;

namespace BrickLua.Syntax
{
    public class SyntaxToken : SyntaxNode
    {
        public SyntaxToken(SyntaxKind kind, in SequenceRange location) : base(location)
        {
            Kind = kind;
            Value = default;
        }

        public SyntaxToken(SyntaxKind kind, object value, in SequenceRange location) : base(location)
        {
            Kind = kind;
            Value = value;
        }

        public SyntaxToken(long value, in SequenceRange location) : this(SyntaxKind.IntegerConstant, location)
        {
            Value = value;
        }

        public SyntaxToken(double value, in SequenceRange location) : this(SyntaxKind.FloatConstant, location)
        {
            Value = value;
        }

        public object? Value { get; }
        public SyntaxKind Kind { get; }
    }
}
