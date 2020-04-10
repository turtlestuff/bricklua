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
                var parser = new Parser(new Lexer(new SequenceReader<char>(seq)));
                parser.ParseChunk().WriteTo(Console.Out);

                foreach (var diag in parser.Diagnostics)
                {

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{diag.Message} ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(parser.Diagnostics.Text.Slice(diag.Location.Start, diag.Location.End).ToString());
                    Console.ResetColor();

                    var text = parser.Diagnostics.Text;
                    var location = diag.Location;
                    var index = text.Slice(0, diag.Location.Start).Length;
                    var reader = new SequenceReader<char>(parser.Diagnostics.Text);

                    ReadOnlySequence<char> line = reader.Sequence;
                    SequencePosition startPos = text.Start;
                    while (reader.TryReadTo(sequence: out var sequence, '\n'))
                    {
                        if (reader.Consumed - 1 > index)
                        {
                            line = sequence;
                            break;
                        }

                        startPos = reader.Position;
                    }

                    Console.WriteLine(line.ToString());

                    var underlineLength = text.Slice(location.Start, location.End).Length;
                    var padLength = text.Slice(startPos, location.Start).Length + underlineLength;
                    Console.WriteLine($"{new string('~', (int) underlineLength).PadLeft((int) padLength)}");
                }
            }
        }
    }
}