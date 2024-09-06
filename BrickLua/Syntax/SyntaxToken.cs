namespace BrickLua.CodeAnalysis.Syntax;

public sealed record SyntaxToken : SyntaxNode
{
    public SyntaxToken(SyntaxKind kind, in SequenceRange location, bool missing = false) : base(location)
    {
        Kind = kind;
        Value = default;
        IsMissing = missing;
    }

    public SyntaxToken(SyntaxKind kind, object value, in SequenceRange location) : base(location)
    {
        Kind = kind;
        Value = value;
    }
    public SyntaxToken(long value, in SequenceRange location) : this(SyntaxKind.IntegerConstant, location)
    {
        Value = value;
    }

    public SyntaxToken(double value, in SequenceRange location) : this(SyntaxKind.FloatConstant, location)
    {
        Value = value;
    }

    public object? Value { get; init; }
    public SyntaxKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the token was synthesized.
    /// </summary>
    public bool IsMissing { get; init; }
}
