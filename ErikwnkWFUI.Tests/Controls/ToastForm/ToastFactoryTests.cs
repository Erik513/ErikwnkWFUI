using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The look of the toast through UIStyles.Toasts: the neutral gray without a color set, the
/// set's color with one, the accent for ShowPrimary, white or black text, and a background kept
/// readable through the transparency - the same rules as the info popup. [Collection]: the sets
/// read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastFactoryTests : IDisposable
{
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastFactoryTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();
        _host?.Dispose();
    }

    private Form Host()
    {
        _host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(500, 300)
        };
        _host.Show();

        return _host;
    }

    private static ToastForm? Current()
    {
        FieldInfo field = typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (ToastForm?)field.GetValue(null);
    }

    private static Color MessageColor(ToastForm toast) => toast.GetPrivateField<Label>("_messageLabel")!.ForeColor;

    private static Color IconColor(ToastForm toast) => toast.GetPrivateField<Label>("_iconLabel")!.ForeColor;

    // What shows on the screen: the window, and the text on it, blended with the page behind.
    private static double Effective(ToastForm toast, Color page)
    {
        int alpha = (int)Math.Round(toast.Opacity * 255);
        Color seenBack = ColorContrastHelper.Composite(Color.FromArgb(alpha, toast.BackColor), page);
        Color seenText = ColorContrastHelper.Composite(Color.FromArgb(alpha, MessageColor(toast)), page);

        return ColorContrastHelper.GetContrastRatio(seenText, seenBack);
    }

    [Fact]
    public void ShowStandard_IsTheNeutralGray_WithWhiteText()
    {
        UIStyles.Toasts.ShowStandard("Done", Host());
        ToastForm toast = Current()!;

        Assert.Null(toast.Colors);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic, toast.BackColor);
        Assert.Equal(Color.White, MessageColor(toast));
        Assert.Equal(Color.White, IconColor(toast));
    }

    [Fact]
    public void WithASet_TheBackgroundIsItsColor_AndTheTextWhiteOrBlack()
    {
        Form host = Host();
        UIColorSet green = UIStyles.Colors.Sets.Green;

        UIStyles.Toasts.ShowStandard("Done", host, green);
        ToastForm toast = Current()!;
        Assert.Same(green, toast.Colors);
        Assert.Equal(PopupSupport.GetBackground(green, toast.Opacity), toast.BackColor);
        Assert.Equal(Color.White, MessageColor(toast));

        // A light color is too light for white text: black.
        UIStyles.Toasts.ShowStandard("Done", host, UIStyles.Colors.CreateSet(Color.White));
        Assert.Equal(Color.Black, MessageColor(Current()!));
        Assert.Equal(Color.Black, IconColor(Current()!));
    }

    [Fact]
    public void ShowPrimary_IsTheStandardWithThePrimarySet_OfTheAccentAtTheTime()
    {
        Color accent = UIColors.Primary;

        try
        {
            Form host = Host();
            UIStyles.Toasts.ShowPrimary("Done", host);
            Color blue = Current()!.BackColor;
            Assert.Equal(PopupSupport.GetBackground(UIColors.PrimarySet, Current()!.Opacity), blue);

            UIColors.SetAccent(UIAccentColors.Purple);
            UIStyles.Toasts.ShowPrimary("Done", host);

            Assert.NotEqual(blue, Current()!.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Fact]
    public void AChangedSetOrTransparency_BringsTheBackgroundAndTheTextAlong()
    {
        UIStyles.Toasts.ShowStandard("Done", Host(), UIStyles.Colors.Sets.Orange, PopupTransparency.Opaque);
        ToastForm toast = Current()!;
        Assert.Equal(UIStyles.Colors.Sets.Orange.Basic, toast.BackColor);

        toast.Transparency = PopupTransparency.High;
        Assert.NotEqual(UIStyles.Colors.Sets.Orange.Basic, toast.BackColor);

        toast.Colors = UIStyles.Colors.CreateSet(Color.White);
        Assert.Equal(Color.Black, MessageColor(toast));

        toast.Colors = null;
        Assert.Equal(Color.White, MessageColor(toast));
    }

    [Fact]
    public void AWaitingToast_KeepsItsColors()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;
        UIStyles.Toasts.ShowStandard("Waits", host, UIStyles.Colors.Sets.Teal);

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

        Assert.Same(UIStyles.Colors.Sets.Teal, Current()!.Colors);
    }

    [Fact]
    public void EverySetAtEveryLevel_ReadsOverAWhitePageAndABlackOne()
    {
        Form host = Host();
        List<string> shortfalls = new List<string>();

        foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                UIStyles.Toasts.ShowStandard("Done", host, (UIColorSet)property.GetValue(null)!, level);
                ToastForm toast = Current()!;

                foreach (Color page in new[] { Color.White, Color.Black })
                {
                    double ratio = Effective(toast, page);

                    if (ratio < 4.49)
                        shortfalls.Add(property.Name + " " + level + " over " + page.Name + " " + ratio.ToString("0.0"));
                }
            }
        }

        Assert.True(shortfalls.Count == 0, string.Join(", ", shortfalls));
    }

    [Fact]
    public void TheTitleAndTheSymbol_ReadLikeTheMessage_EverySetEveryLevel()
    {
        Form host = Host();
        List<string> shortfalls = new List<string>();

        foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                ToastForm.ShowToast(new InfoPopupSection("Title", "Message"), host, (UIColorSet)property.GetValue(null)!, level, icon: ToastIcon.Warning);
                ToastForm toast = Current()!;

                foreach (string name in new[] { "_titleLabel", "_iconLabel" })
                {
                    Color text = toast.GetPrivateField<Label>(name)!.ForeColor;

                    Assert.Equal(MessageColor(toast), text);

                    foreach (Color page in new[] { Color.White, Color.Black })
                    {
                        int alpha = (int)Math.Round(toast.Opacity * 255);
                        double ratio = ColorContrastHelper.GetContrastRatio(
                            ColorContrastHelper.Composite(Color.FromArgb(alpha, text), page),
                            ColorContrastHelper.Composite(Color.FromArgb(alpha, toast.BackColor), page));

                        if (ratio < 4.49)
                            shortfalls.Add(name + " " + property.Name + " " + level + " over " + page.Name + " " + ratio.ToString("0.0"));
                    }
                }
            }
        }

        Assert.True(shortfalls.Count == 0, string.Join(", ", shortfalls));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheToast_LooksTheSame_InBothThemes(bool lightTheme)
    {
        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            UIStyles.Toasts.ShowStandard("Done", Host(), UIStyles.Colors.Sets.Orange);
            Color dark = Current()!.BackColor;
            Current()!.Dispose();

            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            UIStyles.Toasts.ShowStandard("Done", _host!, UIStyles.Colors.Sets.Orange);

            Assert.Equal(dark, Current()!.BackColor);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
