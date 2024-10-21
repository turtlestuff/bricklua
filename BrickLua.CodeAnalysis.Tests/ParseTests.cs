namespace BrickLua.CodeAnalysis.Tests;

using BrickLua.CodeAnalysis.Syntax;

public class ParseTests
{
    [Theory]
    [LuaTestData]
    public void ParseWithNoDiagnostics(string luaCode)
    {
        var syntaxTree = SyntaxTree.Parse(luaCode);

        Assert.Empty(syntaxTree.Diagnostics);
    }
}
