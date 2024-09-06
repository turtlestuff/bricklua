using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class BoundScope(BoundScope? parent)
{
    private readonly Dictionary<string, VariableSymbol> variables = new();

    public BoundScope? Parent { get; } = parent;

    public bool TryDeclare(VariableSymbol variable)
    {
        return variables.TryAdd(variable.Name, variable);
    }

    public bool TryLookup(string name, out VariableSymbol variable)
    {
        if (variables.TryGetValue(name, out variable!))
            return true;

        return Parent?.TryLookup(name, out variable) ?? false;
    }

    public VariableSymbol Lookup(string name)
    {
        if (variables.TryGetValue(name, out var variable))
            return variable;

        return Parent?.Lookup(name) ?? throw new ArgumentException($"Expected variable {name} to be defined.");
    }

    public ImmutableArray<VariableSymbol> GetDeclaredVariables()
    {
        return variables.Values.ToImmutableArray();
    }

}
