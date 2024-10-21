namespace BrickLua.CodeAnalysis.Tests;

using System.Buffers;

using BrickLua.CodeAnalysis.Syntax;

public class ParseTests
{
    [Theory]
    [LuaTestData]
    public void ParseWithNoDiagnostics(string luaCode)
    {
        var seq = new ReadOnlySequence<char>(luaCode.AsMemory());
        var parser = new Parser(new Lexer(new SequenceReader<char>(seq)));
        var chunk = parser.ParseChunk();

        Assert.Empty(parser.Diagnostics);
    }
}
