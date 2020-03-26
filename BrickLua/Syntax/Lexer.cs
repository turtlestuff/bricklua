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
        SequencePosition start;

        public Lexer(in SequenceReader<char> reader)
        {
            source = reader;
            start = default;
        }

        public Token Lex()
        {
            @continue:
            if (!source.TryPeek(out var ch))
            {
                return new Token(default, TokenType.EndOfFile);
            }

            start = source.Position;

            source.Advance(1);

            switch (ch)
            {
                case '\n':
                case '\r':
                    goto @continue; // Avoid tail call, inconsistently optimized

                case ' ':
                case '\f':
                case '\t':
                case '\v':
                    goto @continue;

                case '+': return LexSingleOperator(TokenType.Plus);
                case '*': return LexSingleOperator(TokenType.Asterisk);
                case '/': return LexDoubleOperator('/', TokenType.Slash, TokenType.SlashSlash);
                case '=': return LexDoubleOperator('=', TokenType.Equals, TokenType.EqualsEquals);
                case '%': return LexSingleOperator(TokenType.Asterisk);
                case '^': return LexSingleOperator(TokenType.Caret);
                case '~': return LexDoubleOperator('=', TokenType.Tilde, TokenType.TildeEquals);
                case '<': return LexDoubleChoiceOperator('=', '>', TokenType.Less, TokenType.LessEquals, TokenType.LessLess);
                case '>': return LexDoubleChoiceOperator('=', '>', TokenType.Greater, TokenType.GreaterEquals, TokenType.GreaterGreater);
                case '#': return LexSingleOperator(TokenType.Hash);
                case '&': return LexSingleOperator(TokenType.Ampersand);
                case '|': return LexSingleOperator(TokenType.Pipe);
                case '(': return LexSingleOperator(TokenType.OpenParenthesis);
                case ')': return LexSingleOperator(TokenType.CloseParenthesis);
                case '{': return LexSingleOperator(TokenType.OpenBrace);
                case '}': return LexSingleOperator(TokenType.CloseBrace);
                case '[': return LexSingleOperator(TokenType.OpenBracket);
                case ']': return LexSingleOperator(TokenType.CloseBracket);
                case ':': return LexDoubleOperator(':', TokenType.Colon, TokenType.ColonColon);
                case ';': return LexSingleOperator(TokenType.Semicolon);
                case ',': return LexSingleOperator(TokenType.Comma);

                case '-':
                    if (NextIs('-'))
                    {
                        // TODO: Long comments
                        source.TryAdvanceTo('\n');
                        goto @continue;
                    }

                    return NewToken(TokenType.Minus);

                case '.':
                    if (NextIs('.'))
                    {
                        if (NextIs('.'))
                        {
                            return NewToken(TokenType.DotDotDot);
                        }

                        return NewToken(TokenType.DotDot);
                    }

                    return NewToken(TokenType.Dot);

                case var _ when ch >= '0' && ch <= '9':
                    return LexNumeral();

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
            source.AdvancePastAny("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_");
            var str = source.Sequence.Slice(start, source.Position);

            TokenType type = TokenType.Name;
            if (str.Length <= 8)
            {
                // Since this is tiny, it's probably OK to allocate (not likely to be broken across segments).
                var span = str.IsSingleSegment ? str.FirstSpan : str.ToArray();
                type = SyntaxFacts.GetIdentifierKind(span);
            }

            return NewToken(type);
        }

        Token LexNumeral()
        {
            source.AdvancePastAny("0123456789.");
            var str = source.Sequence.Slice(start, source.Position);

            // TODO: Exponent literals
            return NewToken(TokenType.FloatConstant);
        }


        Token LexSingleOperator(TokenType type)
        {
            return NewToken(type);
        }

        Token LexDoubleOperator(char next, TokenType one, TokenType two)
        {
            TokenType type = one;
            if (NextIs(next))
            {
                type = two;
            }

            return NewToken(type);
        }

        Token LexDoubleChoiceOperator(char char1, char char2, TokenType none, TokenType type1, TokenType type2)
        {
            TokenType type = none;
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
            if (source.TryPeek(out var next) && next == c)
            {
                source.Advance(1);
                return true;
            }

            return false;
        }

        Token NewToken(TokenType type) => new Token(new SequenceRange(start, source.Position), type);
    }
}