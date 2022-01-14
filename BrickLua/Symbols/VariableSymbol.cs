namespace BrickLua.CodeAnalysis.Symbols;

public sealed record VariableSymbol(string Name, bool IsConst = false, bool IsToBeClosed = false);
