namespace BrickLua.CodeAnalysis.Syntax;

public readonly struct SequenceRange : IEquatable<SequenceRange>
{
    public SequenceRange(SequencePosition start, SequencePosition end)
    {
        Start = start;
        End = end;
    }

    public SequencePosition Start { get; }
    public SequencePosition End { get; }

    public override bool Equals(object? obj) => obj is SequenceRange span && Equals(span);
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(in SequenceRange left, in SequenceRange right) => left.Equals(right);
    public static bool operator !=(in SequenceRange left, in SequenceRange right) => !(left == right);

    public bool Equals(SequenceRange other) => Start.Equals(other.Start) && End.Equals(other.End);
}
