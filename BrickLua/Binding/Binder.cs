using System.Collections.Immutable;
using System.Diagnostics;

using BrickLua.CodeAnalysis.Symbols;
using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class Binder
{
    private BoundScope scope;

    private Stack<BoundLabel> breakLabelStack = [];
    private int loopCounter;

    private readonly DiagnosticBag diagnostics = new();

    private Binder(BoundScope? parentScope)
    {
        scope = new BoundScope(parentScope);
    }

    public static BoundChunk BindChunk(SyntaxTree chunk)
    {
        var binder = new Binder(null);
        binder.diagnostics.AddRange(chunk.Diagnostics);

        var block = binder.BindBlock(chunk.Root.Body);
        return new BoundChunk(block, binder.diagnostics.ToImmutableArray());
    }

    private BoundBlock BindBlock(BlockSyntax block)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        scope = new BoundScope(scope);

        foreach (var statement in block.Body)
        {
            statements.Add(BindStatement(statement));
        }

        scope = scope.Parent!;

        return new(statements.DrainToImmutable());
    }

    private BoundStatement BindStatement(StatementSyntax statement) => statement switch
    {
        AssignmentStatementSyntax a => BindAssignmentStatement(a),
        WhileStatementExpression w => BindWhileStatement(w),
        BreakStatementSyntax b => BindBreakStatement(b),
        DoStatementSyntax d => BindDoStatement(d),
        FunctionStatementSyntax f => BindFunctionStatement(f),
        // GotoStatementSyntax g => BindGotoStatement(g),
        // LabelStatementSyntax l => BindLabelStatement(l),
        LocalDeclarationStatementSyntax l => BindLocalDeclarationStatement(l),
        LocalFunctionStatementSyntax l => BindLocalFunctionStatement(l),
        // RepeatStatementSyntax r => BindRepeatStatement(r),
        ExpressionStatementSyntax e => BindExpressionStatement(e),
    };

    private BoundStatement BindLocalDeclarationStatement(LocalDeclarationStatementSyntax local)
    {
        var variables = ImmutableArray.CreateBuilder<BoundVariableExpression>();
        foreach (var declaration in local.Declarations)
        {
            var localVar = DeclareVariable(declaration.Name);
            variables.Add(new BoundNameExpression(localVar));
        }

        var expressions = ImmutableArray.CreateBuilder<BoundExpression>();
        foreach (var var in local.Expressions)
        {
            expressions.Add(BindExpression(var));
        }

        return new BoundAssignmentStatement(variables.DrainToImmutable(), expressions.DrainToImmutable());
    }

    private BoundExpression BindFunctionBody(FunctionBody body)
    {
        scope = new BoundScope(scope);

        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        foreach (var param in body.ParameterNames)
        {
            scope.TryDeclare(new LocalSymbol(param.Value!.ToString()!));
        }

        var bodyBlock = BindBlock(body.Body);

        scope = scope.Parent!;

        return new BoundFunctionExpression(bodyBlock);
    }

    private BoundStatement BindLocalFunctionStatement(LocalFunctionStatementSyntax l)
    {
        var local = DeclareVariable(l.Name);
        var body = BindFunctionBody(l.Body);
        return new BoundAssignmentStatement([new BoundNameExpression(local)], [body]);
    }


    private BoundStatement BindFunctionStatement(FunctionStatementSyntax function)
    {
        var name = BindFunctionName(function.Name);
        var body = BindFunctionBody(function.Body);
        return new BoundAssignmentStatement([name], [body]);
    }

    private BoundVariableExpression BindFunctionName(FunctionName functionName)
    {
        var names = functionName.DottedNames;
        if (functionName.FieldName is not null)
        {
            names = names.Add(functionName.FieldName);
        }

        VariableExpressionSyntax name = new NameExpressionSyntax(names[0]);
        foreach (var nameSegment in names.AsSpan(1..))
        {
            name = new DottedExpressionSyntax(name, nameSegment, default);
        }

        return BindVariableExpression(name);
    }

    private BoundStatement BindDoStatement(DoStatementSyntax @do)
    {
        return new BoundDoStatement(BindBlock(@do.Body));
    }

    private BoundStatement BindBreakStatement(BreakStatementSyntax @break)
    {
        if (breakLabelStack.Count == 0)
        {
            return new BoundExpressionStatement(new BoundErrorExpression());
        }

        var breakLabel = breakLabelStack.Pop();
        return new BoundGotoStatement(breakLabel);
    }


    private BoundWhileStatement BindWhileStatement(WhileStatementExpression @while)
    {
        var condition = BindExpression(@while.Condition);
        var body = BindLoopBody(@while.Body, out var breakLabel);
    
        return new BoundWhileStatement(condition, body, breakLabel);
    }


    private BoundBlock BindLoopBody(BlockSyntax body, out BoundLabel breakLabel)
    {
        loopCounter++;
        breakLabel = new BoundLabel($"break{loopCounter}");

        breakLabelStack.Push(breakLabel);
        var boundBody = BindBlock(body);
        breakLabelStack.Pop();

        return boundBody;
    }

    private BoundAssignmentStatement BindAssignmentStatement(AssignmentStatementSyntax a)
    {
        var variables = ImmutableArray.CreateBuilder<BoundVariableExpression>();
        foreach (var var in a.Variables)
        {
            variables.Add(BindVariableExpression(var));
        }

        var values = ImmutableArray.CreateBuilder<BoundExpression>();
        foreach (var val in a.Values)
        {
            values.Add(BindExpression(val));
        }

        return new BoundAssignmentStatement(variables.DrainToImmutable(), values.DrainToImmutable());
    }

    private BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax e)
        => new(BindExpression(e.Expression));

    public BoundExpression BindExpression(ExpressionSyntax syntax) => syntax switch
    {
        ParenthesizedExpressionSyntax p => BindParenthesizedExpression(p),
        LiteralExpressionSyntax s => BindLiteralExpression(s),
        NameExpressionSyntax n => BindNameExpression(n),
        UnaryExpressionSyntax u => BindUnaryExpression(u),
        BinaryExpressionSyntax b => BindBinaryExpression(b),
        PrefixExpressionSyntax p => BindPrefixExpression(p),
        _ => throw new Exception($"Unexpected syntax {syntax.GetType().Name}"),
    };

    private BoundExpression BindPrefixExpression(PrefixExpressionSyntax prefix) => prefix switch
    {
        ParenthesizedExpressionSyntax p => BindExpression(p.Expression),
        VariableExpressionSyntax v => BindVariableExpression(v),
        CallExpressionSyntax c => BindCallExpression(c),
        _ => throw new UnreachableException(),
    };

    private BoundCallExpression BindCallExpression(CallExpressionSyntax call)
    {
        var receiver = BindPrefixExpression(call.Receiver);

        var args = ImmutableArray.CreateBuilder<BoundExpression>();
        foreach (var arg in call.Arguments)
        {
            args.Add(BindExpression(arg));
        }

        return new BoundCallExpression(receiver, args.DrainToImmutable());
    }

    private BoundVariableExpression BindVariableExpression(VariableExpressionSyntax variable) => variable switch
    {
        DottedExpressionSyntax d 
            => new BoundIndexExpression(
                    BindExpression(d.Receiver),
                    new BoundLiteralExpression(d.Name.Value!)),
        IndexExpressionSyntax i 
            => new BoundIndexExpression(
                    BindExpression(i.Receiver),
                    BindExpression(i.IndexArgument)),
        NameExpressionSyntax n => BindNameExpression(n),
        _ => throw new UnreachableException(),
    };

    private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
    {
        return BindExpression(syntax.Expression);
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var value = syntax.Value.Value ?? 0;
        return new BoundLiteralExpression(value);
    }

    private BoundVariableExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        if (BindVariableReference(syntax.Name) is LocalSymbol variable)
        {
            return new BoundNameExpression(variable);
        }

        var env = scope.Lookup("_ENV");
        return new BoundIndexExpression(new BoundNameExpression(env), new BoundLiteralExpression(syntax.Name));
    }

    private LocalSymbol DeclareVariable(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;
        var local = new LocalSymbol(name);

        if (scope.TryDeclare(local))
        {
            return local;
        }
        else
        {
            // Diagnostic?
            return local;
        }
    }

    private LocalSymbol? BindVariableReference(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;

        if (scope.TryLookup(name, out var symbol))
        {
            return symbol;
        }
        else 
        {
            // Diagnostic
            return null;
        }
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);
        var boundOperator = BoundUnaryOperator.Bind(syntax.Operator);

        if (boundOperator is null)
        {
            // Diagnostic
            return boundOperand;
        }

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
    {
        var boundLeft = BindExpression(syntax.Left);
        var boundRight = BindExpression(syntax.Right);
        var boundOperator = BoundBinaryOperator.Bind(syntax.Operator);

        if (boundOperator is null)
        {
            // Diagnostic
            return boundLeft;
        }

        return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
    }
}
