using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Docs;

/// <summary>
/// docs/CONTROLS.md is generated from the built library (see ControlTableGenerator) and must not fall behind: this test fails when the file is not
/// what the generator makes now. To bring it up to date run tools\Update-ControlTable.ps1 (it runs this test with UPDATE_CONTROLS_MD=1, which writes the file).
/// [Collection]: the defaults are read in the dark theme with the blue accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ControlTableTests
{
    private static string FilePath => Path.Combine(ControlTableGenerator.FindRepositoryRoot(), "docs", "CONTROLS.md");

    [Fact]
    public void ControlsMd_IsCurrent()
    {
#if NETFRAMEWORK
        // (The table is made on .NET 8 - the .NET Framework has other members and defaults here and there, e.g. no PlaceholderText.)
        return;
#else
        string expected = ControlTableGenerator.Build();

        if (Environment.GetEnvironmentVariable("UPDATE_CONTROLS_MD") == "1")
        {
            File.WriteAllText(FilePath, expected, new System.Text.UTF8Encoding(false));

            return;
        }

        Assert.True(File.Exists(FilePath), "docs/CONTROLS.md does not exist: run tools\\Update-ControlTable.ps1");

        string actual = File.ReadAllText(FilePath).Replace("\r\n", "\n");

        Assert.True(
            expected == actual,
            "docs/CONTROLS.md is out of date (a control, a property, a default or a comment changed): run tools\\Update-ControlTable.ps1 and commit the file.");
#endif
    }

    [Fact]
    public void EveryControlOfTheLibrary_HasASection_AndNoControlIsMissingFromTheIndex()
    {
        string table = ControlTableGenerator.Build();

        foreach (Type type in ControlTableGenerator.GetControlTypes())
        {
            Assert.Contains("\n## " + type.Name + "\n", table);
            Assert.Contains("| [" + type.Name + "](#" + type.Name.ToLowerInvariant() + ")", table);
        }

        Assert.True(ControlTableGenerator.GetControlTypes().Count >= 30, "fewer controls than expected were found");
    }

    [Fact]
    public void TheGeneratedTable_IsTheSameEveryTime()
    {
        Assert.Equal(ControlTableGenerator.Build(), ControlTableGenerator.Build());
    }
}