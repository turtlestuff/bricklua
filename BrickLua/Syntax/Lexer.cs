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
using System.Resources;
using System.Text;

namespace BrickLua.Syntax
{
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not an expected scenario")]
    public ref struct Lexer
    {
        bool stop;
        SequenceReader<char> reader;
        SequencePosition start;

        public Lexer(in SequenceReader<char> reader)
        {
            this.reader = reader;
            start = default;
            stop = false;
        }

        public SyntaxToken Lex()
        {
            @continue:
            if (stop || !reader.TryPeek(out var ch))
            {
                return new SyntaxToken(SyntaxKind.EndOfFile, default);
            }

            start = reader.Position;

            switch (ch)
            {
                case '\n':
                case '\r':
                case ' ':
                case '\f':
                case '\t':
                case '\v':
                    reader.Advance(1);
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
                    reader.Advance(1);
                    if (!reader.TryReadTo(sequence: out var seq, '"'))
                    {
                        stop = true;
                    }

                    return new SyntaxToken(SyntaxKind.StringLiteral, ParseString(seq, false), new SequenceRange(start, reader.Position));

                case '[':
                    reader.Advance(1);
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

                    var startString = reader.Position;

                    if (!reader.TryReadTo(sequence: out var literal, endLongLiteral))
                    {
                        stop = true;
                        return new SyntaxToken(SyntaxKind.StringLiteral, ParseString(reader.Sequence.Slice(startString), true), new SequenceRange(start, reader.Sequence.End));
                    }
                    var read = new SequenceReader<char>(literal);

                    var str = literal.ToArray();

                    return new SyntaxToken(SyntaxKind.StringLiteral, ParseString(literal, true), new SequenceRange(start, reader.Position));

                case '-':
                    if (reader.TryPeek(out var next) && next >= '0' && next <= '9')
                        return LexNumeral();

                    reader.Advance(1);

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

                                if (!reader.TryReadTo(sequence: out _, delim))
                                {
                                    stop = true;
                                }
                            }
                        }

                        if (!reader.TryAdvanceTo('\n'))
                            stop = true;

                        goto @continue;
                    }

                    return NewToken(SyntaxKind.Minus);

