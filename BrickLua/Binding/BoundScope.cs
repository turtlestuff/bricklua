using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed record BoundLabel(string Name);

internal sealed class BoundScope(BoundScope? parent)
{
    private readonly Dictionary<string, LocalSymbol> variables = new();

    public BoundScope? Parent { get; } = parent;

    public void Declare(LocalSymbol variable)
    {
        variables[variable.Name] = variable;
    }

    public bool TryLookup(string name, out LocalSymbol variable)
    {
        if (variables.TryGetValue(name, out variable!))
            return true;

        return Parent?.TryLookup(name, out variable) ?? false;
    }

    public LocalSymbol Lookup(string name)
    {
        if (variables.TryGetValue(name, out var variable))
            return variable;

        return Parent?.Lookup(name) ?? throw new ArgumentException($"Expected variable {name} to be defined.");
    }

    public ImmutableArray<LocalSymbol> GetDeclaredVariables()
    {
        return variables.Values.ToImmutableArray();
    }
}
