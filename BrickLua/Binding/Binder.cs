using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

using BrickLua.CodeAnalysis.Symbols;
using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class Binder
{
    private BoundScope scope;

    private readonly Stack<LabelSymbol> breakLabelStack = [];
    private int loopCounter;

    private readonly DiagnosticBag diagnostics = new();

    private Binder(BoundScope? parentScope = null)
    {
        scope = new BoundScope(parentScope);
    }

    public static BoundChunk BindChunk(SyntaxTree chunk)
    {
        var binder = new Binder();
        binder.diagnostics.AddRange(chunk.Diagnostics);

        binder.scope.Declare(new LocalSymbol("_ENV"));
        var block = binder.BindBlock(chunk.Root.Body);
        return new BoundChunk(block, binder.diagnostics.ToImmutableArray());
    }

    BoundBlock BindBlock(BlockSyntax block, bool newScope = true)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        if (newScope)
        {
            scope = new BoundScope(scope);
        }

        foreach (var statement in block.Body)
        {
            if (statement is GotoStatementSyntax g)
            {
                var label = DeclareLabel(g.Label);
                scope.Declare(label);
            }
        }

        foreach (var statement in block.Body)
        {
            statements.Add(BindStatement(statement));
        }

        if (newScope)
        {
            scope = scope.Parent!;
        }

        return new(statements.DrainToImmutable());
    }

    BoundStatement BindStatement(StatementSyntax statement) => statement switch
    {
        AssignmentStatementSyntax a => BindAssignmentStatement(a),
        IfStatementSyntax i => BindIfStatement(i),
        WhileStatementSyntax w => BindWhileStatement(w),
        ForStatementSyntax f => BindForStatement(f),
        NumericalForStatementSyntax f => BindNumericalForStatement(f),
        BreakStatementSyntax b => BindBreakStatement(b),
        DoStatementSyntax d => BindDoStatement(d),
        FunctionStatementSyntax f => BindFunctionStatement(f),
        GotoStatementSyntax g => BindGotoStatement(g),
        LabelStatementSyntax l => BindLabelStatement(l),
        LocalDeclarationStatementSyntax l => BindLocalDeclarationStatement(l),
        LocalFunctionStatementSyntax l => BindLocalFunctionStatement(l),
        RepeatStatementSyntax r => BindRepeatStatement(r),
        ExpressionStatementSyntax e => BindExpressionStatement(e),
    };

    BoundStatement BindLabelStatement(LabelStatementSyntax l)
    {
        var label = LookupLabel(l.Name);
        return new BoundLabelStatement(label!);
    }

    BoundStatement BindGotoStatement(GotoStatementSyntax g)
    {
        var label = LookupLabel(g.Label);

        if (label is null)
        {
            diagnostics.ReportUndefinedLabel(g.Location, g.Label);
            return BindErrorStatement();
        }

        return new BoundGotoStatement(label);
    }


    private BoundStatement BindForStatement(ForStatementSyntax f)
    {
        scope = new BoundScope(scope);

        LocalSymbol? controlVariable = null;
        foreach (var name in f.NameList)
        {
            controlVariable ??= DeclareVariable(name);
        }

        var boundExpressions = ImmutableArray.CreateBuilder<BoundExpression>();
        foreach (var exp in f.ExpressionList)
        {
            boundExpressions.Add(BindExpression(exp));
        }

        var body = BindLoopBody(f.Body, out var breakLabel, newScope: false);
    
        scope = scope.Parent!;

        return new BoundForStatement(controlVariable!, boundExpressions.DrainToImmutable(), body, breakLabel);
    }

    private BoundStatement BindNumericalForStatement(NumericalForStatementSyntax f)
    {
        var initialValue = BindExpression(f.InitialValue);
        var limit = BindExpression(f.Limit);
        var step = f.Step is not null ? BindExpression(f.Step) : new BoundLiteralExpression(1);

        scope = new BoundScope(scope);

        var indexVariable = DeclareVariable(f.InitialValueIdentifier);

        var body = BindLoopBody(f.Body, out var breakLabel, newScope: false);
    
        scope = scope.Parent!;

        return new BoundNumericalForStatement(initialValue, limit, step, indexVariable, body, breakLabel);
    }


    BoundStatement BindIfStatement(IfStatementSyntax i)
    {
        var condition = BindExpression(i.Condition);
        var consequent = BindBlock(i.Consequent);
        var elseClause = i.ElseClause is null ? null : BindBlock(i.ElseClause.Body);

        var boundElseIfClauses = ImmutableArray.CreateBuilder<BoundElseIfClause>();
        foreach (var clause in i.ElseIfClauses)
        {
            boundElseIfClauses.Add(new BoundElseIfClause(
                BindExpression(clause.Condition),
                BindBlock(clause.Consequent)
            ));
        }

        return new BoundIfStatement(
            condition,
            consequent,
            boundElseIfClauses.DrainToImmutable(),
            elseClause
        );
    }

    BoundStatement BindRepeatStatement(RepeatStatementSyntax r)
    {
        var condition = BindExpression(r.Condition);

        var body = BindLoopBody(r.Body, out var breakLabel, newScope: true);
        return new BoundRepeatStatement(condition, body, breakLabel);
    }

    BoundStatement BindLocalDeclarationStatement(LocalDeclarationStatementSyntax local)
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

    BoundFunctionExpression BindFunctionBody(FunctionBody body)
    {
        scope = new BoundScope(scope, stopLabelSearch: true);

        foreach (var param in body.ParameterNames)
        {
            scope.Declare(new LocalSymbol(param.Value!.ToString()!));
        }

        var bodyBlock = BindBlock(body.Body, newScope: false);

        scope = scope.Parent!;

        return new BoundFunctionExpression(bodyBlock);
    }

    BoundStatement BindLocalFunctionStatement(LocalFunctionStatementSyntax l)
    {
        var local = DeclareVariable(l.Name);
        var body = BindFunctionBody(l.Body);
        return new BoundAssignmentStatement([new BoundNameExpression(local)], [body]);
    }

    BoundStatement BindFunctionStatement(FunctionStatementSyntax function)
    {
        var names = function.Name.DottedNames;
        if (function.Name.FieldName is not null)
        {
            names = names.Add(function.Name.FieldName);
        }

        VariableExpressionSyntax name = new NameExpressionSyntax(names[0]);
        foreach (var nameSegment in names.AsSpan(1..))
        {
            name = new DottedExpressionSyntax(name, nameSegment, default);
        }

        var functionVariable = BindVariableExpression(name);

        var body = BindFunctionBody(function.Body);
        return new BoundAssignmentStatement([functionVariable], [body]);
    }

    BoundStatement BindDoStatement(DoStatementSyntax @do)
    {
        return new BoundDoStatement(BindBlock(@do.Body));
    }

    BoundStatement BindBreakStatement(BreakStatementSyntax @break)
    {
        if (breakLabelStack.Count == 0)
        {
            diagnostics.ReportUnexpectedBreak(@break.Location);
            return BindErrorStatement();
        }

        var breakLabel = breakLabelStack.Peek();
        return new BoundGotoStatement(breakLabel);
    }


    BoundStatement BindWhileStatement(WhileStatementSyntax @while)
    {
        var condition = BindExpression(@while.Condition);
        var body = BindLoopBody(@while.Body, out var breakLabel, newScope: true);
    
        return new BoundWhileStatement(condition, body, breakLabel);
    }


    BoundBlock BindLoopBody(BlockSyntax body, out LabelSymbol breakLabel, bool newScope)
    {
        loopCounter++;
        breakLabel = new LabelSymbol($"break{loopCounter}");

        breakLabelStack.Push(breakLabel);
        var boundBody = BindBlock(body, newScope);
        breakLabelStack.Pop();

        return boundBody;
    }

    BoundStatement BindAssignmentStatement(AssignmentStatementSyntax a)
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

    BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax e)
        => new(BindExpression(e.Expression));

    BoundExpression BindExpression(ExpressionSyntax syntax) => syntax switch
    {
        ParenthesizedExpressionSyntax p => BindParenthesizedExpression(p),
        LiteralExpressionSyntax s => BindLiteralExpression(s),
        NameExpressionSyntax n => BindNameExpression(n),
        UnaryExpressionSyntax u => BindUnaryExpression(u),
        BinaryExpressionSyntax b => BindBinaryExpression(b),
        PrefixExpressionSyntax p => BindPrefixExpression(p),
        TableConstructorExpressionSyntax t => BindTableConstructorExpression(t),
        FunctionExpressionSyntax f => BindFunctionExpression(f),
        VarargExpressionSyntax => BindVarargExpression(),
        _ => throw new ArgumentException($"Unexpected syntax {syntax.GetType().Name}"),
    };

    BoundExpression BindVarargExpression()
    {
        return new BoundVarargExpression();
    }

    BoundExpression BindFunctionExpression(FunctionExpressionSyntax f)
    {
        return BindFunctionBody(f.Body);
    }

    BoundExpression BindTableConstructorExpression(TableConstructorExpressionSyntax t)
    {
        int implicitIndexCounter = 0;

        var boundAssignments = ImmutableArray.CreateBuilder<BoundFieldAssignment>();
        foreach (var assignment in t.FieldAssignments)
        {
            var key = assignment.Field switch
            {
                null => new BoundLiteralExpression(implicitIndexCounter++),
                SyntaxToken { Kind: SyntaxKind.Name, Value: string keyName } => new BoundLiteralExpression(keyName),
                ExpressionSyntax expression => BindExpression(expression)
            };

            var value = BindExpression(assignment.Value);

            boundAssignments.Add(new BoundFieldAssignment(key, value));
        }

        return new BoundTableConstructorExpression(boundAssignments.DrainToImmutable());
    }


    BoundExpression BindPrefixExpression(PrefixExpressionSyntax prefix) => prefix switch
    {
        ParenthesizedExpressionSyntax p => BindExpression(p.Expression),
        VariableExpressionSyntax v => BindVariableExpression(v),
        CallExpressionSyntax c => BindCallExpression(c),
        _ => throw new UnreachableException(),
    };

    BoundCallExpression BindCallExpression(CallExpressionSyntax call)
    {
        var receiver = BindPrefixExpression(call.Receiver);

        var args = ImmutableArray.CreateBuilder<BoundExpression>();
        foreach (var arg in call.Arguments)
        {
            args.Add(BindExpression(arg));
        }

        return new BoundCallExpression(receiver, args.DrainToImmutable());
    }

    BoundVariableExpression BindVariableExpression(VariableExpressionSyntax variable) => variable switch
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

    BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
    {
        return BindExpression(syntax.Expression);
    }

    BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var value = syntax.Value.Value ?? 0;
        return new BoundLiteralExpression(value);
    }

    BoundVariableExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        if (LookupVariable(syntax.Name) is LocalSymbol variable)
        {
            return new BoundNameExpression(variable);
        }

        var env = scope.Lookup("_ENV");
        return new BoundIndexExpression(new BoundNameExpression(env), new BoundLiteralExpression(syntax.Name));
    }

    BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);
        var boundOperator = BoundUnaryOperator.Bind(syntax.Operator);

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
    {
        var boundLeft = BindExpression(syntax.Left);
        var boundRight = BindExpression(syntax.Right);
        var boundOperator = BoundBinaryOperator.Bind(syntax.Operator);

        return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
    }

    BoundExpression BindErrorExpression()
    {
        return new BoundErrorExpression();
    }

    BoundStatement BindErrorStatement()
    {
        return new BoundExpressionStatement(BindErrorExpression());
    }

    LocalSymbol DeclareVariable(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;
        var local = new LocalSymbol(name);

        scope.Declare(local);
        return local;
    }

    LabelSymbol DeclareLabel(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;
        var local = new LabelSymbol(name);

        scope.Declare(local);
        return local;
    }


    LocalSymbol? LookupVariable(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;

        if (scope.TryLookup(name, out LocalSymbol symbol))
        {
            return symbol;
        }
        else 
        {
            return null;
        }
    }

    LabelSymbol? LookupLabel(SyntaxToken identifier)
    {
        var name = identifier.Value!.ToString()!;

        if (scope.TryLookup(name, out LabelSymbol symbol))
        {
            return symbol;
        }
        else 
        {
            return null;
        }
    }
}
