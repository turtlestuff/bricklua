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
using System.Globalization;

namespace BrickLua.Syntax
{
    public static class SyntaxFacts
    {
        public static bool IsRightAssociative(SyntaxKind kind) => kind == SyntaxKind.Caret || kind == SyntaxKind.DotDot;

        public static int UnaryOperatorPrecedence(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.Caret:
                    return 12;
                case SyntaxKind.Not:
                case SyntaxKind.Hash:
                case SyntaxKind.Minus:
                case SyntaxKind.Tilde:
                    return 11;

                default:
                    return 0;
            }
        }

        public static int BinaryOperatorPrecedence(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.Caret:
                    return 11;

                case SyntaxKind.Asterisk:
                case SyntaxKind.Slash:
                case SyntaxKind.SlashSlash:
                case SyntaxKind.Percent:
                    return 10;

                case SyntaxKind.Plus:
                case SyntaxKind.Minus:
                    return 9;

                case SyntaxKind.DotDot:
                    return 8;

                case SyntaxKind.GreaterGreater:
                case SyntaxKind.LessLess:
                    return 7;

                case SyntaxKind.Ampersand:
                    return 6;

                case SyntaxKind.Tilde:
                    return 5;

                case SyntaxKind.Pipe:
                    return 4;

                case SyntaxKind.Less:
                case SyntaxKind.Greater:
                case SyntaxKind.LessEquals:
                case SyntaxKind.GreaterEquals:
                case SyntaxKind.TildeEquals:
                case SyntaxKind.EqualsEquals:
                    return 3;

                case SyntaxKind.And:
                    return 2;

                case SyntaxKind.Or:
                    return 1;

                default:
                    return 0;
            }
        }

        public static string GetEofString(this char c) => c == default ? "<eof>" : c.ToString(CultureInfo.InvariantCulture);

        // TODO: Replace with https://github.com/dotnet/csharplang/issues/1881

        public static SyntaxKind GetIdentifierKind(ReadOnlySpan<char> input) => input.Length switch
        {
            2 => input[0] switch
            {
                'd' => input[1] switch
                {
                    'o' => SyntaxKind.Do,
                    _ => SyntaxKind.Name
                },
                'i' => input[1] switch
                {
                    'f' => SyntaxKind.If,
                    'n' => SyntaxKind.In,
                    _ => SyntaxKind.Name
                },
                'o' => input[1] switch
                {
                    'r' => SyntaxKind.Or,
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name,
            },
            3 => input[0] switch
            {
                'a' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        'd' => SyntaxKind.And,
                        _ => SyntaxKind.Name,
                    },
                    _ => SyntaxKind.Name,
                },
                'e' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        'd' => SyntaxKind.End,
                        _ => SyntaxKind.Name,
                    },
                    _ => SyntaxKind.Name,
                },
                'f' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        'r' => SyntaxKind.For,
                        _ => SyntaxKind.Name,
                    },
                    _ => SyntaxKind.Name,
                },
                'n' => input[1] switch
                {
                    'i' => input[2] switch
                    {
                        'l' => SyntaxKind.Nil,
                        _ => SyntaxKind.Name,
                    },
                    'o' => input[2] switch
                    {
                        't' => SyntaxKind.Not,
                        _ => SyntaxKind.Name,
                    },
                    _ => SyntaxKind.Name,
                },
                _ => SyntaxKind.Name,
            },
            4 => input[0] switch
            {
                'e' => input[1] switch
                {
                    'l' => input[2] switch
                    {
                        's' => input[3] switch
                        {
                            'e' => SyntaxKind.Else,
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'g' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        't' => input[3] switch
                        {
                            'o' => SyntaxKind.Goto,
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                't' => input[1] switch
                {
                    'h' => input[2] switch
                    {
                        'e' => input[3] switch
                        {
                            'n' => SyntaxKind.Then,
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    'r' => input[2] switch
                    {
                        'u' => input[3] switch
                        {
                            'e' => SyntaxKind.True,
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            5 => input[0] switch
            {
                'b' => input[1] switch
                {
                    'r' => input[2] switch
                    {
                        'e' => input[3] switch
                        {
                            'a' => input[4] switch
                            {
                                'k' => SyntaxKind.Break,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'f' => input[1] switch
                {
                    'a' => input[2] switch
                    {
                        'l' => input[3] switch
                        {
                            's' => input[4] switch
                            {
                                'e' => SyntaxKind.False,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'l' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        'c' => input[3] switch
                        {
                            'a' => input[4] switch
                            {
                                'l' => SyntaxKind.Local,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'u' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        't' => input[3] switch
                        {
                            'i' => input[4] switch
                            {
                                'l' => SyntaxKind.Until,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'w' => input[1] switch
                {
                    'h' => input[2] switch
                    {
                        'i' => input[3] switch
                        {
                            'l' => input[4] switch
                            {
                                'e' => SyntaxKind.While,
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name
            },
            6 => input[0] switch
            {
                'e' => input[1] switch
                {
                    'l' => input[2] switch
                    {
                        's' => input[3] switch
                        {
                            'e' => input[4] switch
                            {
                                'i' => input[5] switch
                                {
                                    'f' => SyntaxKind.ElseIf,
                                    _ => SyntaxKind.Name,
                                },
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                'r' => input[1] switch
                {
                    'e' => input[2] switch
                    {
                        'p' => input[3] switch
                        {
                            'e' => input[4] switch
                            {
                                'a' => input[5] switch
                                {
                                    't' => SyntaxKind.Repeat,
                                    _ => SyntaxKind.Name,
                                },
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        't' => input[3] switch
                        {
                            'u' => input[4] switch
                            {
                                'r' => input[5] switch
                                {
                                    'n' => SyntaxKind.Return,
                                    _ => SyntaxKind.Name,
                                },
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name,
            },
            8 => input[0] switch
            {
                'f' => input[1] switch
                {
                    'u' => input[2] switch
                    {
                        'n' => input[3] switch
                        {
                            'c' => input[4] switch
                            {
                                't' => input[5] switch
                                {
                                    'i' => input[6] switch
                                    {
                                        'o' => input[7] switch
                                        {
                                            'n' => SyntaxKind.Function,
                                            _ => SyntaxKind.Name,
                                        },
                                        _ => SyntaxKind.Name,
                                    },
                                    _ => SyntaxKind.Name,
                                },
                                _ => SyntaxKind.Name,
                            },
                            _ => SyntaxKind.Name
                        },
                        _ => SyntaxKind.Name
                    },
                    _ => SyntaxKind.Name
                },
                _ => SyntaxKind.Name,
            },
            _ => SyntaxKind.Name
        };
    }
}