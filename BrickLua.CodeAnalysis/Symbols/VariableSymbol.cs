namespace BrickLua.CodeAnalysis.Symbols;

public abstract record Symbol(string Name);

public sealed record LocalSymbol(string Name, bool IsConst = false, bool IsToBeClosed = false) : Symbol(Name);

public sealed record LabelSymbol(string Name) : Symbol(Name);