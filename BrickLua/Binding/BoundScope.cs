using System.Collections.Immutable;

using BrickLua.CodeAnalysis.Symbols;

namespace BrickLua.CodeAnalysis.Binding;

internal sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> variables = new();

    public BoundScope Parent { get; }

    public BoundScope(BoundScope parent)
    {
        Parent = parent;
    }

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

    public ImmutableArray<VariableSymbol> GetDeclaredVariables()
    {
        return variables.Values.ToImmutableArray();
    }

}