                case '.':
                    reader.Advance(1);

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
                    reader.Advance(1);
                    return NewToken(SyntaxKind.BadToken); // TODO: Diagnostics
            }
        }

        SyntaxToken LexIdentifier()
        {
            reader.Advance(1);
            reader.AdvancePastAny("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_");
            var str = reader.Sequence.Slice(start, reader.Position);

            SyntaxKind type = SyntaxKind.Name;
            if (str.Length <= 8)
            {
                // Since this is tiny, it's probably OK to allocate (not likely to be broken across segments).
                var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
                type = SyntaxFacts.GetIdentifierKind(span);
            }

            return new SyntaxToken(type, str.ToArray().AsMemory(), new SequenceRange(start, reader.Position));
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
                var start = reader.Position;
                reader.AdvancePastAny("1234567890ABCDEFabcdef");
                var str = reader.Sequence.Slice(start, reader.Position);

                var num = long.Parse(str.IsSingleSegment ? str.FirstSpan : str.ToArray(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (negative) num = -num;
                token = new SyntaxToken(num, new SequenceRange(this.start, reader.Position));
                return true;
            }
            else
            {
                reader.AdvancePastAny("1234567890ABCDEFabcdef");
                var str = reader.Sequence.Slice(start, reader.Position);
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
                token = new SyntaxToken(num, new SequenceRange(start, reader.Position));
                return true;
            }
        }

        SyntaxToken LexSingleOperator(SyntaxKind type)
        {
            reader.Advance(1);
            return NewToken(type);
        }

        SyntaxToken LexDoubleOperator(char next, SyntaxKind one, SyntaxKind two)
        {
            reader.Advance(1);
            SyntaxKind type = one;
            if (NextIs(next))
            {
                type = two;
            }

            return NewToken(type);
        }

        SyntaxToken LexDoubleChoiceOperator(char char1, char char2, SyntaxKind none, SyntaxKind type1, SyntaxKind type2)
        {
            reader.Advance(1);
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

        ReadOnlyMemory<char> ParseString(in ReadOnlySequence<char> str, bool multiLine)
        {
            var buffer = new ArrayBufferWriter<char>(checked((int) str.Length));

            var reader = new SequenceReader<char>(str);

            if (multiLine)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (reader.TryPeek(out var start) && (start is '\r' || start is 'n'))
                        reader.Advance(1);
                    else
                        break;
                }

                while (reader.TryReadToAny(sequence: out var sequence, "\r\n"))
                {
                    foreach (var memory in sequence)
                    {
                        buffer.Write(memory.Span);
                    }

                    buffer.Write(stackalloc char[] { '\n' });

                    if (reader.TryPeek(out var next) && (next is '\r' || next is 'n'))
                    {
                        reader.Advance(1);
                    }
                }

                foreach (var memory in reader.Sequence.Slice(reader.Position))
                {
                    buffer.Write(memory.Span);
                }
            }
            else
            {
                while (reader.TryReadTo(sequence: out var sequence, '\\'))
                {
                    foreach (var memory in sequence)
                    {
                        buffer.Write(memory.Span);
                    }

                    if (!reader.TryRead(out var c))
                    {
                        break;
                    }

                    switch (c)
                    {
                        case 'a':
                            buffer.Write(stackalloc char[] { '\a' });
                            break;
                        case 'b':
                            buffer.Write(stackalloc char[] { '\b' });
                            break;
                        case 'f':
                            buffer.Write(stackalloc char[] { '\f' });
                            break;
                        case 'n':
                            buffer.Write(stackalloc char[] { '\n' });
                            break;
                        case 'r':
                            buffer.Write(stackalloc char[] { '\r' });
                            break;
                        case 't':
                            buffer.Write(stackalloc char[] { '\t' });
                            break;
                        case 'v':
                            buffer.Write(stackalloc char[] { '\v' });
                            break;
                        case '\\':
                            buffer.Write(stackalloc char[] { '\\' });
                            break;
                        case '"':
                            buffer.Write(stackalloc char[] { '"' });
                            break;
                        case '\'':
                            buffer.Write(stackalloc char[] { '\'' });
                            break;
                        case 'x':
                            if (!reader.TryRead(out var ch1) & !reader.TryRead(out var ch2))
                            {
                                break;
                            }

                            var num = byte.Parse(stackalloc char[] { ch1, ch2 }, NumberStyles.AllowHexSpecifier);
                            buffer.Write(stackalloc char[] { (char) num });
                            break;
                        case 'u':
                            if (!reader.TryRead(out var openBrace) || openBrace != '{')
                            {
                                break;
                            }

                            if (!reader.TryReadTo(span: out var span, '}'))
                            {
                                break;
                            }

                            var value = int.Parse(span, NumberStyles.AllowHexSpecifier);

                            Span<char> chars = stackalloc char[2];
                            new Rune(value).EncodeToUtf16(chars);
                            buffer.Write(chars);
                            break;

                        case var d when c >= '0' && c <= '9':
                            Span<char> escape = stackalloc char[3];
                            escape[0] = d;
                            if (reader.TryRead(out var d2) && d2 >= '0' && c <= '9')
                            {
                                escape[1] = d2;
                                if (reader.TryRead(out var d3) && d2 >= '0' && c <= '9')
                                    escape[2] = d3;
                                else
                                    escape = escape[..2];
                            }
                            else
                            {
                                escape = escape[..1];
                            }

                            var shortNum = short.Parse(escape);
                            buffer.Write(stackalloc char[] { (char) shortNum });
                            break;
                        default:
                            break;
                    }
                }

                foreach (var memory in reader.Sequence.Slice(reader.Position))
                {
                    buffer.Write(memory.Span);
                }
            }

            return buffer.WrittenMemory;
        }

        bool NextIs(char c)
        {
            if (reader.TryPeek(out var next) && next == c)
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        bool NextIs(char a, char b)
        {
            if (reader.TryPeek(out var next) && (next == a || next == b))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }


        SyntaxToken NewToken(SyntaxKind type) => new SyntaxToken(type, new SequenceRange(start, reader.Position));
    }
}
