using System.Reflection;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The name the library keeps files and keys under for the program that uses it.</summary>
public class EntryAssemblyNameTests
{
    [Fact]
    public void TheName_IsThatOfTheEntryAssembly_NotOfTheLibrary()
    {
        string name = EntryAssemblyName.Get();

        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.NotEqual("ErikwnkWFUI", name);
        Assert.Equal(Assembly.GetEntryAssembly()?.GetName().Name ?? "Program", name);
    }

    [Fact]
    public void EveryPlaceThatNamesAProgram_UsesTheSameName()
    {
        Assert.Equal(EntryAssemblyName.Get(), WindowsAutoStart.ApplicationName);
    }
}
