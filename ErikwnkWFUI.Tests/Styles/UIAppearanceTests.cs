using System.Drawing;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>The appearance the user chose (theme, accent color, language): captured, applied, kept in a file, and remembered by the library when the program wants that.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UIAppearanceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ErikwnkWFUI.Tests", "Appearance_" + Guid.NewGuid().ToString("N"));
    private readonly Color _originalAccent = UIColors.Primary;
    private readonly bool _originalLight = UIColors.IsLight(UIColors.BackgroundMedium);
    private readonly AppFormHost _host = new AppFormHost();

    public UIAppearanceTests()
    {
        UIStyles.AppearanceDirectory = _folder;
    }

    public void Dispose()
    {
        _host.Dispose();
        UIStyles.ForgetAppearance();
        UIStyles.AppearanceDirectory = null;
        UIColors.ApplyTheme(_originalLight ? UIThemes.Light : UIThemes.Dark);
        UIColors.SetAccent(_originalAccent);
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;

        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private string FilePath
    {
        get { return Path.Combine(_folder, "appearance.json"); }
    }

    // ---- capture and apply

    [Fact]
    public void Capture_ReadsWhatIsAppliedNow()
    {
        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.FromArgb(12, 200, 99));
        UIStrings.Language = UILanguage.German;

        UIAppearance appearance = UIAppearance.Capture();

        Assert.Equal(UIThemeKind.Light, appearance.Theme);
        Assert.Equal("#0CC863", appearance.Accent);
        Assert.Equal(UILanguage.German, appearance.Language);
    }

    [Fact]
    public void Apply_SetsThemeAccentAndLanguage_AndTheLanguageOfTheProgramsOwnTextsToo()
    {
        new UIAppearance { Theme = UIThemeKind.Light, Accent = "#0CC863", Language = UILanguage.German }.Apply();

        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Equal(Color.FromArgb(12, 200, 99).ToArgb(), UIColors.Primary.ToArgb());
        Assert.Equal(UILanguage.German, UIStyles.Language);
        Assert.Equal(AppLanguage.German, AppLocalization.Language);

        new UIAppearance { Theme = UIThemeKind.Dark, Accent = "#0CC863", Language = UILanguage.English }.Apply();

        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("0CC863")]
    public void AnAccentThatIsNotAColor_LeavesTheAccentAsItIs(string? accent)
    {
        UIColors.SetAccent(Color.FromArgb(1, 2, 3));

        new UIAppearance { Accent = accent }.Apply();

        Assert.Equal(Color.FromArgb(1, 2, 3).ToArgb(), UIColors.Primary.ToArgb());
        Assert.Null(new UIAppearance { Accent = accent }.GetAccentColor());
    }

    [Fact]
    public void TheAccent_IsReadWithSurroundingSpaceAndInAnyCase()
    {
        Assert.Equal(Color.FromArgb(171, 205, 239).ToArgb(), new UIAppearance { Accent = " #abcdef " }.GetAccentColor()!.Value.ToArgb());
    }

    [Fact]
    public void WhatWasCaptured_AppliedAgainAfterAChange_GivesTheSameAppearance()
    {
        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.FromArgb(200, 10, 20));
        UIAppearance light = UIAppearance.Capture();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(Color.FromArgb(5, 6, 7));
        light.Apply();

        Assert.Equal(light.Theme, UIAppearance.Capture().Theme);
        Assert.Equal(light.Accent, UIAppearance.Capture().Accent);
    }

    // ---- the file

    [Fact]
    public void SaveAndLoad_GiveTheSameAppearance_AndTheFolderIsCreated()
    {
        UIAppearance appearance = new UIAppearance { Theme = UIThemeKind.Light, Accent = "#0cc863", Language = UILanguage.German };

        Assert.True(appearance.Save(FilePath));
        UIAppearance? loaded = UIAppearance.Load(FilePath);

        Assert.NotNull(loaded);
        Assert.Equal(UIThemeKind.Light, loaded!.Theme);
        Assert.Equal("#0CC863", loaded.Accent);
        Assert.Equal(UILanguage.German, loaded.Language);
    }

    [Fact]
    public void TheFile_IsASmallJsonObject_ThatAProgramCanReadWithAnyJsonReader()
    {
        new UIAppearance { Theme = UIThemeKind.Dark, Accent = "#005A9E", Language = UILanguage.English }.Save(FilePath);

        string text = File.ReadAllText(FilePath);

        Assert.Contains("\"Theme\": \"Dark\"", text);
        Assert.Contains("\"Accent\": \"#005A9E\"", text);
        Assert.Contains("\"Language\": \"English\"", text);
        Assert.StartsWith("{", text.TrimStart());
    }

    [Fact]
    public void LoadingWhatIsNotThere_OrBroken_GivesNothing_OrTheDefaults()
    {
        Assert.Null(UIAppearance.Load(FilePath));

        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "this is not json at all");
        UIAppearance? broken = UIAppearance.Load(FilePath);

        Assert.NotNull(broken);
        Assert.Equal(UIThemeKind.Dark, broken!.Theme);
        Assert.Null(broken.Accent);
        Assert.Equal(UILanguage.English, broken.Language);

        File.WriteAllText(FilePath, "{ \"Theme\": \"Purple\", \"Language\": \"German\" }");
        UIAppearance? partly = UIAppearance.Load(FilePath);

        Assert.Equal(UIThemeKind.Dark, partly!.Theme);
        Assert.Equal(UILanguage.German, partly.Language);
    }

    [Fact]
    public void ANameInAnyCase_IsUnderstood()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{\"Theme\":\"light\",\"Language\":\"german\"}");

        UIAppearance? loaded = UIAppearance.Load(FilePath);

        Assert.Equal(UIThemeKind.Light, loaded!.Theme);
        Assert.Equal(UILanguage.German, loaded.Language);
    }

    [Fact]
    public void ANumberInsteadOfAName_IsNotAccepted_SoNothingOutsideTheValuesIsSet()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{\"Theme\":\"7\",\"Language\":\"\"}");

        UIAppearance? loaded = UIAppearance.Load(FilePath);

        Assert.Equal(UIThemeKind.Dark, loaded!.Theme);
        Assert.Equal(UILanguage.English, loaded.Language);
    }

    [Fact]
    public void SavingWhereNothingCanBeWritten_IsNoError()
    {
        Directory.CreateDirectory(_folder);
        string blocked = Path.Combine(_folder, "blocked");
        File.WriteAllText(blocked, "a file where the folder should be");

        Assert.False(new UIAppearance().Save(Path.Combine(blocked, "appearance.json")));
        Assert.Null(UIAppearance.Load(Path.Combine(blocked, "appearance.json")));
    }

    // ---- remember

    [Fact]
    public void RememberAppearance_WithoutAFile_ChangesNothing_AndSavesNothingUntilAsked()
    {
        Assert.False(UIStyles.RememberAppearance());

        Assert.False(File.Exists(FilePath));
        Assert.True(UIStyles.SaveAppearance());
        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public void RememberAppearance_LoadsAndAppliesTheFile()
    {
        new UIAppearance { Theme = UIThemeKind.Light, Accent = "#0CC863", Language = UILanguage.German }.Save(FilePath);

        Assert.True(UIStyles.RememberAppearance());

        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Equal(Color.FromArgb(12, 200, 99).ToArgb(), UIColors.Primary.ToArgb());
        Assert.Equal(UILanguage.German, UIStyles.Language);
    }

    [Fact]
    public void SaveAppearance_WithoutRemember_DoesNothing_AndAfterForgettingToo()
    {
        Assert.False(UIStyles.SaveAppearance());
        Assert.False(File.Exists(FilePath));

        UIStyles.RememberAppearance();
        UIStyles.ForgetAppearance();

        Assert.False(UIStyles.SaveAppearance());
        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void ChangingTheAppearance_DoesNotWriteAnything_OnItsOwn()
    {
        UIStyles.RememberAppearance();

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.FromArgb(9, 9, 9));
        UIStyles.Language = UILanguage.German;

        Assert.False(File.Exists(FilePath));
    }

    // ---- the options window

    [Fact]
    public void SavingInTheOptionsWindow_WritesTheAppearance_WhenItIsRemembered()
    {
        UIStyles.RememberAppearance();
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme, OptionsPreset.Language }));
        form.GetPrivateField<System.Windows.Forms.Button>("_themeButton")!.InvokePrivate("OnClick", EventArgs.Empty);
        form.GetPrivateField<System.Windows.Forms.ComboBox>("_languageBox")!.SelectedIndex = 1;

        Assert.False(File.Exists(FilePath));

        form.GetPrivateField<System.Windows.Forms.Button>("_saveButton")!.PerformClick();

        UIAppearance? loaded = UIAppearance.Load(FilePath);
        Assert.Equal(UIThemeKind.Light, loaded!.Theme);
        Assert.Equal(UILanguage.German, loaded.Language);
    }

    [Fact]
    public void CancellingTheOptionsWindow_WritesNothing_AndWithoutRememberSavingWritesNothing()
    {
        UIStyles.RememberAppearance();
        OptionsForm cancelled = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        cancelled.GetPrivateField<System.Windows.Forms.Button>("_themeButton")!.InvokePrivate("OnClick", EventArgs.Empty);
        cancelled.GetPrivateField<System.Windows.Forms.Button>("_cancelButton")!.PerformClick();

        Assert.False(File.Exists(FilePath));

        UIStyles.ForgetAppearance();
        OptionsForm saved = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        saved.GetPrivateField<System.Windows.Forms.Button>("_saveButton")!.PerformClick();

        Assert.False(File.Exists(FilePath));
    }
}
