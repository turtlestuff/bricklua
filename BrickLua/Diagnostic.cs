using System.Collections.ObjectModel;

using BrickLua.CodeAnalysis.Syntax;

namespace BrickLua.CodeAnalysis;

public sealed record Diagnostic(in SequenceRange Location, string Message)
{
    public override string ToString() => Message;
}

internal sealed class DiagnosticBag : ReadOnlyCollection<Diagnostic>
{
    private readonly List<Diagnostic> diagnostics;

    public DiagnosticBag() : base([])
    {
        diagnostics = (List<Diagnostic>)Items;
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        this.diagnostics.AddRange(diagnostics);
    }

    void Report(in SequenceRange location, string message)
    {
        var diagnostic = new Diagnostic(location, message);
        diagnostics.Add(diagnostic);
    }

    internal void ReportUnterminatedString(in SequenceRange location) => Report(location, "Unterminated string literal.");

    internal void ReportUnterminatedLongString(in SequenceRange location) => Report(location, "Unterminated long string literal.");

    internal void ReportUnterminatedLongComment(in SequenceRange location) => Report(location, "Unterminated long comment.");

    internal void ReportBadCharacter(in SequenceRange location, char character) => 
        Report(location, $"Bad character input '{character.GetEofString()}'.");

    internal void ReportInvalidEscapeSequence(in SequenceRange location, char character) => 
        Report(location, $"Invalid escape sequence '{character.GetEofString()}'.");

    internal void ReportUnterminatedEscapeSequence(in SequenceRange location) => Report(location, "Unterminated escape sequence.");

    internal void ReportIncompleteEscapeSequence(in SequenceRange location) => Report(location, "Incomplete escape sequence.");

    internal void ReportExpectedCharacter(in SequenceRange location, char actual, char expected) => 
        Report(location, $"Unexpected character '{actual.GetEofString()}', expected '{actual.GetEofString()}'.");

    internal void ReportUnexpectedToken(in SequenceRange location, SyntaxKind expected, SyntaxKind actual) => 
        Report(location, $"Unexpected token <{actual}>, expected <{expected}>.");

    internal void ReportUnexpectedBreak(in SequenceRange location) =>
        Report(location, $"Unexpected break statement outside of loop.");
}

