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
using System.Globalization;

namespace BrickLua.Syntax
{
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not an expected scenario")]
    public ref struct Lexer
    {
        bool stop;
        public SequenceReader<char> Reader;
        SequencePosition start;

        public Lexer(in SequenceReader<char> reader)
        {
            Reader = reader;
            start = default;
            stop = false;
        }

        public SyntaxToken Lex()
        {
            @continue:
            if (stop || !Reader.TryPeek(out var ch))
            {
                return new SyntaxToken(SyntaxKind.EndOfFile, default);
            }

            start = Reader.Position;

            switch (ch)
            {
                case '\n':
                case '\r':
                case ' ':
                case '\f':
                case '\t':
                case '\v':
                    Reader.Advance(1);
                    goto @continue;

                case '+': return LexSingleOperator(SyntaxKind.Plus);
                case '*': return LexSingleOperator(SyntaxKind.Asterisk);
                case '/': return LexDoubleOperator('/', SyntaxKind.Slash, SyntaxKind.SlashSlash);
                case '=': return LexDoubleOperator('=', SyntaxKind.Equals, SyntaxKind.EqualsEquals);
                case '%': return LexSingleOperator(SyntaxKind.Asterisk);
                case '^': return LexSingleOperator(SyntaxKind.Caret);
                case '~': return LexDoubleOperator('=', SyntaxKind.Tilde, SyntaxKind.TildeEquals);
                case '<': return LexDoubleChoiceOperator('=', '>', SyntaxKind.Less, SyntaxKind.LessEquals, SyntaxKind.LessLess);
                case '>': return LexDoubleChoiceOperator('=', '>', SyntaxKind.Greater, SyntaxKind.GreaterEquals, SyntaxKind.GreaterGreater);
                case '#': return LexSingleOperator(SyntaxKind.Hash);
                case '&': return LexSingleOperator(SyntaxKind.Ampersand);
                case '|': return LexSingleOperator(SyntaxKind.Pipe);
                case '(': return LexSingleOperator(SyntaxKind.OpenParenthesis);
                case ')': return LexSingleOperator(SyntaxKind.CloseParenthesis);
                case '{': return LexSingleOperator(SyntaxKind.OpenBrace);
                case '}': return LexSingleOperator(SyntaxKind.CloseBrace);
                case ']': return LexSingleOperator(SyntaxKind.CloseBracket);
                case ':': return LexDoubleOperator(':', SyntaxKind.Colon, SyntaxKind.ColonColon);
                case ';': return LexSingleOperator(SyntaxKind.Semicolon);
                case ',': return LexSingleOperator(SyntaxKind.Comma);

                case '"':
                    Reader.Advance(1);
                    if (!Reader.TryReadTo(sequence: out var seq, '"'))
                    {
                        stop = true;
                    }

                    return new SyntaxToken(SyntaxKind.StringLiteral, seq.ToString(), new SequenceRange(start, Reader.Position));

                case '[':
                    Reader.Advance(1);
                    var level = 0;
                    while (NextIs('='))
                    {
                        level++;
                    }

                    if (!NextIs('[') && level == 0)
                    {
                        return NewToken(SyntaxKind.OpenBracket);
                    }

                    Span<char> endLongLiteral = new char[level + 2];

                    endLongLiteral.Fill('=');
                    endLongLiteral[0] = ']';
                    endLongLiteral[^1] = ']';

                    var startString = Reader.Position;

                    if (!Reader.TryReadTo(sequence: out var literal, endLongLiteral))
                    {
                        stop = true;
                        return new SyntaxToken(SyntaxKind.StringLiteral, Reader.Sequence.Slice(startString), new SequenceRange(start, Reader.Sequence.End));
                    }

                    return new SyntaxToken(SyntaxKind.StringLiteral, literal.ToString(), new SequenceRange(start, Reader.Position));

                case '-':
                    if (Reader.TryPeek(out var next) && next >= '0' && next <= '9')
                        return LexNumeral();

                    Reader.Advance(1);

                    if (NextIs('-'))
                    {
                        if (NextIs('['))
                        {
                            var numEquals = 0;
                            while (NextIs('='))
                            {
                                numEquals++;
                            }

                            if (NextIs('['))
                            {
                                Span<char> delim = new char[numEquals + 2];

                                delim.Fill('=');
                                delim[0] = ']';
                                delim[^1] = ']';

                                if (!Reader.TryReadTo(sequence: out _, delim))
                                {
                                    stop = true;
                                }
                            }
                        }

                        if (!Reader.TryAdvanceTo('\n'))
                            stop = true;

                        goto @continue;
                    }

                    return NewToken(SyntaxKind.Minus);

                case '.':
                    Reader.Advance(1);

                    if (NextIs('.'))
                        if (NextIs('.'))
                            return NewToken(SyntaxKind.DotDotDot);
                        else
                            return NewToken(SyntaxKind.DotDot);

                    return NewToken(SyntaxKind.Dot);

                case var _ when ch >= '0' && ch <= '9':
                    return LexNumeral();

                case '_':
                case var _ when ch >= 'A' && ch <= 'Z':
                case var _ when ch >= 'a' && ch <= 'z':
                    return LexIdentifier();

                default:
                    Reader.Advance(1);
                    return NewToken(SyntaxKind.BadToken); // TODO: Diagnostics
            }
        }

        SyntaxToken LexIdentifier()
        {
            Reader.Advance(1);
            Reader.AdvancePastAny("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_");
            var str = Reader.Sequence.Slice(start, Reader.Position);

            SyntaxKind type = SyntaxKind.Name;
            if (str.Length <= 8)
            {
                // Since this is tiny, it's probably OK to allocate (not likely to be broken across segments).
                var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
                type = SyntaxFacts.GetIdentifierKind(span);
            }

            return new SyntaxToken(type, str.ToString(), new SequenceRange(start, Reader.Position));
        }

        SyntaxToken LexNumeral()
        {
            // TODO: Replace/enhance hex parsing code with https://github.com/dotnet/runtime/issues/1630

            if (LexInteger(out var token))
            {
                return token;
            }

            throw new NotImplementedException("This type of literal is not supported");
        }

        bool LexInteger(out SyntaxToken token)
        {
            bool negative = NextIs('-');
            if (NextIs('0') && NextIs('X', 'x'))
            {
                var start = Reader.Position;
                Reader.AdvancePastAny("1234567890ABCDEFabcdef");
                var str = Reader.Sequence.Slice(start, Reader.Position);

                var num = long.Parse(str.IsSingleSegment ? str.FirstSpan : str.ToArray(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (negative) num = -num;
                token = new SyntaxToken(num, new SequenceRange(this.start, Reader.Position));
                return true;
            }
            else
            {
                Reader.AdvancePastAny("1234567890ABCDEFabcdef");
                var str = Reader.Sequence.Slice(start, Reader.Position);
                long num = 0;

                foreach (var memory in str)
                {
                    foreach (var ch in memory.Span)
                    {
                        var d = ch - '0';
                        if (num >= long.MaxValue / 10 && (num > long.MaxValue / 10 || d > (long.MaxValue % 10) + (negative ? 1 : 0)))
                        {
                            token = default;
                            return false;
                        }
                        num = num * 10 + d;
                    }
                }

                if (negative) num = -num;
                token = new SyntaxToken(num, new SequenceRange(start, Reader.Position));
                return true;
            }
        }

        SyntaxToken LexSingleOperator(SyntaxKind type)
        {
            Reader.Advance(1);
            return NewToken(type);
        }

        SyntaxToken LexDoubleOperator(char next, SyntaxKind one, SyntaxKind two)
        {
            Reader.Advance(1);
            SyntaxKind type = one;
            if (NextIs(next))
            {
                type = two;
            }

            return NewToken(type);
        }

        SyntaxToken LexDoubleChoiceOperator(char char1, char char2, SyntaxKind none, SyntaxKind type1, SyntaxKind type2)
        {
            Reader.Advance(1);
            SyntaxKind type = none;
            if (NextIs(char1))
            {
                type = type1;
            }
            else if (NextIs(char2))
            {
                type = type2;
            }

            return NewToken(type);
        }

        bool NextIs(char c)
        {
            if (Reader.TryPeek(out var next) && next == c)
            {
                Reader.Advance(1);
                return true;
            }

            return false;
        }

        bool NextIs(char a, char b)
        {
            if (Reader.TryPeek(out var next) && (next == a || next == b))
            {
                Reader.Advance(1);
                return true;
            }

            return false;
        }


        SyntaxToken NewToken(SyntaxKind type) => new SyntaxToken(type, new SequenceRange(start, Reader.Position));
    }
}
