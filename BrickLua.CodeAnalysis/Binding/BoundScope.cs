using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class BoundScope(BoundScope? parent, bool stopLabelSearch = false)
{
    private readonly Dictionary<string, LocalSymbol> variables = [];
    private readonly Dictionary<string, LabelSymbol> labels = [];

    private readonly bool stopLabelSearch = stopLabelSearch;

    public BoundScope? Parent { get; } = parent;

    public void Declare(LocalSymbol variable)
    {
        variables[variable.Name] = variable;
    }

    public void Declare(LabelSymbol label)
    {
        labels[label.Name] = label;
    }

    public bool TryLookup(string name, out LocalSymbol variable)
    {
        if (variables.TryGetValue(name, out variable!))
        {
            return true;
        }

        return Parent?.TryLookup(name, out variable) ?? false;
    }

    public bool TryLookup(string name, out LabelSymbol label)
    {
        if (labels.TryGetValue(name, out label!))
        {
            return true;
        }

        if (stopLabelSearch)
        {
            return false;
        }
        
        return Parent?.TryLookup(name, out label) ?? false;
    }

    public LocalSymbol Lookup(string name)
    {
        if (variables.TryGetValue(name, out var variable))
        {
            return variable;
        }

        return Parent?.Lookup(name) ?? throw new ArgumentException($"Expected variable {name} to be defined.");
    }

    public ImmutableArray<LocalSymbol> GetDeclaredVariables()
    {
        return variables.Values.ToImmutableArray();
    }
}
