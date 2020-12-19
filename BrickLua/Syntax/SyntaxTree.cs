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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace BrickLua.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private delegate void ParseHandler(SyntaxTree syntaxTree,
                                           out ChunkSyntax root,
                                           out ImmutableArray<Diagnostic> diagnostics);

        private SyntaxTree(in ReadOnlySequence<char> text, ParseHandler handler)
        {
            Text = text;

            handler(this, out var root, out var diagnostics);

            Diagnostics = diagnostics;
            Root = root;
        }

        public ReadOnlySequence<char> Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ChunkSyntax Root { get; }

        private static void Parse(SyntaxTree syntaxTree, out ChunkSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            var parser = new Parser(new Lexer(new SequenceReader<char>(syntaxTree.Text)));
            root = parser.ParseChunk();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Load(string fileName) => Parse(new ReadOnlySequence<char>(File.ReadAllText(fileName).AsMemory()));

        public static SyntaxTree Parse(string text) => Parse(new ReadOnlySequence<char>(text.AsMemory()));

        public static SyntaxTree Parse(in ReadOnlySequence<char> text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(in ReadOnlySequence<char> text, out ImmutableArray<Diagnostic> diagnostics)
        {
            var tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree st, out ChunkSyntax root, out ImmutableArray<Diagnostic> d)
            {
                var l = new Lexer(new SequenceReader<char>(st.Text));
                while (true)
                {
                    var token = l.Lex();
                    if (token.Kind == SyntaxKind.EndOfFile)
                    {
                        root = new ChunkSyntax(new BlockSyntax(ImmutableArray<StatementSyntax>.Empty, null, default), default);
                        break;
                    }

                    tokens.Add(token);
                }

                d = l.Diagnostics.ToImmutableArray();
            }

            var syntaxTree = new SyntaxTree(text, ParseTokens);
            diagnostics = syntaxTree.Diagnostics;
            return tokens.ToImmutableArray();
        }
    }
}