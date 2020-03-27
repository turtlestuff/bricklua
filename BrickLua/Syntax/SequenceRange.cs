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

using System;

namespace BrickLua.Syntax
{
    public readonly struct SequenceRange : IEquatable<SequenceRange>
    {
        public SequenceRange(SequencePosition start, SequencePosition end)
        {
            Start = start;
            End = end;
        }

        public SequencePosition Start { get; }
        public SequencePosition End { get; }

        public override bool Equals(object obj) => obj is SequenceRange span ? Equals(span) : false;
        public override int GetHashCode() => HashCode.Combine(Start, End);

        public static bool operator ==(in SequenceRange left, in SequenceRange right) => left.Equals(right);
        public static bool operator !=(in SequenceRange left, in SequenceRange right) => !(left == right);

        public bool Equals(SequenceRange other) => Start.Equals(other.Start) && End.Equals(other.End);
    }
}
