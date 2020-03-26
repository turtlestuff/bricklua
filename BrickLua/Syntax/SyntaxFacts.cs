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
    public static class SyntaxFacts
    {
        // TODO: Replace with https://github.com/dotnet/csharplang/issues/1881

        public static TokenType GetIdentifierKind(ReadOnlySpan<char> input) => input.Length switch
        {
            2 => input[0] switch
            {
                'd' => input[1] switch
                {
                    'o' => TokenType.Do,
                    _ => TokenType.Name
                },
                'i' => input[1] switch
                {
                    'f' => TokenType.If,
                    'n' => TokenType.In,
                    _ => TokenType.Name
                },
                'o' => input[1] switch
                {
                    'r' => TokenType.Or,
                    _ => TokenType.Name
                },
                _ => TokenType.Name,
            },
            3 => input[0] switch
            {
                'a' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        'd' => TokenType.And,
                        _ => TokenType.Name,
                    },
                    _ => TokenType.Name,
                },
                'e' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        'd' => TokenType.End,
                        _ => TokenType.Name,
                    },
                    _ => TokenType.Name,
                },
                'f' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        'r' => TokenType.For,
                        _ => TokenType.Name,
                    },
                    _ => TokenType.Name,
                },
                'n' => input[1] switch
                {
                    'i' => input[2] switch
                    {
                        'l' => TokenType.Nil,
                        _ => TokenType.Name,
                    },
                    'o' => input[2] switch
                    {
                        't' => TokenType.Not,
                        _ => TokenType.Name,
                    },
                    _ => TokenType.Name,
                },
                _ => TokenType.Name,
            },
            4 => input[0] switch
            {
                'e' => input[1] switch
                {
                    'l' => input[2] switch
                    {
                        's' => input[3] switch
                        {
                            'e' => TokenType.Else,
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                'g' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        't' => input[3] switch
                        {
                            'o' => TokenType.Goto,
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                't' => input[1] switch
                {
                    'h' => input[2] switch
                    {
                        'e' => input[3] switch
                        {
                            'n' => TokenType.Then,
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    'r' => input[2] switch
                    {
                        'u' => input[3] switch
                        {
                            'e' => TokenType.True,
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                _ => TokenType.Name
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
                                'k' => TokenType.Break,
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                'f' => input[1] switch
                {
                    'a' => input[2] switch
                    {
                        'l' => input[3] switch
                        {
                            's' => input[4] switch
                            {
                                'e' => TokenType.False,
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                'l' => input[1] switch
                {
                    'o' => input[2] switch
                    {
                        'c' => input[3] switch
                        {
                            'a' => input[4] switch
                            {
                                'l' => TokenType.Local,
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                'u' => input[1] switch
                {
                    'n' => input[2] switch
                    {
                        't' => input[3] switch
                        {
                            'i' => input[4] switch
                            {
                                'l' => TokenType.Until,
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                'w' => input[1] switch
                {
                    'h' => input[2] switch
                    {
                        'i' => input[3] switch
                        {
                            'l' => input[4] switch
                            {
                                'e' => TokenType.While,
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                _ => TokenType.Name
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
                                    'f' => TokenType.ElseIf,
                                    _ => TokenType.Name,
                                },
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
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
                                    't' => TokenType.Repeat,
                                    _ => TokenType.Name,
                                },
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        't' => input[3] switch
                        {
                            'u' => input[4] switch
                            {
                                'r' => input[5] switch
                                {
                                    'n' => TokenType.Return,
                                    _ => TokenType.Name,
                                },
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                _ => TokenType.Name,
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
                                            'n' => TokenType.Function,
                                            _ => TokenType.Name,
                                        },
                                        _ => TokenType.Name,
                                    },
                                    _ => TokenType.Name,
                                },
                                _ => TokenType.Name,
                            },
                            _ => TokenType.Name
                        },
                        _ => TokenType.Name
                    },
                    _ => TokenType.Name
                },
                _ => TokenType.Name,
            },
            _ => TokenType.Name
        };
    }
}