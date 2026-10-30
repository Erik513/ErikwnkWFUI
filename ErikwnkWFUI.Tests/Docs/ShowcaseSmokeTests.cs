using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TabControl = System.Windows.Forms.TabControl;

namespace ErikwnkWFUI.Tests.Docs;

/// <summary>
/// The Showcase, which is the guide of the library, builds and paints every one of its pages without an exception, in both themes and both
/// languages. It is loaded from its own build (the project is not referenced by the tests: build it first, or the test says so by being left out),
/// shown off the screen, and each tab - and each page of a tab inside a tab, and of a multi panel - is selected and painted.
/// [Collection]: the theme, the accent and the language are changed.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ShowcaseSmokeTests : IDisposable
{
    private readonly UILanguage _language = UIStrings.Language;
    private readonly UIColorTheme _theme = UIColors.IsLight(UIColors.BackgroundMedium) ? UIThemes.Light : UIThemes.Dark;

    public void Dispose()
    {
        UIStrings.Language = _language;
        UIColors.ApplyTheme(_theme);
    }

    private static string? FindShowcase()
    {
        string? folder = AppContext.BaseDirectory;

        for (int i = 0; i < 8 && folder != null; i++)
        {
            string candidate = Path.Combine(folder, "ErikwnkWFUI.Showcase", "bin", "Debug", "net8.0-windows", "ErikwnkWFUI.Showcase.dll");

            if (File.Exists(candidate))
                return candidate;

            folder = Path.GetDirectoryName(folder);
        }

        return null;
    }

    private static int SelectAndPaint(Control control, List<Exception> problems)
    {
        int painted = 0;

        foreach (TabControl tabs in Descendants<TabControl>(control))
        {
            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                tabs.SelectedIndex = i;
                Application.DoEvents();
                painted++;
            }

            tabs.SelectedIndex = 0;
        }

        foreach (MultiPanel book in Descendants<MultiPanel>(control))
        {
            for (int i = 0; i < book.PageCount; i++)
            {
                book.SelectedIndex = i;
                Application.DoEvents();
                painted++;
            }
        }

        return painted;
    }

    private static IEnumerable<T> Descendants<T>(Control control) where T : Control
    {
        foreach (Control child in control.Controls)
        {
            if (child is T match)
                yield return match;

            foreach (T inner in Descendants<T>(child))
                yield return inner;
        }
    }

    [Fact]
    public void EveryPageOfTheShowcase_BuildsAndPaints_InBothThemesAndBothLanguages()
    {
#if NETFRAMEWORK
        // (The Showcase is a program for .NET 8 only: it cannot be loaded into the .NET Framework.)
        return;
#else
        string? path = FindShowcase();

        Assert.True(path != null, "ErikwnkWFUI.Showcase is not built (build the solution first)");
        if (path == null)
            return;

        Assembly showcase = Assembly.LoadFrom(path);
        Type type = showcase.GetType("ErikwnkWFUI.Showcase.ShowcaseForm", throwOnError: true)!;
        List<Exception> problems = new List<Exception>();
        ThreadExceptionEventHandler handler = (sender, e) => problems.Add(e.Exception);
        Application.ThreadException += handler;
        int pages = 0;

        try
        {
            using Form form = (Form)Activator.CreateInstance(type)!;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, 0);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();

            foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
            {
                foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
                {
                    UIStrings.Language = language;
                    UIColors.ApplyTheme(theme);
                    Application.DoEvents();

                    try
                    {
                        pages += SelectAndPaint(form, problems);

                        using Bitmap picture = new Bitmap(form.Width, form.Height);
                        form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
                    }
                    catch (Exception ex)
                    {
                        problems.Add(new InvalidOperationException(language + " / " + (theme == UIThemes.Light ? "light" : "dark") + ": " + ex.Message, ex));
                    }

                    Application.DoEvents();
                }
            }

            // The accent bar: every accent color, in both themes, with the pages of the whole window painted again.
            foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
            {
                UIColors.ApplyTheme(theme);

                foreach (Color accent in new[] { UIAccentColors.Blue, UIAccentColors.Red, UIAccentColors.Yellow, UIAccentColors.Gray, UIAccentColors.BlackOrWhite })
                {
                    UIColors.SetAccent(accent);
                    Application.DoEvents();

                    using Bitmap picture = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
                }
            }

            UIColors.SetAccent(UIAccentColors.Blue);
        }
        finally
        {
            Application.ThreadException -= handler;
        }

        Assert.True(pages > 20, "the pages were not found: " + pages);
        Assert.True(problems.Count == 0, string.Join("\n", problems.Select(p => p.GetType().Name + ": " + p.Message + "\n" + p.StackTrace)));
#endif
    }
}
