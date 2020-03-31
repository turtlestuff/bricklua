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
using System.Data;
using System.Runtime.InteropServices;

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
            this.current = peek ?? lexer.Lex();

            if (peek is { })
                peek = null;

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

        bool IsNot(SyntaxKind kind) => current.Kind != kind && current.Kind != SyntaxKind.EndOfFile;

        static bool StartsExpression(SyntaxToken token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.Name:
                case SyntaxKind.IntegerConstant:
                case SyntaxKind.FloatConstant:
                case SyntaxKind.StringLiteral:
                case SyntaxKind.Nil:
                case SyntaxKind.True:
                case SyntaxKind.False:
                case SyntaxKind.Minus:
                case SyntaxKind.Not:
                case SyntaxKind.Hash:
                case SyntaxKind.Tilde:
                case SyntaxKind.Function:
                case SyntaxKind.OpenParenthesis:
                case SyntaxKind.OpenBrace:
                case SyntaxKind.DotDotDot:
                    return true;
                default:
                    return false;
            }
        }

        SequenceRange From(SyntaxNode first, SyntaxNode last) => new SequenceRange(first.Location.Start, last.Location.End);

        public ChunkSyntax ParseFile()
        {
            var block = ParseBlock();
            return new ChunkSyntax(block, block.Location);
        }

        StatementSyntax? ParseStatement() => current.Kind switch
        {
            SyntaxKind.Semicolon => ParseStatement(),
            SyntaxKind.Break => ParseBreak(),
            SyntaxKind.Goto => ParseGoto(),
            SyntaxKind.Do => ParseDo(),
            SyntaxKind.While => ParseWhile(),
            SyntaxKind.Repeat => ParseRepeat(),
            SyntaxKind.If => ParseIf(),
            SyntaxKind.For => ParseFor(),
            /*SyntaxKind.Function => ParseFunction(),
            SyntaxKind.Local => ParseLocal(),*/
            SyntaxKind.ColonColon => ParseLabel(),
            _ => null,
        };

        BlockStatementSyntax ParseBlock()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            StatementSyntax? statement;
            while ((statement = ParseStatement()) != null)
            {
                statements.Add(statement);
            }

            ReturnStatementSyntax? @return = null;
            if (MaybeMatchToken(SyntaxKind.Return) is { } returnToken)
            {
                var returnValues = StartsExpression(current) ? ParseExpressionList() : ImmutableArray<ExpressionSyntax>.Empty;

                var semi = MaybeMatchToken(SyntaxKind.Semicolon);
                @return = new ReturnStatementSyntax(returnValues, From(returnToken, semi ?? GetLast(returnValues, returnToken)));
            }

            var statementArr = statements.ToImmutable();
            return new BlockStatementSyntax(statementArr, @return, new SequenceRange(
                statementArr.IsDefaultOrEmpty
                    ? @return is null
                        ? default
                        : @return.Location.Start
                    : statementArr[0].Location.Start,
                @return is null
                    ? statementArr.IsDefaultOrEmpty
                        ? default
                        : statementArr[^1].Location.End
                    : @return.Location.End));
        }

        static object @true = true;
        static object @false = false;

        ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = SyntaxFacts.UnaryOperatorPrecedence(current.Kind);
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(operatorToken.Kind, operand, From(operatorToken, operand));
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = SyntaxFacts.BinaryOperatorPrecedence(current.Kind);
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                var operatorToken = NextToken();
                var right = ParseExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken.Kind, right, From(left, right));
            }

            return left;
        }

        ExpressionSyntax ParsePrimaryExpression() => current.Kind switch
        {
            SyntaxKind.Nil => new LiteralExpressionSyntax(null, MatchToken(SyntaxKind.Nil).Location),
            SyntaxKind.True => new LiteralExpressionSyntax(@true, MatchToken(SyntaxKind.True).Location),
            SyntaxKind.False => new LiteralExpressionSyntax(@false, MatchToken(SyntaxKind.False).Location),
            SyntaxKind.IntegerConstant => new LiteralExpressionSyntax((long) current.Value!, MatchToken(SyntaxKind.IntegerConstant).Location),
            SyntaxKind.FloatConstant => new LiteralExpressionSyntax((double) current.Value!, MatchToken(SyntaxKind.FloatConstant).Location),
            SyntaxKind.StringLiteral => new LiteralExpressionSyntax((string) current.Value!, MatchToken(SyntaxKind.StringLiteral).Location),
            SyntaxKind.DotDotDot => new VarargExpressionSyntax(MatchToken(SyntaxKind.DotDotDot).Location),
            SyntaxKind.OpenBrace => ParseTableConstructor(),
            SyntaxKind.Function => ParseFunctionExpression(),
            _ => ParsePrefixExpression()
            //SyntaxKind.OpenParenthesis => ParsePrefixExpression(),
        };

        ExpressionSyntax ParsePrefixExpression()
        {
            return null;
        }

        ExpressionSyntax ParseNextExpression()
        {
            MatchToken(SyntaxKind.Semicolon);
            return ParseExpression();
        }

        FunctionExpressionSyntax ParseFunctionExpression()
        {
            var function = MatchToken(SyntaxKind.Function);
            var body = ParseFunctionBody();
            return new FunctionExpressionSyntax(body, From(function, body.Body));
        }

        TableConstructorExpressionSyntax ParseTableConstructor()
        {
            var statements = ImmutableArray.CreateBuilder<TableConstructorExpressionSyntax.FieldAssignmentExpressionSyntax>();
            var start = MatchToken(SyntaxKind.OpenBrace);
            while (IsNot(SyntaxKind.CloseBrace))
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
            bool isVararg;
            if (current.Kind != SyntaxKind.CloseParenthesis)
            {
                names = ParseParameterList(out isVararg);
            }
            else
            {
                names = ImmutableArray<SyntaxToken>.Empty;
                isVararg = false;
            }

            MatchToken(SyntaxKind.CloseParenthesis);

            var body = ParseBlock();
            MatchToken(SyntaxKind.End);
            return new FunctionBody(names, isVararg, body);
        }

        ImmutableArray<SyntaxToken> ParseParameterList(out bool isVarargs)
        {
            if (current.Kind == SyntaxKind.DotDot)
            {
                isVarargs = true;
                return ImmutableArray<SyntaxToken>.Empty;
            }

            var list = ParseNameList();
            if (current.Kind == SyntaxKind.Comma && Peek().Kind == SyntaxKind.DotDotDot)
            {
                NextToken();
                NextToken();
                isVarargs = true;
            }
            else
            {
                isVarargs = false;
            }

            return list;
        }

        ImmutableArray<SyntaxToken> ParseNameList()
        {
            var statements = ImmutableArray.CreateBuilder<SyntaxToken>();
            statements.Add(MatchToken(SyntaxKind.Name));
            while (current.Kind == SyntaxKind.Comma && Peek().Kind == SyntaxKind.Name)
            {
                NextToken();
                statements.Add(NextToken());
            }

            return statements.ToImmutable();
        }

        ImmutableArray<ExpressionSyntax> ParseExpressionList()
        {
            var statements = ImmutableArray.CreateBuilder<ExpressionSyntax>();
            statements.Add(ParseExpression());
            while (current.Kind == SyntaxKind.Comma)
            {
                statements.Add(ParseExpression());
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

        WhileStatementExpression ParseWhile()
        {
            var @while = MatchToken(SyntaxKind.While);
            var expression = ParseExpression();
            MatchToken(SyntaxKind.Do);
            var body = ParseBlock();
            var end = MatchToken(SyntaxKind.End);

            return new WhileStatementExpression(expression, body, From(@while, end));
        }

        RepeatStatementSyntax ParseRepeat()
        {
            var repeat = MatchToken(SyntaxKind.Repeat);
            var body = ParseBlock();
            MatchToken(SyntaxKind.Until);
            var expr = ParseExpression();

            return new RepeatStatementSyntax(body, expr, From(repeat, expr));
        }

        IfStatementSyntax ParseIf()
        {
            var @if = MatchToken(SyntaxKind.If);
            var expr = ParseExpression();
            var then = MatchToken(SyntaxKind.Then);
            var body = ParseBlock();

            var elseIfClauses = ImmutableArray.CreateBuilder<ElseIfClauseSyntax>();
            ElseClauseSyntax? elseClause = null;
            while (true)
            {
                switch (current.Kind)
                {
                    case SyntaxKind.ElseIf:
                        var elif = MatchToken(SyntaxKind.ElseIf);
                        var elifExpr = ParseExpression();
                        var elifThen = MatchToken(SyntaxKind.Then);
                        var elifBody = ParseBlock();
                        elseIfClauses.Add(new ElseIfClauseSyntax(elifExpr, elifBody, From(elif, GetLast(elifBody.Body, elifThen))));
                        break;
                    case SyntaxKind.Else:
                        var @else = MatchToken(SyntaxKind.If);
                        var elseBody = ParseBlock();
                        elseClause = new ElseClauseSyntax(body, From(@else, GetLast(body.Body, @else)));
                        break;
                    case SyntaxKind.End:
                        goto exit;
                    default:
                        goto exit;
                }

            }

            exit:

            var clauses = elseIfClauses.ToImmutable();
            return new IfStatementSyntax(expr, body, clauses, elseClause, From(@if,
                elseClause is { } ? elseClause : GetLast(clauses, GetLast(body.Body, then))));
        }

        StatementSyntax ParseFor()
        {
            var @for = MatchToken(SyntaxKind.For);
            switch (Peek().Kind)
            {
                case SyntaxKind.Equals:
                    var name = MatchToken(SyntaxKind.Name);
                    MatchToken(SyntaxKind.Equals);
                    var initial = ParseExpression();
                    MatchToken(SyntaxKind.Comma);
                    var limit = ParseExpression();
                    ExpressionSyntax? step = null;
                    if (MaybeMatchToken(SyntaxKind.Comma) is { })
                        step = ParseExpression();

                    MatchToken(SyntaxKind.Do);
                    var body = ParseBlock();
                    var end = MatchToken(SyntaxKind.End);

                    return new NumericalForStatementSyntax(name, initial, limit, step, body, From(@for, end));
                case SyntaxKind.Comma:
                case SyntaxKind.In:
                    var list = ParseNameList();
                    MatchToken(SyntaxKind.In);
                    var exprs = ParseExpressionList();
                    MatchToken(SyntaxKind.Do);
                    var inBody = ParseBlock();
                    var inEnd = MatchToken(SyntaxKind.End);

                    return new ForStatementSyntax(list, exprs, inBody, From(@for, inEnd));
                default:
                    return default!;
            }
        }


        SyntaxNode GetLast<TNode>(ImmutableArray<TNode> node, SyntaxNode last) where TNode : SyntaxNode => node.IsDefaultOrEmpty ? last : node[^1];

        LabelStatementSyntax ParseLabel()
        {
            var first = MatchToken(SyntaxKind.ColonColon);
            var name = MatchToken(SyntaxKind.Name);
            var last = MatchToken(SyntaxKind.ColonColon);

            return new LabelStatementSyntax(name, From(first, last));
        }
    }
}
