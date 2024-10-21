namespace BrickLua.CodeAnalysis.Tests;

public sealed class LuaTestData : TheoryData<string>
{
    public LuaTestData()
    {
        var asm = typeof(LuaTestData).Assembly;

        var files = asm.GetManifestResourceNames();
        foreach (var file in files)
        {
            var luaCode = new StreamReader(asm.GetManifestResourceStream(file)!).ReadToEnd();
            Add(luaCode);
        }
    }
}

public sealed class LuaTestDataAttribute() : ClassDataAttribute(typeof(LuaTestData));
