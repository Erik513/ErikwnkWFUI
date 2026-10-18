using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;
using ProgressBar = System.Windows.Forms.ProgressBar;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The panels, labels, progress bars, spinners, sliders and toggle switches follow a change of the theme or the accent
/// while they are shown: afterwards they look as one that is made after the change. What was set by hand stays.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeFollowSimpleControlsTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private T Show<T>(T control) where T : Control
    {
        if (control.Dock == DockStyle.None)
            control.Bounds = new Rectangle(5, 5, 120, 40);

        control.TabStop = false; // the form that is shown last is the active one: no focus cue on only one of two
        Form form = OffscreenForm.Create(new Size(140, 60), Color.FromArgb(40, 90, 140));
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    // Colors, and for the controls that paint themselves the picture too.
    private static string Look(Control control, bool picture)
    {
        if (control is ProgressBar)
            control.Refresh(); // the system's bar is painted now, not whenever the message loop gets to it
        string look = $"{control.BackColor.ToArgb()} {control.ForeColor.ToArgb()}";

        if (!picture)
            return look;

        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
        byte[] bytes = new byte[bitmap.Width * bitmap.Height * 4];
        int i = 0;

        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                int argb = bitmap.GetPixel(x, y).ToArgb();
                bytes[i++] = (byte)argb;
                bytes[i++] = (byte)(argb >> 8);
                bytes[i++] = (byte)(argb >> 16);
                bytes[i++] = (byte)(argb >> 24);
            }

        using SHA256 sha = SHA256.Create();

        return look + " " + Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    // The ones with no background of their own and the ones in a color set (a set is the same in every theme): they look the
    // same whatever the theme, and are not told to paint again. A volume control is a transparent panel whose parts (the button and the
    // slider) paint themselves: the panel itself paints nothing (its parts are looked at by the tests of the track bar and the control).
    private static readonly HashSet<string> LooksTheSameInEveryTheme = new HashSet<string>
    {
        "Panels.Transparent",
        "Panels.Standard(colors)",
        "FlowLayoutPanels.Transparent",
        "FlowLayoutPanels.Standard(colors)",
        "VersionPanels.Standard(colors)",
        "VolumeControls.Primary"
    };

    // A flow panel is as big as its content: with nothing in it it has no size to look at.
    private static Control WithContent(System.Windows.Forms.FlowLayoutPanel panel)
    {
        panel.Controls.Add(new Label { Text = "Text", AutoSize = false, Size = new Size(60, 20) });

        return panel;
    }

    // A multi panel with its first page shown (and a title, so that the header has a text).
    private static Control WithPages(ErikwnkWFUI.Controls.MultiPanel panel)
    {
        panel.AddPage("First");
        panel.AddPage("Second");
        panel.ShowPageCount = true;
        panel.ShowHeaderLine = true;

        return panel;
    }

    // name, how it is made, whether its picture can be compared (the system's own progress bar and the turning
    // spinner cannot).
    public static IEnumerable<object[]> Controls()
    {
        foreach (var entry in new (string, Func<Control>, bool)[]
        {
            ("Panels.Standard", () => UIStyles.Panels.CreateStandard(), true),
            ("Panels.Dark", () => UIStyles.Panels.CreateDark(), true),
            ("Panels.Medium", () => UIStyles.Panels.CreateMedium(), true),
            ("Panels.Elevated", () => UIStyles.Panels.CreateElevated(), true),
            ("Panels.Primary", () => UIStyles.Panels.CreatePrimary(), true),
            ("TableLayoutPanels.Standard", () => UIStyles.TableLayoutPanels.CreateStandard(1, 1), true),
            ("TableLayoutPanels.Dark", () => UIStyles.TableLayoutPanels.CreateDark(1, 1), true),
            ("Panels.Transparent", () => UIStyles.Panels.CreateTransparent(), true),
            ("Panels.Standard(colors)", () => UIStyles.Panels.CreateStandard(UIStyles.Colors.Sets.Green), true),
            ("Panels.CardStandard", () => UIStyles.Panels.CreateCardStandard(), true),
            ("Panels.CardPrimary", () => UIStyles.Panels.CreateCardPrimary(), true),
            ("Panels.CardStandard(colors)", () => UIStyles.Panels.CreateCardStandard(UIStyles.Colors.Sets.Green), true),
            ("TableLayoutPanels.Primary", () => UIStyles.TableLayoutPanels.CreatePrimary(1, 1), true),
            ("TableLayoutPanels.Transparent", () => UIStyles.TableLayoutPanels.CreateTransparent(1, 1), true),
            ("TableLayoutPanels.Standard(colors)", () => UIStyles.TableLayoutPanels.CreateStandard(1, 1, UIStyles.Colors.Sets.Green), true),
            ("FlowLayoutPanels.Standard", () => WithContent(UIStyles.FlowLayoutPanels.CreateStandard()), true),
            ("FlowLayoutPanels.Primary", () => WithContent(UIStyles.FlowLayoutPanels.CreatePrimary()), true),
            ("FlowLayoutPanels.Transparent", () => UIStyles.FlowLayoutPanels.CreateTransparent(), true),
            ("FlowLayoutPanels.Standard(colors)", () => WithContent(UIStyles.FlowLayoutPanels.CreateStandard(UIStyles.Colors.Sets.Green)), true),
            ("MultiPanels.Standard", () => WithPages(UIStyles.MultiPanels.CreateStandard()), true),
            ("MultiPanels.Primary", () => WithPages(UIStyles.MultiPanels.CreatePrimary()), true),
            ("MultiPanels.Standard(colors)", () => WithPages(UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Green)), true),
            ("MultiPanels.CardStandard", () => WithPages(UIStyles.MultiPanels.CreateCardStandard()), true),
            ("MultiPanels.CardPrimary", () => WithPages(UIStyles.MultiPanels.CreateCardPrimary()), true),
            ("VersionPanels.Standard", () => UIStyles.VersionPanels.CreateStandard(), true),
            ("VersionPanels.Primary", () => UIStyles.VersionPanels.CreatePrimary(), true),
            ("VersionPanels.Standard(colors)", () => UIStyles.VersionPanels.CreateStandard(UIStyles.Colors.Sets.Green), true),
            ("Labels.Standard", () => UIStyles.Labels.CreateStandard("Text"), true),
            ("Labels.Primary", () => UIStyles.Labels.CreatePrimary("Text"), true),
            ("Labels.Title", () => UIStyles.Labels.CreateTitle("Text"), true),
            ("Labels.Muted", () => UIStyles.Labels.CreateMuted("Text"), true),
            ("SlimProgressBars.Standard", () => { var b = UIStyles.SlimProgressBars.CreateStandard(); b.Value = 40; return b; }, true),
            ("SlimProgressBars.Primary", () => { var b = UIStyles.SlimProgressBars.CreatePrimary(); b.Value = 40; return b; }, true),
            ("SlimProgressBars.Status", () => { var b = UIStyles.SlimProgressBars.CreateStatus(); b.Value = 40; return b; }, true),
            ("SlimProgressBars.Standard(colors)", () => { var b = UIStyles.SlimProgressBars.CreateStandard(UIStyles.Colors.Sets.Teal); b.Value = 40; return b; }, true),
            ("ProgressBars.Standard", () => { var b = UIStyles.ProgressBars.CreateStandard(); b.Value = 40; return b; }, false),
            ("ProgressBars.Primary", () => { var b = UIStyles.ProgressBars.CreatePrimary(); b.Value = 40; return b; }, false),
            ("ProgressBars.Status", () => { var b = UIStyles.ProgressBars.CreateStatus(); b.Value = 40; return b; }, false),
            ("ProgressBars.Standard(colors)", () => { var b = UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Teal); b.Value = 40; return b; }, false),
            ("ProgressBars.Standard(BorderStyle None)", () => { var b = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(); b.BorderStyle = System.Windows.Forms.BorderStyle.None; b.Value = 40; return b; }, false),
            ("Spinners.Standard", () => UIStyles.Spinners.CreateStandard(), false),
            ("Spinners.Primary", () => UIStyles.Spinners.CreatePrimary(), false),
            ("TrackBars.Standard", () => UIStyles.TrackBars.CreateStandard(0.4), true),
            ("TrackBars.Primary", () => UIStyles.TrackBars.CreatePrimary(0.4), true),
            ("VolumeControls.Primary", () =>
            {
                VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.4);
                control.Slider.TabStop = false;   // no focus cue (a bigger thumb) on only one of two

                return control;
            }, true),
            ("TrackBars.PrimaryWithPopup", () => UIStyles.TrackBars.CreatePrimary(0.4, 1.0, showValuePopup: true, formatValue: ErikwnkWFUI.Controls.TrackBar.FormatPercent), true),
            ("ToggleSwitches.Standard", () => UIStyles.ToggleSwitches.CreateStandard(true), true),
            ("ToggleSwitches.Unchecked", () => UIStyles.ToggleSwitches.CreateStandard(false), true),
        })
            yield return new object[] { entry.Item1, entry.Item2, entry.Item3 };
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterTheChange_InBothDirections(string name, Func<Control> make, bool picture)
    {
        Control shown = Show(make());
        string dark = Look(shown, picture);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Control madeInLight = Show(make());

        Assert.Equal(Look(madeInLight, picture), Look(shown, picture));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Control madeInDark = Show(make());

        Assert.Equal(Look(madeInDark, picture), Look(shown, picture));
        Assert.Equal(dark, Look(shown, picture));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterAChangeOfTheAccent(string name, Func<Control> make, bool picture)
    {
        Control shown = Show(make());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();
        Control made = Show(make());

        Assert.Equal(Look(made, picture), Look(shown, picture));
    }

    // The picture can be taken at any time (DrawToBitmap paints with what is read then), so the look alone does not show
    // whether a shown control was told to paint again: it is counted (a window far off the screen is not painted at all).
    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_IsToldToPaintAgain_WhenTheThemeChanges(string name, Func<Control> make, bool picture)
    {
        if (!picture)
            return; // the system's progress bar paints natively, and the spinner turns anyway

        if (LooksTheSameInEveryTheme.Contains(name))
            return; // nothing that is painted depends on the theme, so there is nothing to paint again

        Control shown = Show(make());
        int paints = 0;
        shown.Invalidated += (s, e) => paints++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        shown.Update();
        Assert.True(paints > 0);
    }

    [Fact]
    public void AColorSetByHand_Stays()
    {
        Panel panel = Show(UIStyles.Panels.CreateDark());
        panel.BackColor = Color.Red;
        SlimProgressBar slim = Show(UIStyles.SlimProgressBars.CreatePrimary());
        slim.ForeColor = Color.Yellow;
        slim.BackColor = Color.Purple;
        ProgressBar bar = Show(UIStyles.ProgressBars.CreatePrimary());
        bar.ForeColor = Color.Yellow;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Red, panel.BackColor);
        Assert.Equal(Color.Yellow, slim.ForeColor);
        Assert.Equal(Color.Purple, slim.BackColor);
        Assert.Equal(Color.Yellow, bar.ForeColor);
    }

    [Fact]
    public void ADisabledProgressBar_KeepsItsGray_AndRestoresTheNewColor()
    {
        ProgressBar bar = Show(UIStyles.ProgressBars.CreatePrimary());
        bar.Enabled = false;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(UIColors.DisabledGray, bar.ForeColor);

        bar.Enabled = true;

        Assert.Equal(UIColors.Primary, bar.ForeColor);
    }

    [Fact]
    public void TheTitleBar_FollowsToo_ButNotAColorThatWasGiven()
    {
        TitleBarControl bar = Show(new TitleBarControl(null, "Title"));
        TitleBarControl given = Show(new TitleBarControl(null, "Title", backColor: Color.Navy));
        Color darkBack = bar.BackColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using TitleBarControl made = new TitleBarControl(null, "Title");
        Assert.NotEqual(darkBack, bar.BackColor);
        Assert.Equal(made.BackColor, bar.BackColor);
        Assert.Equal(Color.Navy, given.BackColor);
    }
}
