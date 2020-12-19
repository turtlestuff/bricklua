//
//  Copyright (C) 2020 John Tur
//  
//  This file is part of BrickLua, a simple Lua 5.4 CIL compiler.
//  
//  BrickLua is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//  
//  BrickLua is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY, without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//  
//  You should have received a copy of the GNU Lesser General Public License
//  along with BrickLua.  If not, see <https://www.gnu.org/licenses/>.
//

namespace BrickLua.CodeAnalysis.Syntax
{
    public enum SyntaxKind : byte
    {
        EndOfFile,
        BadToken,

        Name,
        LiteralString,
        IntegerConstant,
        FloatConstant,

        // Keywords
        And,
        Break,
        Do,
        Else,
        ElseIf,
        End,
        False,
        For,
        Function,
        Goto,
        If,
        In,
        Local,
        Nil,
        Not,
        Or,
        Repeat,
        Return,
        Then,
        True,
        Until,
        While,

        // Operators
        Plus,
        Minus,
        Asterisk,
        Slash,
        Percent,
        Caret,
        Hash,
        Ampersand,
        Tilde,
        Pipe,
        LessLess,
        GreaterGreater,
        SlashSlash,
        EqualsEquals,
        TildeEquals,
        LessEquals,
        GreaterEquals,
        Less,
        Greater,
        Equals,
        OpenParenthesis,
        CloseParenthesis,
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        ColonColon,
        Semicolon,
        Colon,
        Comma,
        Dot,
        DotDot,
        DotDotDot,
    }
}
