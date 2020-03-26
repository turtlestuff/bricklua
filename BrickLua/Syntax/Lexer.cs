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
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace BrickLua.Syntax
{
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not an expected scenario")]
    public ref struct Lexer
    {
        SequenceReader<char> source;

        public Lexer(in SequenceReader<char> reader)
        {
            source = reader;
        }

        public Token Lex()
        {
            @continue:
            if (!source.TryPeek(out var ch))
            {
                return new Token(default, TokenType.EndOfFile);
            }

            var start = source.Position;

            switch (ch)
            {
                case '\n':
                case '\r':
                    goto @continue; // Avoid tail call, inconsistently optimized

                case ' ':
                case '\f':
                case '\t':
                case '\v':
                    source.Advance(1);
                    goto @continue;

                case '-':
                    if (source.TryPeek(out ch) && ch == '-')
                    {
                        // TODO: Long comments
                        source.TryAdvanceTo('\n');
                        goto @continue;
                    }

                    source.Advance(1);

                    return new Token(new SequenceRange(start, source.Position), TokenType.Minus);

                case '_':
                case var _ when ch >= 'A' && ch <= 'Z':
                case var _ when ch >= 'a' && ch <= 'z':
                    return LexIdentifier();

                default:
                    return default; // TODO: Diagnostics
            }
        }

        Token LexIdentifier()
        {
            var start = source.Position;
            source.AdvancePastAny("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_");
            var end = source.Position;

            var str = source.Sequence.Slice(start, end);

            TokenType type = TokenType.Name;
            if (str.Length <= 8)
            {
                // Since this is tiny, it's probably OK to allocate (not likely to be broken across segments).
                var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
                type = SyntaxFacts.GetIdentifierKind(span);
            }

            return new Token(new SequenceRange(start, end), type);
        }
    }
}