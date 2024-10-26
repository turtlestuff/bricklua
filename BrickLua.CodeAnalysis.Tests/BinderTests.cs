namespace BrickLua.CodeAnalysis.Tests;

using BrickLua.CodeAnalysis.Binding;
using BrickLua.CodeAnalysis.Syntax;

public class BinderTests
{
    [Theory]
    [LuaTestData]
    public void BindWithNoDiagnostics(string luaCode)
    {
        var chunk = SyntaxTree.Parse(luaCode);
        var boundChunk = Binder.BindChunk(chunk);

        Assert.Empty(boundChunk.Diagnostics);
    }
}
