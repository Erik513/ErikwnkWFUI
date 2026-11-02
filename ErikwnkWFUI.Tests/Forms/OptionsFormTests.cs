using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Microsoft.Win32;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The options window: a settings window with a table and the buttons Save and Cancel (or without them), in the language of the library.</summary>
[Collection(AccentColorTestCollection.Name)]
public class OptionsFormTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();
    private readonly string _keyPath = @"Software\ErikwnkWFUI.Tests\OptionsRun_" + Guid.NewGuid().ToString("N");
    private readonly string _originalKeyPath = WindowsAutoStart.RunKeyPath;
    private readonly Func<string> _originalPath = WindowsAutoStart.ExecutablePath;
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentUICulture;

    public OptionsFormTests()
    {
        // Starting with Windows is tried in a key of the tests, never in the real one.
        WindowsAutoStart.RunKeyPath = _keyPath;
        WindowsAutoStart.ExecutablePath = () => @"C:\Programs\Test App\TestApp.exe";
    }

    public void Dispose()
    {
        _host.Dispose();
        WindowsAutoStart.RunKeyPath = _originalKeyPath;
        WindowsAutoStart.ExecutablePath = _originalPath;
        CultureInfo.CurrentUICulture = _originalCulture;
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, false);
        Registry.CurrentUser.DeleteSubKey(@"Software\ErikwnkWFUI.Tests", false);
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Button Find(OptionsForm form, string field)
    {
        return form.GetPrivateField<Button>(field)!;
    }

    [Fact]
    public void ItIsASettingsWindow_WithTheTableTheVersionStripAndTheTitleOfTheLanguage()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        Assert.NotNull(form.PropertyTable);
        Assert.NotNull(form.VersionStrip);
        Assert.Equal("Options", form.Text);
        Assert.False(form.TitleBar.ShowMaximizeButton);
        Assert.Equal(ContentAlignment.MiddleLeft, form.TitleBar.TitleTextAlign);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Optionen", _host.Add(UIStyles.Forms.CreateOptionsStandard()).Text);
        Assert.Equal("My title", _host.Add(UIStyles.Forms.CreateOptionsStandard("My title")).Text);
    }

    [Fact]
    public void TheButtons_AreInTheLanguage_AndFollowAChangeWhileTheWindowIsOpen()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());

        Assert.Equal("Save", form.SaveButtonText);
        Assert.Equal("Cancel", form.CancelButtonText);

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Speichern", form.SaveButtonText);
        Assert.Equal("Abbrechen", form.CancelButtonText);
    }

    [Fact]
    public void ATextThatWasSet_IsKeptWhateverTheLanguage_AndNullGoesBackToTheLanguage()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.SaveButtonText = "Apply";
        form.CancelButtonText = "Discard";

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Apply", form.SaveButtonText);
        Assert.Equal("Discard", form.CancelButtonText);

        form.SaveButtonText = null!;
        form.CancelButtonText = null!;

        Assert.Equal("Speichern", form.SaveButtonText);
        Assert.Equal("Abbrechen", form.CancelButtonText);
    }

    [Fact]
    public void SaveRaisesTheEventAndClosesWithOk_CancelClosesWithCancel()
    {
        OptionsForm saved = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm cancelled = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        int raised = 0;
        saved.SaveClicked += (sender, e) => raised++;
        cancelled.SaveClicked += (sender, e) => raised += 100;

        Find(saved, "_saveButton").PerformClick();
        Find(cancelled, "_cancelButton").PerformClick();

        Assert.Equal(1, raised);
        Assert.Equal(DialogResult.OK, saved.DialogResult);
        Assert.Equal(DialogResult.Cancel, cancelled.DialogResult);
    }

    [Fact]
    public void SaveWaitsForTheEditorsToAcceptTheirValues_ALockedWindowStaysOpen()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        System.Windows.Forms.TextBox name = new System.Windows.Forms.TextBox();
        form.PropertyTable.AddRow("Name", name);
        int saved = 0;
        form.SaveClicked += (sender, e) => saved++;
        name.Validating += (sender, e) => e.Cancel = name.Text.Length == 0;

        Find(form, "_saveButton").PerformClick();

        Assert.Equal(0, saved);
        Assert.False(form.IsDisposed);
        Assert.NotEqual(DialogResult.OK, form.DialogResult);

        name.Text = "ready";
        Find(form, "_saveButton").PerformClick();

        Assert.Equal(1, saved);
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void WithoutTheButtons_TheBarIsGone_EnterAndEscapeAreNotTheirs_AndTheWindowIsLower()
    {
        OptionsForm with = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm without = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        without.ShowButtons = false;

        Assert.True(with.ShowButtons);
        Assert.False(without.ShowButtons);
        Assert.NotNull(with.AcceptButton);
        Assert.NotNull(with.CancelButton);
        Assert.Null(without.AcceptButton);
        Assert.Null(without.CancelButton);

        with.FitToContent();
        without.FitToContent();
        Assert.True(without.ClientSize.Height < with.ClientSize.Height);

        without.ShowButtons = true;
        Assert.NotNull(without.CancelButton);
    }

    [Fact]
    public void WithoutTheButtons_EscapeClosesTheWindow_LikeInEveryDialog()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        form.ShowButtons = false;
        bool closed = false;
        form.FormClosed += (sender, e) => closed = true;

        Assert.True(form.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));
        Assert.True(closed);
    }

    [Fact]
    public void FitToContent_MakesRoomForTheRows_TheButtonsAndTheVersionStrip()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.PropertyTable.AddSection("Section");
        form.PropertyTable.AddRow("Row", new Button());
        form.PropertyTable.AddRow("Row 2", new Button());

        form.FitToContent();

        int rows = form.PropertyTable.PreferredSize.Height;
        Assert.True(form.ClientSize.Height >= form.TitleBar.Height + rows + 56 + form.VersionStrip!.Height);
    }

    private static Panel ScrollHost(OptionsForm form)
    {
        return form.GetPrivateField<Panel>("_scrollHost")!;
    }

    [Fact]
    public void ManyRows_MakeTheWindowAsHighAsTheScreenAllows_AndTheTableScrolls_TheButtonsAndTheStripStay()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        for (int i = 0; i < 80; i++)
            form.PropertyTable.AddRow("Row " + i, new Button());

        form.FitToContent();
        _host.Show(form);
        TestPictures.Pump(100);

        Rectangle area = Screen.FromControl(form).WorkingArea;
        Panel host = ScrollHost(form);
        Panel bar = form.GetPrivateField<Panel>("_buttonBar")!;

        Assert.True(form.Height <= area.Height, "the window is higher than the screen");
        Assert.True(host.DisplayRectangle.Height > host.ClientRectangle.Height, "the table does not scroll");
        Assert.True(bar.Bottom <= form.ContentPanel.ClientRectangle.Bottom + 1, "the buttons are pushed out of the window");
        Assert.True(form.VersionStrip!.Visible);
        Assert.True(bar.Visible);
    }

    [Fact]
    public void FewRows_NeedNoScrolling_AndTheWindowIsNotCapped()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.PropertyTable.AddSection("Section");
        form.PropertyTable.AddRow("Row", new Button());

        form.FitToContent();
        _host.Show(form);
        TestPictures.Pump(100);

        Panel host = ScrollHost(form);
        Assert.True(host.DisplayRectangle.Height <= host.ClientRectangle.Height, "a table that fits scrolls");
        Assert.True(form.Height < Screen.FromControl(form).WorkingArea.Height / 2);
    }

    [Fact]
    public void TheSizes_AreGivenFor96Dpi_AndScaledToTheScreen()
    {
        OptionsForm normal = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm big = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        big.DpiOverride = 144;
        Panel normalBar = normal.GetPrivateField<Panel>("_buttonBar")!;
        Panel bigBar = big.GetPrivateField<Panel>("_buttonBar")!;

        Assert.Equal(Dpi.Scale(56, normal.DeviceDpi), normalBar.Height);
        Assert.Equal(new Size(Dpi.Scale(340, normal.DeviceDpi), Dpi.Scale(160, normal.DeviceDpi)), normal.MinimumSize);

        Assert.Equal(84, bigBar.Height);
        Assert.Equal(new Size(510, 240), big.MinimumSize);
        Assert.Equal(new Padding(30), big.ContentPanel.Padding);
        Assert.Equal(new Size(150, 48), Find(big, "_saveButton").Size);
        Assert.Equal(new Size(150, 48), Find(big, "_cancelButton").Size);
    }

    // ---- the basic settings

    private static Button ThemeButton(OptionsForm form)
    {
        return Find(form, "_themeButton");
    }

    // The button takes no focus (like a tool bar button), so PerformClick - which needs a button that can be selected - does nothing.
    private static void ClickTheme(OptionsForm form)
    {
        ThemeButton(form).InvokePrivate("OnClick", EventArgs.Empty);
    }

    private static System.Windows.Forms.ComboBox Box(OptionsForm form, string field)
    {
        return form.GetPrivateField<System.Windows.Forms.ComboBox>(field)!;
    }

    private static string[] RowLabels(OptionsForm form)
    {
        return PropertyTableHost.AllLabels(form.PropertyTable).Select(l => l.Text).ToArray();
    }

    [Fact]
    public void TheTable_StartsWithTheReadyMadeOptions_ThemeAccentColorLanguageAndAlwaysOnTop_WhichShowWhatIsApplied()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        Assert.Equal(new[] { "General", "Accent color", "Language", "Always on top" }, RowLabels(form));
        Assert.Equal(UIStyles.Symbols.Sun, ThemeButton(form).Text);
        Assert.Equal(0, Box(form, "_accentBox").SelectedIndex);
        Assert.Equal(0, Box(form, "_languageBox").SelectedIndex);
        Assert.Equal(15, Box(form, "_accentBox").Items.Count);
        Assert.Equal(new object[] { "English", "Deutsch" }, Box(form, "_languageBox").Items.Cast<object>().ToArray());
        Assert.Same(UIThemes.Dark, form.SelectedTheme);
        Assert.Equal(UIAccentColors.Blue.ToArgb(), form.SelectedAccentColor.ToArgb());
        Assert.Equal(UILanguage.English, form.SelectedLanguage);
    }

    [Fact]
    public void ChoosingATheme_AnAccentColor_OrALanguage_AppliesItAtOnce()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());

        ClickTheme(form);
        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Same(UIThemes.Light, form.SelectedTheme);

        Box(form, "_accentBox").SelectedIndex = 3;
        Assert.Equal(UIAccentColors.Amber.ToArgb(), UIColors.Primary.ToArgb());

        Box(form, "_languageBox").SelectedIndex = 1;
        Assert.Equal(UILanguage.German, UIStrings.Language);
        Assert.Equal(AppLanguage.German, AppLocalization.Language);
        Assert.Equal(UILanguage.German, form.SelectedLanguage);
    }

    [Fact]
    public void AfterALanguageSwitch_TheNamesOfTheRowsAndTheItemsAreInTheNewLanguage()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());

        Box(form, "_languageBox").SelectedIndex = 1;

        Assert.Equal(new[] { "Allgemein", "Akzentfarbe", "Sprache", "Immer im Vordergrund" }, RowLabels(form));
        Assert.Equal("Zum hellen Design wechseln", ThemeButton(form).AccessibleName);
        Assert.Equal("Rot", Box(form, "_accentBox").Items[1]);
        Assert.Equal(1, Box(form, "_languageBox").SelectedIndex);
        Assert.Equal("Speichern", form.SaveButtonText);
    }

    [Fact]
    public void ClosedWithoutSave_WhatWasChosenIsTakenBack_ClosedWithSaveItStays()
    {
        OptionsForm cancelled = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        ClickTheme(cancelled);
        Box(cancelled, "_accentBox").SelectedIndex = 3;
        Box(cancelled, "_languageBox").SelectedIndex = 1;

        Find(cancelled, "_cancelButton").PerformClick();

        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Equal(UIAccentColors.Blue.ToArgb(), UIColors.Primary.ToArgb());
        Assert.Equal(UILanguage.English, UIStrings.Language);
        Assert.Equal(AppLanguage.English, AppLocalization.Language);

        OptionsForm saved = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        ClickTheme(saved);

        Find(saved, "_saveButton").PerformClick();

        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Fact]
    public void ClosedByTheCloseButtonOrEscape_WithoutSave_AlsoTakesItBack_AndWithoutTheButtonsNothingIsTakenBack()
    {
        OptionsForm closed = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        ClickTheme(closed);
        closed.Close();
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));

        OptionsForm direct = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        direct.ShowButtons = false;
        ClickTheme(direct);
        direct.Close();
        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Fact]
    public void WithAnEmptySelection_TheTableHasNoOptions_AndAddOptionPutsThemInOneByOne_NeverTwice()
    {
        OptionsForm none = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new OptionsPreset[0]));
        OptionsForm some = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language }));
        OptionsForm all = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        Assert.Empty(RowLabels(none));
        Assert.Equal(new[] { "General", "Language" }, RowLabels(some));
        Assert.Equal(4, RowLabels(all).Length);

        // The theme is no row: a button in the corner, there once.
        Assert.Null(none.AddOption(OptionsPreset.Theme));
        Assert.Empty(RowLabels(none));
        Button corner = ThemeButton(none);
        none.AddOption(OptionsPreset.AlwaysOnTop);
        Assert.Null(none.AddOption(OptionsPreset.Theme));
        Assert.Same(corner, ThemeButton(none));
        Assert.Equal(new[] { "General", "Always on top" }, RowLabels(none));

        none.AddOptions(OptionsPreset.AccentColor, OptionsPreset.Language, OptionsPreset.Theme);
        Assert.Equal(new[] { "General", "Always on top", "Accent color", "Language" }, RowLabels(none));
    }

    [Fact]
    public void TheOptionsOfTheLibrary_CanBeMixedWithOwnRows_AndAnOptionAddedLaterStandsBelow()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        form.PropertyTable.AddRow("Own", new Button());
        form.AddOption(OptionsPreset.Language);

        Assert.Equal(new[] { "Own", "General", "Language" }, RowLabels(form));
    }

    [Fact]
    public void TheTableIsACardInTheMiddleOfTheWindow_WithTheSameRoomAtBothSides()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.PropertyTable.AddRow("Own", new Button());
        form.FitToContent();
        _host.Show(form);
        TestPictures.Pump(100);

        Panel host = ScrollHost(form);
        int tableWidth = form.PropertyTable.Width;

        Assert.Equal(Dpi.Scale(520, form.DeviceDpi), tableWidth);
        Assert.True(form.PropertyTable.Left > 0, "the table fills the window to its edge");
        Assert.Equal(form.PropertyTable.Left, host.ClientSize.Width - form.PropertyTable.Right);
    }

    [Fact]
    public void AWiderWindow_KeepsTheCardInTheMiddle()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new OptionsPreset[0]));
        form.PropertyTable.AddRow("Own", new Button());
        form.ClientSize = new Size(1000, 400);
        _host.Show(form);
        TestPictures.Pump(100);

        Panel host = ScrollHost(form);

        Assert.True(form.PropertyTable.Left > 100);
        Assert.Equal(form.PropertyTable.Left, host.ClientSize.Width - form.PropertyTable.Right);
    }

    [Fact]
    public void AlwaysOnTop_ActsOnTheWindowThatOwnsTheOptions_AndIsTakenBackWithoutSave()
    {
        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main window"));
        OptionsForm cancelled = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        cancelled.Show(owner);

        ErikwnkWFUI.Controls.ToggleSwitch toggle = cancelled.GetPrivateField<ErikwnkWFUI.Controls.ToggleSwitch>("_topMostSwitch")!;
        Assert.False(toggle.Checked);

        toggle.Checked = true;
        Assert.True(owner.TopMost);
        Assert.True(cancelled.SelectedAlwaysOnTop);

        Find(cancelled, "_cancelButton").PerformClick();
        Assert.False(owner.TopMost);

        OptionsForm saved = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        saved.Show(owner);
        saved.GetPrivateField<ErikwnkWFUI.Controls.ToggleSwitch>("_topMostSwitch")!.Checked = true;
        Find(saved, "_saveButton").PerformClick();
        Assert.True(owner.TopMost);
    }

    [Fact]
    public void TheAlwaysOnTopSwitch_ShowsWhetherTheOwnerIsOnTopAlready()
    {
        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main window"));
        owner.TopMost = true;
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        form.Show(owner);

        Assert.True(form.GetPrivateField<ErikwnkWFUI.Controls.ToggleSwitch>("_topMostSwitch")!.Checked);
        Assert.True(owner.TopMost);
    }

    [Fact]
    public void TheRowOfAnyTable_CanChangeItsText_AfterwardsWithoutBuildingTheTableAnew()
    {
        using ErikwnkWFUI.Controls.PropertyTable table = new ErikwnkWFUI.Controls.PropertyTable();
        ErikwnkWFUI.Controls.PropertyRow section = table.AddSection("Old section");
        ErikwnkWFUI.Controls.PropertyRow row = table.AddRow("Old row", new Button());

        section.Text = "New section";
        row.Text = "New row";

        Assert.Equal("New section", section.Text);
        Assert.Equal("New row", row.Text);
        Assert.Equal(new[] { "New section", "New row" }, PropertyTableHost.AllLabels(table).Select(l => l.Text).ToArray());

        table.ClearRows();
        Assert.Null(Record.Exception(() => row.Text = "Gone"));
    }

    // ---- start with Windows, reset to defaults, the toasts

    private static ErikwnkWFUI.Controls.ToggleSwitch Switch(OptionsForm form, string field)
    {
        return form.GetPrivateField<ErikwnkWFUI.Controls.ToggleSwitch>(field)!;
    }

    private string? RunEntry()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyPath);

        return key?.GetValue(WindowsAutoStart.ApplicationName) as string;
    }

    [Fact]
    public void StartWithWindows_ShowsWhatWindowsHas_AndIsWrittenOnlyWhenTheUserSaves()
    {
        WindowsAutoStart.Set(true);
        OptionsForm on = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        Assert.True(Switch(on, "_startSwitch").Checked);
        Assert.Contains("Start with Windows", RowLabels(on));

        Switch(on, "_startSwitch").Checked = false;
        Assert.NotNull(RunEntry());
        Assert.False(on.SelectedStartWithWindows);

        Find(on, "_cancelButton").PerformClick();
        Assert.NotNull(RunEntry());

        OptionsForm save = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        Switch(save, "_startSwitch").Checked = false;
        Find(save, "_saveButton").PerformClick();

        Assert.Null(RunEntry());
        Assert.False(WindowsAutoStart.IsEnabled());

        OptionsForm enable = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        Switch(enable, "_startSwitch").Checked = true;
        Find(enable, "_saveButton").PerformClick();

        Assert.True(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void WithoutTheButtons_StartWithWindowsIsWrittenAtOnce()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        form.ShowButtons = false;

        Switch(form, "_startSwitch").Checked = true;

        Assert.True(WindowsAutoStart.IsEnabled());
    }

    [Fact]
    public void TheButtonReset_PutsTheOptionsBackToTheirDefaults_TellsTheProgram_AndCancelTakesItBackToWhatWasThere()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(UIAccentColors.Purple);
        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main window"));
        owner.TopMost = true;

        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new[]
        {
            OptionsPreset.Theme, OptionsPreset.AccentColor, OptionsPreset.Language, OptionsPreset.AlwaysOnTop,
            OptionsPreset.StartWithWindows, OptionsPreset.ResetToDefaults
        }));
        form.Show(owner);
        Switch(form, "_startSwitch").Checked = true;
        Box(form, "_languageBox").SelectedIndex = 1;
        int told = 0;
        form.DefaultsRestored += (sender, e) => told++;

        Find(form, "_resetButton").PerformClick();

        Assert.Equal(1, told);
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Equal(UIAccentColors.Blue.ToArgb(), UIColors.Primary.ToArgb());
        Assert.Equal(UILanguage.English, UIStrings.Language);
        Assert.False(owner.TopMost);
        Assert.False(Switch(form, "_startSwitch").Checked);
        Assert.Equal(new[] { "" }, RowLabels(form).Where(label => label.Length == 0).ToArray()); // the reset row has no label: the button says it all

        Find(form, "_cancelButton").PerformClick();

        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
        Assert.Equal(UIAccentColors.Purple.ToArgb(), UIColors.Primary.ToArgb());
        Assert.True(owner.TopMost);
    }

    [Fact]
    public void StartWithWindows_CanStartTheProgramWithArguments_SoItCanTellThatWindowsStartedIt()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        form.StartWithWindowsArguments = "--autostart";
        Switch(form, "_startSwitch").Checked = true;

        Find(form, "_saveButton").PerformClick();

        Assert.Equal("\"C:\\Programs\\Test App\\TestApp.exe\" --autostart", RunEntry());
    }

    private static List<(string Message, Form? Owner)> CaptureToasts(OptionsForm form)
    {
        List<(string, Form?)> toasts = new List<(string, Form?)>();
        form.ShowToast = (message, owner) => toasts.Add((message, owner));

        return toasts;
    }

    [Fact]
    public void SavingTellsTheUserByAToast_InTheLanguageOfTheLibrary_OverTheWindowThatOwnsTheOptions()
    {
        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main window"));
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language }));
        List<(string Message, Form? Owner)> toasts = CaptureToasts(form);
        form.Show(owner);

        Find(form, "_saveButton").PerformClick();

        Assert.Equal(("Settings saved", (Form?)owner), toasts.Single());

        UIStrings.Language = UILanguage.German;
        OptionsForm german = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        List<(string Message, Form? Owner)> germanToasts = CaptureToasts(german);
        german.Show(owner);
        Find(german, "_saveButton").PerformClick();

        Assert.Equal("Einstellungen gespeichert", germanToasts.Single().Message);
    }

    [Fact]
    public void WithoutAnOwner_OrWithToastsSwitchedOff_NothingIsShown_AndCancelShowsNoToast()
    {
        OptionsForm noOwner = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        List<(string Message, Form? Owner)> none = CaptureToasts(noOwner);
        Find(noOwner, "_saveButton").PerformClick();
        Assert.Empty(none);

        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main window"));
        OptionsForm off = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        off.ShowToasts = false;
        List<(string Message, Form? Owner)> silent = CaptureToasts(off);
        off.Show(owner);
        Find(off, "_saveButton").PerformClick();
        Assert.Empty(silent);

        OptionsForm cancelled = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        List<(string Message, Form? Owner)> cancelledToasts = CaptureToasts(cancelled);
        cancelled.Show(owner);
        Find(cancelled, "_cancelButton").PerformClick();
        Assert.Empty(cancelledToasts);
    }

    [Fact]
    public void ResettingTellsTheUserByAToast_InTheLanguageThatTheDefaultsHave()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language, OptionsPreset.ResetToDefaults }));
        List<(string Message, Form? Owner)> toasts = CaptureToasts(form);

        Find(form, "_resetButton").PerformClick();

        // The defaults are German here, and the toast is in the language they brought, not in the one before.
        Assert.Equal(("Standardwerte wiederhergestellt", (Form?)form), toasts.Single());
    }

    [Fact]
    public void TheDefaultLanguage_IsTheLanguageOfWindows_GermanForAGermanOne()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language, OptionsPreset.ResetToDefaults }));

        Find(form, "_resetButton").PerformClick();

        Assert.Equal(UILanguage.German, UIStrings.Language);
        Assert.Contains("Auf Standard zurücksetzen", PropertyTableHost.AllButtons(form.PropertyTable).Select(b => b.Text));
    }

    // ---- edge cases

    [Fact]
    public void EnterSavesAndEscapeCancels_AsInEveryDialog()
    {
        OptionsForm entered = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        int saved = 0;
        entered.SaveClicked += (sender, e) => saved++;
        ClickTheme(entered);

        Assert.True(entered.InvokePrivate<bool>("ProcessDialogKey", Keys.Enter));

        Assert.Equal(1, saved);
        Assert.Equal(DialogResult.OK, entered.DialogResult);
        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));

        UIColors.ApplyTheme(UIThemes.Dark);
        OptionsForm escaped = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        ClickTheme(escaped);

        Assert.True(escaped.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));

        Assert.Equal(DialogResult.Cancel, escaped.DialogResult);
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Fact]
    public void ClickingSaveTwice_AndDisposingTwice_DoNotThrow()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());

        Assert.Null(Record.Exception(() =>
        {
            Find(form, "_saveButton").PerformClick();
            Find(form, "_saveButton").PerformClick();
            form.Dispose();
            form.Dispose();
        }));
    }

    [Fact]
    public void AWindowThatWasNeverShown_CanBeFittedAndDisposed()
    {
        OptionsForm form = UIStyles.Forms.CreateOptionsStandard();

        Assert.Null(Record.Exception(() =>
        {
            form.FitToContent();
            form.FitToContent();
            form.Dispose();
        }));
    }

    [Fact]
    public void WithNoRowsAtAll_TheWindowIsAsLowAsItsLeastSize_AndFittingTwiceGivesTheSameSize()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard(presets: new OptionsPreset[0]));

        form.FitToContent();
        Size first = form.ClientSize;
        form.FitToContent();

        Assert.Equal(first, form.ClientSize);
        Assert.True(form.ClientSize.Width >= form.MinimumSize.Width && form.ClientSize.Height >= form.MinimumSize.Height);
    }

    [Fact]
    public void AnOptionAddedWhileTheWindowIsOpen_IsThere_AndTheWindowCanBeFittedAgain()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        form.FitToContent();
        int before = form.ClientSize.Height;

        form.AddOption(OptionsPreset.Language);
        form.FitToContent();
        TestPictures.Pump(60);

        Assert.Equal(new[] { "General", "Language" }, RowLabels(form));
        Assert.True(form.ClientSize.Height > before);
    }

    [Fact]
    public void AnAccentColorThatIsNotInTheList_ShowsNoChoice_AndCancelGivesItBack()
    {
        Color custom = Color.FromArgb(12, 200, 99);
        UIColors.SetAccent(custom);
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.AccentColor, OptionsPreset.Theme }));

        Assert.Equal(-1, Box(form, "_accentBox").SelectedIndex);
        Assert.Equal(custom.ToArgb(), form.SelectedAccentColor.ToArgb());

        Box(form, "_accentBox").SelectedIndex = 2;
        Assert.NotEqual(custom.ToArgb(), UIColors.Primary.ToArgb());

        Find(form, "_cancelButton").PerformClick();
        Assert.Equal(custom.ToArgb(), UIColors.Primary.ToArgb());
    }

    [Fact]
    public void WhenWindowsDoesNotLetTheAutoStartBeWritten_SaveStillClosesTheWindow()
    {
        WindowsAutoStart.RunKeyPath = new string('x', 300);
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.StartWithWindows }));
        Switch(form, "_startSwitch").Checked = true;

        Assert.Null(Record.Exception(() => Find(form, "_saveButton").PerformClick()));
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void AChangeThatIsTakenBackDoesNotTakeBackWhatWasChosenInAnotherWindowInTheMeantime()
    {
        OptionsForm first = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        ClickTheme(first);
        Find(first, "_saveButton").PerformClick();
        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));

        // A second window starts from what is applied now (light) and takes back only what it changed itself.
        OptionsForm second = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        Assert.Equal(UIStyles.Symbols.Moon, ThemeButton(second).Text);
        Find(second, "_cancelButton").PerformClick();

        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Fact]
    public void TheWindow_FollowsAChangeOfTheTheme_InItsCardTheHostAndTheButtonBar()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        Panel bar = form.GetPrivateField<Panel>("_buttonBar")!;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(UIColors.BackgroundDark.ToArgb(), form.PropertyTable.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), ScrollHost(form).BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), bar.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), form.ContentPanel.BackColor.ToArgb());
    }

    [Fact]
    public void TheTabOrder_GoesThroughTheTableFirst_AndThenTheButtons()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        Panel bar = form.GetPrivateField<Panel>("_buttonBar")!;
        Panel host = ScrollHost(form);

        Assert.True(form.ContentPanel.Controls.IndexOf(host) < form.ContentPanel.Controls.IndexOf(bar), "the buttons come before the table in the tab order");
    }

    // ---- cleaning up

    private static int Listeners(Type type, string eventName)
    {
        FieldInfo? field = type.GetField(eventName, BindingFlags.NonPublic | BindingFlags.Static);

        return ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    [Fact]
    public void ADisposedWindow_ListensToNeitherTheThemeNorTheLanguageAnyMore()
    {
        int themeBefore = Listeners(typeof(UIColors), "ThemeChanged");
        int languageBefore = Listeners(typeof(UIStrings), "LanguageChanged");

        OptionsForm form = UIStyles.Forms.CreateOptionsStandard(presets: AllPresets);
        Assert.True(Listeners(typeof(UIColors), "ThemeChanged") > themeBefore);
        Assert.True(Listeners(typeof(UIStrings), "LanguageChanged") > languageBefore);

        form.Dispose();

        Assert.Equal(themeBefore, Listeners(typeof(UIColors), "ThemeChanged"));
        Assert.Equal(languageBefore, Listeners(typeof(UIStrings), "LanguageChanged"));
        Assert.Null(Record.Exception(() => UIColors.ApplyTheme(UIThemes.Light)));
    }
    // ---- language

    [Theory]
    [InlineData(UILanguage.English)]
    [InlineData(UILanguage.German)]
    public void EveryText_FitsInItsPlace_InBothLanguages(UILanguage language)
    {
        UIStrings.Language = language;
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: AllPresets));
        form.FitToContent();
        TestPictures.Pump(60);

        foreach (Label label in PropertyTableHost.AllLabels(form.PropertyTable))
        {
            int needed = TextRenderer.MeasureText(label.Text, label.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            Assert.True(needed <= label.Width, language + ": '" + label.Text + "' needs " + needed + " px of " + label.Width);
        }

        foreach (string field in new[] { "_saveButton", "_cancelButton", "_resetButton" })
        {
            Button button = Find(form, field);
            int needed = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            Assert.True(needed <= button.Width, language + ": '" + button.Text + "' needs " + needed + " px of " + button.Width);
        }

        Assert.True(form.PreferredSize.Width <= form.Width || form.Width >= form.MinimumSize.Width);
    }

    [Fact]
    public void EveryTextOfTheWindow_ExistsInBothLanguages()
    {
        string[] keys = { "Title", "Save", "Cancel", "General", "AccentColor", "Language", "AlwaysOnTop", "StartWithWindows", "ResetToDefaults", "Saved", "DefaultsRestored", "SwitchToLight", "SwitchToDark" };

        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        {
            UIStrings.Language = language;

            foreach (string key in keys)
                Assert.False(string.IsNullOrWhiteSpace(UIStrings.Get("OptionsForm." + key)) || UIStrings.Get("OptionsForm." + key) == "OptionsForm." + key, language + ": " + key);
        }

        foreach (string accent in new[] { "Blue", "Red", "Orange", "Amber", "Yellow", "Green", "Teal", "Cyan", "Indigo", "Purple", "Magenta", "Pink", "Brown", "Gray", "BlackOrWhite" })
        {
            UIStrings.Language = UILanguage.German;
            Assert.NotEqual("OptionsForm.Accent." + accent, UIStrings.Get("OptionsForm.Accent." + accent));
        }

        UIStrings.Language = UILanguage.German;
        Assert.NotEqual("Save", UIStrings.Get("OptionsForm.Save"));
    }
    // ---- the theme button in the corner

    [Fact]
    public void TheTheme_IsAButtonInTheTopRightCorner_AndNotARowOfTheTable()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        Button button = ThemeButton(form);
        Control strip = button.Parent!;

        Assert.Empty(RowLabels(form));
        Assert.Equal(DockStyle.Top, strip.Dock);
        Assert.Equal(strip.ClientSize.Width, button.Right);
        Assert.True(strip.Top < ScrollHost(form).Top, "the corner is above the table");
        Assert.False(button.TabStop);
    }

    [Fact]
    public void WithoutTheThemeOption_ThereIsNoCornerAndNoSpaceForIt()
    {
        OptionsForm with = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language }));
        OptionsForm without = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language }));
        OptionsForm corner = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language, OptionsPreset.Theme }));
        with.FitToContent();
        corner.FitToContent();

        Assert.Null(with.GetPrivateField<Button>("_themeButton"));
        Assert.True(corner.ClientSize.Height > with.ClientSize.Height);
        Assert.Null(without.GetPrivateField<Button>("_themeStrip"));
    }

    [Fact]
    public void TheButtonShowsTheThemeItSwitchesTo_WhoeverChangedTheTheme()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        Assert.Equal(UIStyles.Symbols.Sun, ThemeButton(form).Text);
        Assert.Equal("Switch to light theme", ThemeButton(form).AccessibleName);

        ClickTheme(form);

        Assert.Equal(UIStyles.Symbols.Moon, ThemeButton(form).Text);
        Assert.Equal("Switch to dark theme", ThemeButton(form).AccessibleName);

        // Changed from outside (a toolbar of the program): the button follows.
        UIColors.ApplyTheme(UIThemes.Dark);
        Assert.Equal(UIStyles.Symbols.Sun, ThemeButton(form).Text);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Zum hellen Design wechseln", ThemeButton(form).AccessibleName);
    }

    [Fact]
    public void ClickingTwice_GoesBack_AndCancelTakesTheThemeBack_AndResetGoesToDark()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme, OptionsPreset.ResetToDefaults }));

        ClickTheme(form);
        Assert.True(UIColors.IsLight(UIColors.BackgroundMedium));
        ClickTheme(form);
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));

        ClickTheme(form);
        Find(form, "_resetButton").PerformClick();
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));

        ClickTheme(form);
        Find(form, "_cancelButton").PerformClick();
        Assert.False(UIColors.IsLight(UIColors.BackgroundMedium));
    }

    [Fact]
    public void TheButton_StaysInTheCornerWhenTheWindowIsResized_AndOnAnotherDpi()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Theme }));
        Button button = ThemeButton(form);
        int size96 = button.Height;

        form.ClientSize = new Size(form.ClientSize.Width + 120, form.ClientSize.Height);
        Application.DoEvents();

        Assert.Equal(button.Parent!.ClientSize.Width, button.Right);

        form.DpiOverride = 192;
        Application.DoEvents();

        Assert.Equal(size96 * 2, button.Height);
        Assert.Equal(button.Parent.ClientSize.Width, button.Right);
        Assert.Equal((button.Parent.ClientSize.Height - button.Height) / 2, button.Top);
    }
    // ---- both themes

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void EverythingInTheWindow_CanBeReadAndSeen_InBothThemes(bool lightTheme)
    {
        ColorContrastTestSupport.Run(lightTheme, shortfalls =>
        {
            foreach (bool primary in new[] { false, true })
            {
                OptionsForm form = _host.Show(primary
                    ? UIStyles.Forms.CreateOptionsPrimary(presets: AllPresets)
                    : UIStyles.Forms.CreateOptionsStandard(presets: AllPresets));
                string window = primary ? "primary" : "standard";
                Color card = form.PropertyTable.BackColor;

                foreach (Label label in PropertyTableHost.AllLabels(form.PropertyTable))
                {
                    Color back = label.BackColor.A == 0 ? card : label.BackColor;
                    ColorContrastTestSupport.Check(shortfalls, window + ", label '" + label.Text + "'", label.ForeColor, back, 4.5);
                }

                foreach (string field in new[] { "_accentBox", "_languageBox" })
                {
                    System.Windows.Forms.ComboBox box = Box(form, field);
                    ColorContrastTestSupport.Check(shortfalls, window + ", " + field + " text", box.ForeColor, box.BackColor, 4.5);
                    ColorContrastTestSupport.Check(shortfalls, window + ", " + field + " against the card", box.BackColor, card, 1.1);
                }

                foreach (string field in new[] { "_saveButton", "_cancelButton", "_resetButton", "_themeButton" })
                    ColorContrastTestSupport.Check(shortfalls, window + ", " + field + " text", Find(form, field).ForeColor, Find(form, field).BackColor, 4.5);

                ColorContrastTestSupport.Check(shortfalls, window + ", the card against the window", card, ScrollHost(form).BackColor, 1.03);
                ColorContrastTestSupport.Check(shortfalls, window + ", the button bar against the card", form.GetPrivateField<Panel>("_buttonBar")!.BackColor, card, 1.03);
            }
        });
    }

    private static readonly OptionsPreset[] AllPresets =
    {
        OptionsPreset.Theme, OptionsPreset.AccentColor, OptionsPreset.Language, OptionsPreset.AlwaysOnTop, OptionsPreset.StartWithWindows, OptionsPreset.ResetToDefaults
    };

    [Fact]
    public void TheFactories_TakeAColorSetOrTheAccent_ForTheTitleBar()
    {
        OptionsForm standard = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm yellow = _host.Add(UIStyles.Forms.CreateOptionsStandard("Yellow", UIStyles.Colors.Sets.Yellow));
        OptionsForm primary = _host.Show(UIStyles.Forms.CreateOptionsPrimary());

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), standard.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void TheWindowOfYourOwn_CanBeMadeFromOptions_AndNullOptionsAreASettingsWindow()
    {
        FormOptions options = FormOptions.CreateSettingsPrimary("Own");
        options.RememberBoundsKey = "options";

        OptionsForm own = _host.Add(new OptionsForm(options));
        OptionsForm none = _host.Add(new OptionsForm((FormOptions)null!));

        Assert.Equal("Own", own.Text);
        Assert.Equal("options", own.RememberBoundsKey);
        Assert.NotNull(none.VersionStrip);
    }

    [Fact]
    public void TheOptionsWindow_StopsListeningToTheLanguage_WhenItIsDisposed()
    {
        FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", BindingFlags.NonPublic | BindingFlags.Static);
        int Listeners() => ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
        int before = Listeners();

        OptionsForm form = UIStyles.Forms.CreateOptionsStandard();
        Assert.True(Listeners() > before);

        form.Dispose();

        Assert.Equal(before, Listeners());
    }

#pragma warning disable CS0618
    [Fact]
    public void TheOldName_StyledOptionsForm_StillMakesAnOptionsWindow()
    {
        using StyledOptionsForm old = new StyledOptionsForm("Old");

        Assert.Equal("Old", old.Text);
        Assert.NotNull(old.PropertyTable);
    }
#pragma warning restore CS0618
}
