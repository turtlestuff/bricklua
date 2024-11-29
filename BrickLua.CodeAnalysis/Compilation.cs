using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis;

public sealed class Compilation
{
    private Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
    {
        IsScript = isScript;
        Previous = previous;
        SyntaxTrees = syntaxTrees.ToImmutableArray();
    }

    public static Compilation Create(params SyntaxTree[] syntaxTrees)
    {
        return new Compilation(isScript: false, previous: null, syntaxTrees);
    }

    public static Compilation CreateScript(Compilation previous, params SyntaxTree[] syntaxTrees)
    {
        return new Compilation(isScript: true, previous, syntaxTrees);
    }

    public bool IsScript { get; }
    public Compilation? Previous { get; }
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
}
