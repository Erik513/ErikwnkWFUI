using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// <c>CreateSymbol</c>: a button made for a symbol of <c>UIStyles.Symbols</c>,
/// in the neutral look or in the colors of a set. What is its own here: the
/// symbol in a font that grows with the button, no padding, and the choice
/// between the neutral look and a set; the states of a set button are covered
/// like those of any other set button (ColoredButtonTests).
/// [Collection]: the colors read the live accent and the tests switch themes.
/// </summary>
public abstract class SymbolButtonTestBase
{
    protected static void Raise(Button button, string handler, params object[] args)
    {
        button.InvokePrivate(handler, args);
    }

    // A Font has no "disposed" flag or event; its native handle is zeroed on
    // Dispose (named differently on .NET Framework and .NET).
    protected static bool IsDisposed(Font font)
    {
        System.Reflection.FieldInfo? handle = typeof(Font)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .FirstOrDefault(field => field.FieldType == typeof(IntPtr) && field.Name.ToLowerInvariant().Contains("nativefont"));

        Assert.NotNull(handle);

        return (IntPtr)handle!.GetValue(font)! == IntPtr.Zero;
    }

    // How far the visible part of the symbol is from the middle of the
    // button, in pixels.
    protected static (double X, double Y)? InkOffset(Button button)
    {
        using Bitmap bitmap = Draw(button);
        Color back = button.BackColor;
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

        for (int y = 3; y < bitmap.Height - 3; y++)
        {
            for (int x = 3; x < bitmap.Width - 3; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                int difference = Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B);

                if (difference > 90)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
            return null;

        return ((minX + maxX + 1) / 2.0 - bitmap.Width / 2.0, (minY + maxY + 1) / 2.0 - bitmap.Height / 2.0);
    }

    // The box around everything drawn in the symbol's color.
    protected static Rectangle? InkBounds(Button button)
    {
        using Bitmap bitmap = Draw(button);
        Color back = button.BackColor;
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

        for (int y = 3; y < bitmap.Height - 3; y++)
        {
            for (int x = 3; x < bitmap.Width - 3; x++)
            {
                Color c = bitmap.GetPixel(x, y);

                if (Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B) > 90)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        return maxX < 0 ? null : new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    protected static Color FillOf(Button button)
    {
        using Bitmap bitmap = Draw(button);
        return bitmap.GetPixel(4, 4);
    }

    protected static void MouseDown(Button button, MouseButtons which = MouseButtons.Left)
    {
        Raise(button, "OnMouseDown", new MouseEventArgs(which, 1, 5, 5, 0));
    }

    protected static void MouseUp(Button button, MouseButtons which = MouseButtons.Left)
    {
        Raise(button, "OnMouseUp", new MouseEventArgs(which, 1, 5, 5, 0));
    }

    protected static Button CreateTeal() =>
        UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, size: new Size(40, 40), colors: UIStyles.Colors.Sets.Teal);

    protected static bool SamePixels(Button first, Button second)
    {
        using Bitmap a = Draw(first);
        using Bitmap b = Draw(second);

        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }
}
