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
            current = this.lexer.Lex();
            peek = null;
        }

        SyntaxToken NextToken()
        {
            var current = this.current;
            if (peek is null)
            {
                var lex = lexer.Lex();
                this.current = lex;
            }
            else
            {
                this.current = peek;
            }
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


        SyntaxToken? MaybeMatchToken(SyntaxKind kind)
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

        static object @true = true;
        static object @false = false;

        ExpressionSyntax ParseExpression()
        {
            return current.Kind switch
            {
                SyntaxKind.Semicolon => ParseNextExpression(),
                SyntaxKind.Nil => new LiteralExpressionSyntax(null, MatchToken(SyntaxKind.Nil).Location),
                SyntaxKind.True => new LiteralExpressionSyntax(@true, MatchToken(SyntaxKind.True).Location),
                SyntaxKind.False => new LiteralExpressionSyntax(@false, MatchToken(SyntaxKind.False).Location),
                SyntaxKind.IntegerConstant => new LiteralExpressionSyntax((long) current.Value!, MatchToken(SyntaxKind.IntegerConstant).Location),
                SyntaxKind.FloatConstant => new LiteralExpressionSyntax((double) current.Value!, MatchToken(SyntaxKind.FloatConstant).Location),
                SyntaxKind.StringLiteral => new LiteralExpressionSyntax((string) current.Value!, MatchToken(SyntaxKind.StringLiteral).Location),
                SyntaxKind.DotDotDot => new VarargExpressionSyntax(MatchToken(SyntaxKind.DotDotDot).Location),
                SyntaxKind.OpenBrace => ParseTableConstructor(),
                SyntaxKind.Function => ParseFunctionExpression(),
                _ => null!,
            };
        }

        ExpressionSyntax ParseNextExpression()
        {
            MatchToken(SyntaxKind.Semicolon);
            return ParseExpression();
        }

        FunctionExpressionSyntax ParseFunctionExpression()
        {
            var current = this.current;
            var body = ParseFunctionBody();
            return new FunctionExpressionSyntax(body, From(current, body.Body.Body[^1]));
        }

        TableConstructorExpressionSyntax ParseTableConstructor()
        {
            var statements = ImmutableArray.CreateBuilder<TableConstructorExpressionSyntax.FieldAssignmentExpressionSyntax>();
            var start = MatchToken(SyntaxKind.OpenBrace);
            while (current.Kind != SyntaxKind.CloseBrace && current.Kind != SyntaxKind.EndOfFile)
            {
                SyntaxNode target;
                ExpressionSyntax? value = null;
                switch (current.Kind)
                {
                    case SyntaxKind.OpenBracket:
                        MatchToken(SyntaxKind.OpenBracket);
                        target = ParseExpression();
                        MatchToken(SyntaxKind.CloseBracket);
                        MatchToken(SyntaxKind.Equals);
                        value = ParseExpression();
                        break;
                    case SyntaxKind.Name when Peek().Kind == SyntaxKind.Equals:
                        target = MatchToken(SyntaxKind.Name);
                        MatchToken(SyntaxKind.Equals);
                        value = ParseExpression();
                        break;
                    default:
                        target = ParseExpression();
                        break;
                }

                statements.Add(new TableConstructorExpressionSyntax.FieldAssignmentExpressionSyntax(target, value, From(target, value ?? target)));

                if (current.Kind == SyntaxKind.Semicolon || current.Kind == SyntaxKind.Comma)
                {
                    NextToken();
                }
            }

            var end = MatchToken(SyntaxKind.CloseBrace);

            return new TableConstructorExpressionSyntax(statements.ToImmutable(), From(start, end));
        }

        FunctionBody ParseFunctionBody()
        {
            MatchToken(SyntaxKind.OpenParenthesis);
            ImmutableArray<SyntaxToken> names;
            bool isVararg = false;
            if (current.Kind == SyntaxKind.DotDotDot)
            {
                names = ImmutableArray<SyntaxToken>.Empty;
                isVararg = true;
            }
            else
            {
                names = ParseNameList();
                if (current.Kind == SyntaxKind.Comma)
                {
                    MatchToken(SyntaxKind.DotDotDot);
                    isVararg = true;
                }
            }

            return new FunctionBody(names, isVararg, ParseBlock());
        }

        ImmutableArray<SyntaxToken> ParseNameList()
        {
            var statements = ImmutableArray.CreateBuilder<SyntaxToken>();
            statements.Add(MatchToken(SyntaxKind.Name));
            while (current.Kind == SyntaxKind.Comma)
            {
                MatchToken(SyntaxKind.Comma);
                statements.Add(MatchToken(SyntaxKind.Name));
            }
            return statements.ToImmutable();
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
