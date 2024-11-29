namespace BrickLua.CodeAnalysis.Lowering;

using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Binding;
using BrickLua.CodeAnalysis.Symbols;

using static BrickLua.CodeAnalysis.Binding.BoundNodeFactory;

internal sealed class Lowerer : BoundTreeRewriter
{
    public static BoundStatement Lower(BoundStatement statement)
    {
        var lowerer = new Lowerer();
        var result = lowerer.RewriteStatement(statement);
        return result;
    }

    private int labelCount;

    private LabelSymbol GenerateLabel()
    {
        return new LabelSymbol($"{labelCount++}");
    }

    private BoundStatement Statements(ImmutableArray<BoundStatement>.Builder statements)
        => RewriteStatement(Do(Block(statements.DrainToImmutable())));

    protected override BoundStatement RewriteIfStatement(BoundIfStatement i)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        var end = GenerateLabel();

        void AddBranch(BoundExpression condition, BoundBlock consequent)
        {
            var next = GenerateLabel();
            statements.Add(GotoFalse(next, condition));
            statements.AddRange(consequent.Statements);
            statements.Add(Goto(end));
            statements.Add(Label(next));
        }

        AddBranch(i.Condition, i.Consequent);

        foreach (var elif in i.ElseIfClauses)
        {
            AddBranch(elif.Condition, elif.Consequent);
        }

        if (i.ElseClause is not null)
        {
            statements.AddRange(i.ElseClause.Statements);
        }

        return Statements(statements);
    }

    protected override BoundStatement RewriteWhileStatement(BoundWhileStatement w)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        var check = GenerateLabel();
        var end = w.BreakLabel;

        statements.Add(Label(check));
        statements.Add(GotoFalse(end, w.Condition));
        statements.AddRange(w.Body.Statements);
        statements.Add(Goto(check));
        statements.Add(Label(end));

        return Statements(statements);
    }

    protected override BoundStatement RewriteRepeatStatement(BoundRepeatStatement r)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        var start = GenerateLabel();
        var end = r.BreakLabel;

        statements.Add(Label(start));
        statements.AddRange(r.Body.Statements);
        statements.Add(GotoFalse(start, r.Condition));
        statements.Add(Label(end));

        return Statements(statements);
    }

    protected override BoundStatement RewriteForStatement(BoundForStatement f)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        var iterator = new LocalSymbol($"_iterator{labelCount}");
        var state = new LocalSymbol($"_state{labelCount}");
        var closingValue = new LocalSymbol($"_close{labelCount}");

        var controlVar = f.Variables[0];

        statements.Add(Assignment(
            [Name(iterator), Name(state), controlVar, Name(closingValue)],
            f.ExpressionList
        ));

        var start = GenerateLabel();
        var end = f.BreakLabel;

        statements.Add(Label(start));

        var call = Call(Name(iterator), [Name(state), controlVar]);
        statements.Add(Assignment(
            ImmutableArray<BoundVariableExpression>.CastUp(f.Variables),
            [call]
        ));

        var cond = EqualTo(controlVar, Literal(null));
        statements.Add(GotoTrue(end, cond));

        statements.AddRange(f.Body.Statements);

        statements.Add(Goto(start));
        statements.Add(Label(end));

        return Statements(statements);
    }

    protected override BoundStatement RewriteNumericalForStatement(BoundNumericalForStatement f)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        var idx = f.IndexVariable;
        var limit = new LocalSymbol($"_limit{labelCount}");
        var step = new LocalSymbol($"_step{labelCount}");

        // TODO: If both the initial value and the step are integers, the loop is done with integers; note that the limit may not be an integer.
        // Otherwise, the three values are converted to floats and the loop is done with floats. Beware of floating-point accuracy in this case.

        statements.Add(
            Assignment(
                [Name(idx), Name(limit), Name(step)],
                Expressions: [f.InitialValue, f.Limit, f.Step]
            )
        );

        var start = GenerateLabel();
        var end = f.BreakLabel;

        statements.Add(Label(start));

        // (step > 0 and i <= limit) or (step <= 0 and i >= limit)
        var zero = Literal(0);
        var stepGtZero = GreaterThan(Name(step), zero);
        var stepLeZero = LessThanOrEqualTo(Name(step), zero);
        var iLeLimit = GreaterThan(Name(idx), Name(limit));
        var iGeLimit = GreaterThan(Name(idx), Name(limit));

        var cond = LogicalOr(
            LogicalAnd(stepGtZero, iLeLimit),
            LogicalAnd(stepLeZero, iGeLimit)
        );

        statements.Add(GotoFalse(end, cond));

        statements.AddRange(f.Body.Statements);

        var addStep = Addition(Name(idx), Name(step));
        statements.Add(Assignment([Name(idx)], [addStep]));

        statements.Add(Goto(start));
        statements.Add(Label(end));

        return Statements(statements);
    }
}