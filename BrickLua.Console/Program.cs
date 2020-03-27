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
using BrickLua.Syntax;

namespace BrickLua.Console
{
    using Console = System.Console;

    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var seq = new ReadOnlySequence<char>(Console.ReadLine().AsMemory());
                var lexer = new Lexer(new SequenceReader<char>(seq));
                while (lexer.Lex() is SyntaxToken t && t.Kind != SyntaxKind.EndOfFile)
                {
                    Console.WriteLine(@$"{t.Kind}: {seq.Slice(t.SourceRange.Start, t.SourceRange.End)} {t.Kind switch
                    {
                        SyntaxKind.IntegerConstant => t.IntegerData.ToString(),
                        SyntaxKind.FloatConstant => t.FloatData.ToString(),
                        _ => ""
                    }}");
                }
            }
        }
    }
}
