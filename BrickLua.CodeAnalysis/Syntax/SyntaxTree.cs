using System.Buffers;
using System.Collections.Immutable;

namespace BrickLua.CodeAnalysis.Syntax;

public sealed class SyntaxTree
{
    internal SyntaxTree(in ReadOnlySequence<char> text, ImmutableArray<Diagnostic> diagnostics, ChunkSyntax root)
    {
        Text = text;
        Diagnostics = diagnostics;
        Root = root;
    }

    public ReadOnlySequence<char> Text { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public ChunkSyntax Root { get; }

    public static SyntaxTree Load(string fileName) => Parse(new ReadOnlySequence<char>(File.ReadAllText(fileName).AsMemory()));

    public static SyntaxTree Parse(string text) => Parse(new ReadOnlySequence<char>(text.AsMemory()));

    public static SyntaxTree Parse(in ReadOnlySequence<char> text) => Parser.Parse(text);
}
