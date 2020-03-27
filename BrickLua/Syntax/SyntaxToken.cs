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
            IntegerData = default;
        }

        public SyntaxToken(long numericData, in SequenceRange location) : this(SyntaxKind.IntegerConstant, location)
        {
            IntegerData = numericData;
        }

        public SyntaxToken(double numericData, in SequenceRange location) : this(SyntaxKind.FloatConstant, location)
        {
            IntegerData = Unsafe.As<double, long>(ref numericData);
        }

        public long IntegerData { get; }
        public override SyntaxKind Kind { get; }

        public double FloatData
        {
            get
            {
                var data = IntegerData;
                return Unsafe.As<long, double>(ref data);
            }
        }
    }
}
