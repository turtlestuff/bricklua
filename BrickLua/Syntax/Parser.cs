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

using System.Collections.Immutable;

namespace BrickLua.Syntax
{
    public ref struct Parser
    {
        SyntaxToken current;
        SyntaxToken? peek;

        Lexer lexer;

        public Parser(in Lexer lexer)
        {
            this.lexer = lexer;
            current = lexer.Lex();
            peek = null;
        }

        SyntaxToken NextToken()
        {
            var current = peek ?? this.current;
            lexer.Lex();
            if (peek is { }) peek = null;
            return current;
        }

        SyntaxToken Peek()
        {
            peek ??= lexer.Lex();
            return peek;
        }

        SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (current.Kind == kind)
                return NextToken();

            // TODO: Diagnostics
            return new SyntaxToken(kind, default);
        }

        SequenceRange From(SyntaxToken first, SyntaxToken last) => new SequenceRange(first.Location.Start, last.Location.End);


        public ChunkSyntax ParseFile()
        {
            var members = ImmutableArray.CreateBuilder<StatementSyntax>();
            while (current.Kind != SyntaxKind.EndOfFile)
            {
                members.Add(ParseStatement());
            }

            var pos = new SequenceRange(lexer.Reader.Sequence.GetPosition(0), lexer.Reader.Position);
            return new ChunkSyntax(new BlockStatementSyntax(members.ToImmutable(), null, pos), pos);
        }

        StatementSyntax ParseStatement()
        {
            @continue:
            switch (current.Kind)
            {
                case SyntaxKind.Semicolon:
                    // Empty statement, don't bother
                    goto @continue;
            }

            return default;
        }
    }
}
