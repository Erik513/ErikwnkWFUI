using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The edge cases of the panels: no size, no window, a window that is made again, a control that goes away.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelRobustnessTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private Form Host(Control control, Size size)
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 240), BackColor = Color.Black };
        control.Location = new Point(10, 10);

        if (control.Dock == DockStyle.Fill || control.Dock == DockStyle.Bottom)
            control.Dock = DockStyle.None;

        control.Size = size;
        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return host;
    }

    // A control swallows an error while it paints and shows a red cross: the painting is called directly.
    private static void PaintDirectly(Control control)
    {
        using Bitmap picture = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));
        using Graphics graphics = Graphics.FromImage(picture);
        PaintEventArgs args = new PaintEventArgs(graphics, new Rectangle(Point.Empty, control.Size));
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        try
        {
            control.GetType().GetMethod("OnPaintBackground", flags)!.Invoke(control, new object[] { args });
            control.GetType().GetMethod("OnPaint", flags)!.Invoke(control, new object[] { args });
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException!;
        }
    }

    private static ThemedTableLayoutPanel Lines(CellBorderLines lines, bool transparent)
    {
        ThemedTableLayoutPanel table = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
        table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
        table.CellBorderLines = lines;

        if (transparent)
            table.BackColor = Color.Transparent;

        for (int i = 0; i < 3; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

        for (int i = 0; i < 2; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        return table;
    }

    // One cell only: there is nothing between the cells, or the lines around the table are the only ones.
    private static ThemedTableLayoutPanel OneCell(CellBorderLines lines)
    {
        ThemedTableLayoutPanel table = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(1, 1);
        table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
        table.CellBorderLines = lines;

        return table;
    }

    private static MultiPanel Pages(MultiPanel panel)
    {
        panel.AddPage("A long title for the narrow header");
        panel.AddPage("B");
        panel.ShowPageCount = true;
        panel.ShowHeaderLine = true;

        return panel;
    }

    public static IEnumerable<object[]> Panels()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("Panel", () => UIStyles.Panels.CreateMedium()),
            ("Card", () => UIStyles.Panels.CreateCardStandard()),
            ("Table", () => UIStyles.TableLayoutPanels.CreateStandard(2, 2)),
            ("Table, all lines", () => Lines(CellBorderLines.All, false)),
            ("Table, inner lines", () => Lines(CellBorderLines.InnerOnly, false)),
            ("Table, outer lines", () => Lines(CellBorderLines.OuterOnly, false)),
            ("Table, transparent, inner lines", () => Lines(CellBorderLines.InnerOnly, true)),
            ("Table, transparent, outer lines", () => Lines(CellBorderLines.OuterOnly, true)),
            ("Table, one cell, all lines", () => OneCell(CellBorderLines.All)),
            ("Table, one cell, inner lines", () => OneCell(CellBorderLines.InnerOnly)),
            ("Table, one cell, outer lines", () => OneCell(CellBorderLines.OuterOnly)),
            ("Flow", () => UIStyles.FlowLayoutPanels.CreateStandard()),
            ("MultiPanel", () => Pages(UIStyles.MultiPanels.CreateStandard())),
            ("MultiPanel, card", () => Pages(UIStyles.MultiPanels.CreateCardPrimary())),
            ("MultiPanel, without pages", () => UIStyles.MultiPanels.CreateStandard()),
            ("VersionPanel", () => UIStyles.VersionPanels.CreateStandard())
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void NoSize_OrASliverOfOne_IsPaintedWithoutAnError(string name, Func<Control> make)
    {
        foreach (Size size in new[] { new Size(0, 0), new Size(1, 1), new Size(3, 200), new Size(200, 3) })
        {
            Control control = make();
            Host(control, size);

            Exception? error = Record.Exception(() => PaintDirectly(control));

            Assert.True(error == null, $"{name} at {size}: {error}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ShowChangeAndDispose(Func<Control> make)
    {
        Control control = make();
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        host.Dispose();
        WeakReference reference = new WeakReference(control);
        UIColors.ApplyTheme(UIThemes.Dark);

        return reference;
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void AShownPanel_ThatIsDisposed_IsNotHeldByTheTheme(string name, Func<Control> make)
    {
        Color accent = UIColors.Primary;

        try
        {
            WeakReference reference = ShowChangeAndDispose(make);

            for (int i = 0; i < 3 && reference.IsAlive; i++)
            {
                Application.DoEvents();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(reference.IsAlive, name + " is still held after it was disposed");
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    public static IEnumerable<object[]> ThemedPanels()
    {
        yield return new object[] { "Panel", (Func<Control>)(() => UIStyles.Panels.CreateElevated()) };
        yield return new object[] { "Card", (Func<Control>)(() => UIStyles.Panels.CreateCardStandard()) };
        yield return new object[] { "Table", (Func<Control>)(() => UIStyles.TableLayoutPanels.CreateStandard(1, 1)) };
        yield return new object[] { "Flow", (Func<Control>)(() => UIStyles.FlowLayoutPanels.CreateStandard()) };
        yield return new object[] { "MultiPanel", (Func<Control>)(() => Pages(UIStyles.MultiPanels.CreateStandard())) };
        yield return new object[] { "VersionPanel", (Func<Control>)(() => UIStyles.VersionPanels.CreateStandard()) };
    }

    private static string Look(Control control)
    {
        using Bitmap picture = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(picture, new Rectangle(Point.Empty, control.Size));
        StringBuilder look = new StringBuilder(control.BackColor.ToArgb().ToString());

        for (int x = 0; x < picture.Width; x += 3)
            for (int y = 0; y < picture.Height; y += 3)
                look.Append(picture.GetPixel(x, y).ToArgb()).Append(',');

        return look.ToString();
    }

    [Theory]
    [MemberData(nameof(ThemedPanels))]
    public void ThePanelThatIsMadeBeforeItIsShown_TakesTheThemeOfTheTimeItIsShown(string name, Func<Control> make)
    {
        // Made in the dark theme, never had a window while the theme changed, shown in the light one.
        Control early = make();
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Host(early, new Size(120, 60));

        Control late = make();
        Host(late, new Size(120, 60));

        Assert.Equal(Look(late), Look(early));
    }

    [Theory]
    [MemberData(nameof(ThemedPanels))]
    public void AWindowThatIsMadeAgain_DoesNotMakeAPanelDeaf_ToTheTheme(string name, Func<Control> make)
    {
        Control panel = make();
        Form host = Host(panel, new Size(120, 60));

        // The handle of the form and of everything in it is made again (the taskbar button is one reason).
        host.ShowInTaskbar = true;
        Application.DoEvents();
        host.ShowInTaskbar = false;
        Application.DoEvents();

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Control fresh = make();
        Host(fresh, new Size(120, 60));

        Assert.Equal(Look(fresh), Look(panel));
    }

    // Answers after a wait that the test ends itself.
    private sealed class SlowGitHub : HttpMessageHandler
    {
        public TaskCompletionSource<bool> Gate { get; } = new TaskCompletionSource<bool>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Gate.Task.ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"tag_name\":\"v99.0.0\",\"html_url\":\"https://example.com/r\",\"assets\":[]}", Encoding.UTF8, "application/json")
            };
        }
    }

    [Fact]
    public void AVersionPanel_ThatIsDisposedWhileItChecksForAnUpdate_GetsNoButton_AndNoError()
    {
        SlowGitHub github = new SlowGitHub();
        using HttpClient client = new HttpClient(github);
        AppUpdater updater = new AppUpdater("owner", "repo", client, client);
        VersionPanel panel = UIStyles.VersionPanels.CreateStandard();
        Host(panel, new Size(200, 24));

        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        try
        {
            Task<bool> check = panel.CheckForUpdateAsync(updater, TimeSpan.FromSeconds(10), new Version(1, 0, 0));
            panel.Dispose();
            github.Gate.SetResult(true);

            DateTime end = DateTime.UtcNow.AddSeconds(10);

            while (!check.IsCompleted && DateTime.UtcNow < end)
            {
                Application.DoEvents();
                Thread.Sleep(5);
            }

            Assert.True(check.IsCompleted);
            Assert.False(check.GetAwaiter().GetResult());
            Assert.False(panel.UpdateAvailable);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    // The red lines of a table: where they are along a row (y) / a column (x).
    private static List<int> Red(Bitmap picture, bool along, int fixedCoordinate)
    {
        int length = along ? picture.Width : picture.Height;

        return Enumerable.Range(0, length)
            .Where(i => (along ? picture.GetPixel(i, fixedCoordinate) : picture.GetPixel(fixedCoordinate, i)).ToArgb() == Color.Red.ToArgb())
            .ToList();
    }

    private ThemedTableLayoutPanel AbsoluteTable(CellBorderLines lines, bool scrolling)
    {
        ThemedTableLayoutPanel table = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 3);
        table.AutoScroll = scrolling;
        table.BackColor = Color.White;
        table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
        table.CellBorderColor = Color.Red;
        table.CellBorderLines = lines;

        for (int i = 0; i < 3; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        }

        return table;
    }

    [Theory]
    [InlineData(CellBorderLines.All)]
    [InlineData(CellBorderLines.InnerOnly)]
    [InlineData(CellBorderLines.OuterOnly)]
    public void ATableThatIsScrolled_HasItsLinesWhereTheCellsAre_AndOnlyTheOnesThatAreChosen(CellBorderLines lines)
    {
        // The whole table, not scrolled: where its vertical lines are.
        ThemedTableLayoutPanel whole = AbsoluteTable(lines, false);
        Host(whole, new Size(300, 200));
        using Bitmap wholePicture = new Bitmap(whole.Width, whole.Height);
        whole.DrawToBitmap(wholePicture, new Rectangle(Point.Empty, whole.Size));
        List<int> wholeColumns = Red(wholePicture, true, 5);

        Assert.Equal(lines == CellBorderLines.All ? 4 : 2, wholeColumns.Count);

        // The same table in a window that shows only a part of it, scrolled to the right.
        ThemedTableLayoutPanel part = AbsoluteTable(lines, true);
        Host(part, new Size(120, 80));
        part.AutoScrollPosition = new Point(40, 0);
        Application.DoEvents();
        Assert.Equal(40, -part.AutoScrollPosition.X);

        using Bitmap partPicture = new Bitmap(part.Width, part.Height);
        part.DrawToBitmap(partPicture, new Rectangle(Point.Empty, part.Size));

        List<int> expected = wholeColumns.Select(x => x - 40).Where(x => x >= 0 && x < part.ClientSize.Width).ToList();

        Assert.Equal(expected, Red(partPicture, true, 5));
    }
}
