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
using System.Linq.Expressions;

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

        SyntaxToken MaybeMatchToken(SyntaxKind kind)
        {
            if (current.Kind == kind)
                return NextToken();

            return null;
        }


        SequenceRange From(SyntaxNode first, SyntaxNode last) => new SequenceRange(first.Location.Start, last.Location.End);

        public ChunkSyntax ParseFile()
        {
            var block = ParseBlock();
            return new ChunkSyntax(block, block.Location);
        }

        StatementSyntax ParseStatement() => current.Kind switch
        {
            SyntaxKind.Semicolon => ParseStatement(),
            SyntaxKind.Break => ParseBreak(),
            SyntaxKind.Goto => ParseGoto(),
            SyntaxKind.Do => ParseDo(),
            SyntaxKind.ColonColon => ParseLabel(),
            _ => default!,
        };

        BlockStatementSyntax ParseBlock()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            while (current.Kind != SyntaxKind.Return && current.Kind != SyntaxKind.EndOfFile)
            {
                statements.Add(ParseStatement());
            }

            ReturnStatementSyntax? syntax = null;
            if (current.Kind == SyntaxKind.Return)
            {
                var returnValues = ParseExpressionList();
                var semi = MaybeMatchToken(SyntaxKind.Semicolon);
                syntax = new ReturnStatementSyntax(returnValues, From(returnValues[0], semi ?? (SyntaxNode) returnValues[^1]));
            }

            return new BlockStatementSyntax(statements.ToImmutable(), syntax, From(statements[0], syntax ?? statements[^1]));
        }

        ImmutableArray<ExpressionSyntax> ParseExpressionList()
        {
            var statements = ImmutableArray.CreateBuilder<ExpressionSyntax>();
            do
            {
                statements.Add(ParseExpression());
            } while (Peek().Kind == SyntaxKind.Comma);

            return statements.ToImmutable();
        }

        ExpressionSyntax ParseExpression()
        {
            return default;
        }

        BreakStatementSyntax ParseBreak()
        {
            var @break = MatchToken(SyntaxKind.Break);
            return new BreakStatementSyntax(@break.Location);
        }

        GotoStatementSyntax ParseGoto()
        {
            var @goto = MatchToken(SyntaxKind.Goto);
            var name = MatchToken(SyntaxKind.Name);

            return new GotoStatementSyntax(name, From(@goto, name));
        }

        DoStatementSyntax ParseDo()
        {
            var @do = MatchToken(SyntaxKind.Do);
            var block = ParseBlock();
            var end = MatchToken(SyntaxKind.End);

            return new DoStatementSyntax(block, From(@do, end));
        }

        LabelStatementSyntax ParseLabel()
        {
            var first = MatchToken(SyntaxKind.ColonColon);
            var name = MatchToken(SyntaxKind.Name);
            var last = MatchToken(SyntaxKind.ColonColon);

            return new LabelStatementSyntax(name, From(first, last));
        }
    }
}
