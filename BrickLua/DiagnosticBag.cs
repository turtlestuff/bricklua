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
using System.Collections;
using System.Collections.Generic;
using BrickLua.Syntax;

namespace BrickLua
{
    public sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        public DiagnosticBag(in ReadOnlySequence<char> text)
        {
            this.text = text;
        }

        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        public ref readonly ReadOnlySequence<char> Text => ref text;
        ReadOnlySequence<char> text;

        public IEnumerator<Diagnostic> GetEnumerator() => diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(DiagnosticBag diagnostics)
        {
            this.diagnostics.AddRange(diagnostics.diagnostics);
        }

        void Report(in SequenceRange location, string message)
        {
            var diagnostic = new Diagnostic(location, message);
            diagnostics.Add(diagnostic);
        }

        internal void ReportUnterminatedString(in SequenceRange location) => Report(location, "Unterminated string literal.");

        internal void ReportUnterminatedLongString(in SequenceRange location) => Report(location, "Unterminated long string literal.");

        internal void ReportUnterminatedLongComment(in SequenceRange location) => Report(location, "Unterminated long comment.");

        internal void ReportBadCharacter(in SequenceRange location, char character) => Report(location, $"Bad character input '{character.GetEofString()}'.");

        internal void ReportInvalidEscapeSequence(in SequenceRange location, char character) => Report(location, $"Invalid escape sequence '{character.GetEofString()}'.");

        internal void ReportUnterminatedEscapeSequence(in SequenceRange location) => Report(location, "Unterminated escape sequence.");

        internal void ReportIncompleteEscapeSequence(in SequenceRange location) => Report(location, "Incomplete escape sequence.");

        internal void ReportExpectedCharacter(in SequenceRange location, char actual, char expected) => Report(location, $"Unexpected character '{actual.GetEofString()}', expected '{actual.GetEofString()}'.");

        internal void ReportUnexpectedToken(in SequenceRange location, SyntaxKind expected, SyntaxKind actual) => Report(location, $"Unexpected token <{actual}>, expected <{expected}>.");
    }
}
