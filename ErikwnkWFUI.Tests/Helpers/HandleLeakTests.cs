using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Every control and window that <c>UIStyles</c> makes is made, shown (off the screen), painted and disposed again a hundred and twenty times, and
/// the GDI objects, the USER objects and the handles of the process are counted before and after: a control that keeps a pen, a font, a bitmap
/// or a window handle after it is disposed shows as a number that grows with the cycles, not as a few that a cache holds.
/// The factories are found by reflection (the ones with no parameters that must be given, or only a text), so a new one is tested without being
/// listed here. [Collection]: nothing else may make or free handles meanwhile.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class HandleLeakTests
{
    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);

    private const uint GdiObjects = 0;
    private const uint UserObjects = 1;
    private const int Cycles = 120;
    private const int Warmup = 8;

    // What a cache may hold (fonts, a shared bitmap) and what the system makes on its own; a leak of one handle per cycle is a hundred and twenty.
    private const int GdiTolerance = 40;
    private const int UserTolerance = 40;
    private const int HandleTolerance = 100;

    private static IEnumerable<MethodInfo> Factories()
    {
        foreach (Type type in typeof(UIStyles).GetNestedTypes(BindingFlags.Public).OrderBy(t => t.Name))
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static).OrderBy(m => m.Name))
            {
                if (!method.Name.StartsWith("Create", StringComparison.Ordinal) || method.IsGenericMethod || method.GetCustomAttribute<ObsoleteAttribute>() != null)
                    continue;

                if (!typeof(Control).IsAssignableFrom(method.ReturnType) && !typeof(Form).IsAssignableFrom(method.ReturnType))
                    continue;

                if (method.GetParameters().Any(p => !p.IsOptional && p.ParameterType != typeof(string)))
                    continue;

                yield return method;
            }
        }
    }

    public static IEnumerable<object[]> Names()
    {
        // (The signature is part of the name: some factories have overloads.)
        foreach (MethodInfo method in Factories())
            yield return new object[] { method.DeclaringType!.Name + "." + method.Name + "(" + string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name)) + ")" };
    }

    private static MethodInfo Find(string name)
    {
        return Factories().Single(m => m.DeclaringType!.Name + "." + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")" == name);
    }

    private static object? Make(MethodInfo method)
    {
        object?[] arguments = method.GetParameters().Select(p => p.ParameterType == typeof(string) && !p.IsOptional ? "1" : Type.Missing).ToArray();

        return method.Invoke(null, arguments);
    }

    private static void OneCycle(MethodInfo method)
    {
        object? made = Make(method);

        if (made is Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, 0);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            form.Dispose();
        }
        else if (made is ToolStripDropDown menu)
        {
            // (A menu is a window of its own, and cannot be put on a form.)
            menu.Show(new Point(-4000, 0));
            Application.DoEvents();
            menu.Close();
            menu.Dispose();
        }
        else if (made is Control control)
        {
            // (A control the factory makes with no size - it is docked or sized by its content - is given one, so that it can be painted into a picture.)
            if (control.Width <= 0 || control.Height <= 0)
                control.Size = new Size(Math.Max(control.Width, 120), Math.Max(control.Height, 40));

            using Form host = OffscreenForm.Show(control, new Size(Math.Max(control.Width + 20, 120), Math.Max(control.Height + 20, 80)), Color.Black, new Point(5, 5));
            Application.DoEvents();

            // (A control that sizes itself by its content has none while it is empty: nothing to paint into a picture then.)
            if (control.Width > 0 && control.Height > 0)
            {
                using Bitmap picture = TestPictures.Draw(control);
            }
        }

        Application.DoEvents();
    }

    private static (int Gdi, int User, int Handles) Count()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Application.DoEvents();
        }

        Process process = Process.GetCurrentProcess();
        process.Refresh();

        return ((int)GetGuiResources(process.Handle, GdiObjects), (int)GetGuiResources(process.Handle, UserObjects), process.HandleCount);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void MakingShowingAndDisposing_LeavesNoHandlesBehind(string name)
    {
        MethodInfo method = Find(name);

        for (int i = 0; i < Warmup; i++)
            OneCycle(method);

        (int gdi, int user, int handles) before = Count();

        for (int i = 0; i < Cycles; i++)
            OneCycle(method);

        (int gdi, int user, int handles) after = Count();

        Assert.True(after.gdi - before.gdi <= GdiTolerance, name + ": GDI objects " + before.gdi + " -> " + after.gdi + " in " + Cycles + " cycles");
        Assert.True(after.user - before.user <= UserTolerance, name + ": USER objects " + before.user + " -> " + after.user + " in " + Cycles + " cycles");
        Assert.True(after.handles - before.handles <= HandleTolerance, name + ": handles " + before.handles + " -> " + after.handles + " in " + Cycles + " cycles");
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr handle);

    [Fact]
    public void TheCounters_SeeAHandleThatIsNotGivenBack()
    {
        (int gdi, int user, int handles) before = Count();
        List<IntPtr> leaked = new List<IntPtr>();

        try
        {
            for (int i = 0; i < Cycles; i++)
            {
                using Bitmap bitmap = new Bitmap(4, 4);
                leaked.Add(bitmap.GetHbitmap());
            }

            (int gdi, int user, int handles) after = Count();

            Assert.True(after.gdi - before.gdi > GdiTolerance, "a hundred and twenty bitmaps that were not given back are not seen: " + before.gdi + " -> " + after.gdi);
        }
        finally
        {
            foreach (IntPtr handle in leaked)
                DeleteObject(handle);
        }
    }

    [Fact]
    public void TheFactoriesAreFound_SoThatTheTestsAboveAreNotEmpty()
    {
        Assert.True(Factories().Count() > 60, "only " + Factories().Count() + " factories were found");
    }
}
