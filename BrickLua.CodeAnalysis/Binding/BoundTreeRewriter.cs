namespace BrickLua.CodeAnalysis.Binding;

using System.Collections.Immutable;
using System.Diagnostics;

internal abstract class BoundTreeRewriter
{
    protected virtual BoundBlock RewriteBlock(BoundBlock block)
    {
        var rewrittenStmts = ImmutableArray.CreateBuilder<BoundStatement>(block.Statements.Length);

        foreach (var statement in block.Statements)
        {
            rewrittenStmts.Add(RewriteStatement(statement));
        }

        return new BoundBlock(rewrittenStmts.MoveToImmutable());
    }

    public virtual BoundStatement RewriteStatement(BoundStatement statement) 
        => statement switch
        {
            BoundExpressionStatement e => RewriteExpressionStatement(e),
            BoundAssignmentStatement a => RewriteAssignmentStatement(a),
            BoundIfStatement i => RewriteIfStatement(i),
            BoundWhileStatement w => RewriteWhileStatement(w),
            BoundForStatement f => RewriteForStatement(f),
            BoundNumericalForStatement f => RewriteNumericalForStatement(f),
            BoundGotoStatement g => RewriteGotoStatement(g),
            BoundConditionalGotoStatement g => RewriteConditionalGotoStatement(g),
            BoundLabelStatement l => RewriteLabelStatement(l),
            BoundDoStatement d => RewriteDoStatement(d),
            BoundRepeatStatement r => RewriteRepeatStatement(r),
            _ => throw new UnreachableException()
        };

    protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement e)
    {
        var expr = RewriteExpression(e.Expression);
        return new BoundExpressionStatement(expr);
    }

    protected virtual BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement a)
    {
        var rewrittenExprs = ImmutableArray.CreateBuilder<BoundExpression>();
        
        foreach (var expr in a.Expressions)
        {
            rewrittenExprs.Add(RewriteExpression(expr));
        }

        return new BoundAssignmentStatement(a.Variables, rewrittenExprs.DrainToImmutable());
    }

    protected virtual BoundStatement RewriteIfStatement(BoundIfStatement i)
    {
        var condition = RewriteExpression(i.Condition);
        var consequent = RewriteBlock(i.Consequent);

        var elseIfClauses = ImmutableArray.CreateBuilder<BoundElseIfClause>(i.ElseIfClauses.Length);
        foreach (var clause in i.ElseIfClauses)
        {
            elseIfClauses.Add(RewriteElseIfClause(clause));
        }

        var elseClause = i.ElseClause is not null ? RewriteBlock(i.ElseClause) : null;

        return new BoundIfStatement(condition, consequent, elseIfClauses.MoveToImmutable(), elseClause);
    }

    protected virtual BoundElseIfClause RewriteElseIfClause(BoundElseIfClause c)
    {
        var condition = RewriteExpression(c.Condition);
        var consequent = RewriteBlock(c.Consequent);
        return new BoundElseIfClause(condition, consequent);
    }

    protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement w)
    {
        var condition = RewriteExpression(w.Condition);
        var body = RewriteBlock(w.Body);

        return new BoundWhileStatement(condition, body, w.BreakLabel);
    }

    protected virtual BoundStatement RewriteForStatement(BoundForStatement f)
    {
        var body = RewriteBlock(f.Body);

        var expressions = ImmutableArray.CreateBuilder<BoundExpression>(f.ExpressionList.Length);
        foreach (var expr in f.ExpressionList)
        {
            expressions.Add(RewriteExpression(expr));
        }

        return new BoundForStatement(f.Variables, expressions.MoveToImmutable(), body, f.BreakLabel);
    }

    protected virtual BoundStatement RewriteNumericalForStatement(BoundNumericalForStatement f)
    {
        var initialValue = RewriteExpression(f.InitialValue);
        var limit = RewriteExpression(f.Limit);
        var step = RewriteExpression(f.Step);
        var body = RewriteBlock(f.Body);

        return new BoundNumericalForStatement(initialValue, limit, step, f.IndexVariable, body, f.BreakLabel);
    }

    protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement g)
    {
        return g;
    }
    protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement g)
    {
        return g;
    }

    protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement l)
    {
        return l;
    }

    protected virtual BoundStatement RewriteDoStatement(BoundDoStatement d)
    {
        var body = RewriteBlock(d.Body);
        return new BoundDoStatement(body);
    }

    protected virtual BoundStatement RewriteRepeatStatement(BoundRepeatStatement r)
    {
        var condition = RewriteExpression(r.Condition);
        var body = RewriteBlock(r.Body);
        return new BoundRepeatStatement(condition, body, r.BreakLabel);
    }

    public virtual BoundExpression RewriteExpression(BoundExpression expr)
        => expr switch 
        {
            BoundIndexExpression i => RewriteIndexExpression(i),
            BoundNameExpression n => RewriteNameExpression(n),
            BoundCallExpression c => RewriteCallExpression(c),
            BoundLiteralExpression l => RewriteLiteralExpression(l),
            BoundVarargExpression v => RewriteVarargExpression(v),
            BoundBinaryExpression b => RewriteBinaryExpression(b),
            BoundUnaryExpression u => RewriteUnaryExpression(u),
            BoundFunctionExpression f => RewriteFunctionExpression(f),
            BoundErrorExpression e => RewriteErrorExpression(e),
            _ => throw new UnreachableException()
        };

    protected virtual BoundExpression RewriteIndexExpression(BoundIndexExpression i)
    {
        var receiver = RewriteExpression(i.Receiver);
        var argument = RewriteExpression(i.IndexArgument);
        return new BoundIndexExpression(receiver, argument);
    }

    protected virtual BoundExpression RewriteNameExpression(BoundNameExpression n)
    {
        return n;
    }

    protected virtual BoundExpression RewriteCallExpression(BoundCallExpression c)
    {
        var rewrittenArgs = ImmutableArray.CreateBuilder<BoundExpression>(c.Arguments.Length);

        foreach (var expr in c.Arguments)
        {
            rewrittenArgs.Add(RewriteExpression(expr));
        }

        var receiver = RewriteExpression(c.Receiver);
        return new BoundCallExpression(receiver, rewrittenArgs.MoveToImmutable());
    }

    protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression l)
    {
        return l;
    }

    protected virtual BoundExpression RewriteVarargExpression(BoundVarargExpression v)
    {
        return v;
    }

    protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression b)
    {
        var left = RewriteExpression(b.Left);
        var right = RewriteExpression(b.Right);
        return new BoundBinaryExpression(left, b.Operator, right);
    }

    protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression u)
    {
        var arg = RewriteExpression(u.Operand);
        return new BoundUnaryExpression(u.Operator, arg);
    }

    protected virtual BoundExpression RewriteFunctionExpression(BoundFunctionExpression f)
    {
        var body = RewriteBlock(f.Body);
        return new BoundFunctionExpression(body);
    }

    protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression e)
    {
        return e;
    }
}