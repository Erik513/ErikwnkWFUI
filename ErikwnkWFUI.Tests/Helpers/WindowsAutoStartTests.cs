using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using Microsoft.Win32;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>Starting with Windows: the entry of the program in the Run key of the user - here in a key of the tests, never in the real one.</summary>
[Collection(AccentColorTestCollection.Name)]
public class WindowsAutoStartTests : IDisposable
{
    private const string FakePath = @"C:\Programs\Test App\TestApp.exe";

    private readonly string _keyPath = @"Software\ErikwnkWFUI.Tests\Run_" + Guid.NewGuid().ToString("N");
    private readonly string _approvedPath = @"Software\ErikwnkWFUI.Tests\Approved_" + Guid.NewGuid().ToString("N");
    private readonly string _originalKeyPath = WindowsAutoStart.RunKeyPath;
    private readonly string _originalApprovedPath = WindowsAutoStart.ApprovedKeyPath;
    private readonly Func<string> _originalPath = WindowsAutoStart.ExecutablePath;

    public WindowsAutoStartTests()
    {
        WindowsAutoStart.RunKeyPath = _keyPath;
        WindowsAutoStart.ApprovedKeyPath = _approvedPath;
        WindowsAutoStart.ExecutablePath = () => FakePath;
    }

    public void Dispose()
    {
        WindowsAutoStart.RunKeyPath = _originalKeyPath;
        WindowsAutoStart.ApprovedKeyPath = _originalApprovedPath;
        Registry.CurrentUser.DeleteSubKeyTree(_approvedPath, false);
        WindowsAutoStart.ExecutablePath = _originalPath;
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, false);
        Registry.CurrentUser.DeleteSubKey(@"Software\ErikwnkWFUI.Tests", false);
    }

    private string? Entry()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyPath);

        return key?.GetValue(WindowsAutoStart.ApplicationName) as string;
    }

    [Fact]
    public void WithoutAnEntry_TheProgramDoesNotStartWithWindows()
    {
        Assert.False(WindowsAutoStart.IsEnabled());
        Assert.Null(Entry());
    }

    [Fact]
    public void SettingIt_WritesTheQuotedPathUnderTheNameOfTheProgram_AndRemovingItTakesItAway()
    {
        Assert.True(WindowsAutoStart.Set(true));

        Assert.True(WindowsAutoStart.IsEnabled());
        Assert.Equal("\"" + FakePath + "\"", Entry());

        Assert.True(WindowsAutoStart.Set(false));

        Assert.False(WindowsAutoStart.IsEnabled());
        Assert.Null(Entry());
    }

    [Fact]
    public void RemovingWhatIsNotThere_AndSettingTwice_AreFine()
    {
        Assert.True(WindowsAutoStart.Set(false));
        Assert.True(WindowsAutoStart.Set(true));
        Assert.True(WindowsAutoStart.Set(true));

        Assert.True(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void AnEntryOfTheSameNameThatPointsToAnotherProgram_IsNotThisProgram()
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            key.SetValue(WindowsAutoStart.ApplicationName, "\"C:\\Other\\Other.exe\"");

        Assert.False(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void AnEntryThatTheUserSwitchedOffInTaskManager_DoesNotStart_AndSettingItSwitchesItOnAgain()
    {
        WindowsAutoStart.Set(true);
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_approvedPath))
            approved.SetValue(WindowsAutoStart.ApplicationName, new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);

        Assert.False(WindowsAutoStart.IsEnabled());

        Assert.True(WindowsAutoStart.Set(true));

        Assert.True(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void AnEntryThatTaskManagerHasApproved_Starts()
    {
        WindowsAutoStart.Set(true);
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_approvedPath))
            approved.SetValue(WindowsAutoStart.ApplicationName, new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);

        Assert.True(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void TheArguments_AreAppendedToTheQuotedPath_AndNothingIsAppendedWithoutThem()
    {
        WindowsAutoStart.Set(true, "--autostart");
        Assert.Equal("\"" + FakePath + "\" --autostart", Entry());
        Assert.True(WindowsAutoStart.IsEnabled());

        WindowsAutoStart.Set(true, "  ");
        Assert.Equal("\"" + FakePath + "\"", Entry());
    }

    [Fact]
    public void TheNameOfTheEntry_IsTheNameOfTheProgram()
    {
        Assert.False(string.IsNullOrWhiteSpace(WindowsAutoStart.ApplicationName));
    }

    [Fact]
    public void WhenWindowsDoesNotLetItBeWritten_ItIsFalse_AndThereIsNoException()
    {
        // A key that cannot be made: a name with a character that a path of the registry cannot have in this place.
        WindowsAutoStart.RunKeyPath = new string('x', 300);

        Assert.False(WindowsAutoStart.Set(true));
        Assert.False(WindowsAutoStart.IsEnabled());
    }
}
