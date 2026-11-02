using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// What the popups that hang on a window (the info popup, the toast) share: they are shown
    /// only while that window is in front, and they move with it.
    /// </summary>
    internal static class PopupSupport
    {
        // Whether the window the popup belongs to is the one in front. Replaceable so a test
        // does not depend on what is in front on the machine it runs on.
        internal static Func<Form, bool> OwnerIsActive = owner => IsActiveFor(Form.ActiveForm, owner);

        // The window in front is the owner itself, or a window owned by it (a dialog opened
        // from it). With another window of the application in front, or another program, it is
        // not. A control without a form has no window to compare: any active window will do.
        internal static bool IsActiveFor(Form active, Form owner)
        {
            if (active == null)
                return false;

            if (owner == null)
                return true;

            for (Form form = active; form != null; form = form.Owner)
            {
                if (form == owner)
                    return true;
            }

            return false;
        }

        // The text of a popup: white, or black when the set's color is too light for white. On the
        // color itself one of the two always reads (at least 4.58:1).
        internal static Color GetTextColor(UIColorSet colors)
        {
            Color basic = colors.Basic;

            return UIColors.GetContrastRatio(Color.White, basic) >= UIColors.GetContrastRatio(Color.Black, basic)
                ? Color.White
                : Color.Black;
        }

        // The background of a popup: the set's basic color, kept readable through the
        // transparency. The whole window is see-through, so the text fades with it and the page
        // behind (white or black, the worst) shines through the background; with a white page
        // behind, white text on a mid-tone color can drop far below 4.5:1. The color is shifted
        // away from the text (darker for white text, lighter for black) just far enough for the
        // text to read over both a white and a black page. A color that reads already stays.
        internal static Color GetBackground(UIColorSet colors, double opacity)
        {
            Color back = colors.Basic;
            Color text = GetTextColor(colors);
            double step = text == Color.White ? -0.01 : 0.01;

            for (int i = 0; i < 100 && WorstContrast(text, back, opacity) < 4.5; i++)
                back = UIColors.ShiftLightness(back, step);

            return back;
        }

        // The lower of the contrasts the text has over a white page and over a black page.
        private static double WorstContrast(Color text, Color back, double opacity)
        {
            double worst = double.MaxValue;

            foreach (Color page in new[] { Color.White, Color.Black })
            {
                Color seenBack = UIColors.Blend(page, back, opacity);
                Color seenText = UIColors.Blend(page, text, opacity);

                worst = Math.Min(worst, UIColors.GetContrastRatio(seenText, seenBack));
            }

            return worst;
        }

        internal const int ScreenMargin = 10;

        // Where a popup may be: inside the window of its control, and on the screen where that
        // window reaches beyond it. A control without a form has the screen.
        internal static Rectangle GetPlacementArea(Control control)
        {
            Rectangle screen = Screen.FromControl(control).WorkingArea;
            Form form = control.FindForm();

            if (form == null)
                return screen;

            Rectangle inside = Rectangle.Intersect(form.Bounds, screen);

            return inside.IsEmpty ? screen : inside;
        }

        // Moves a popup of this size inside the area, with a margin to its edge where there is
        // room for it. A popup larger than the area cannot fit: it sits at the start of the area
        // (the top left), as far as the screen shows it.
        internal static Point KeepInside(Point location, Size size, Rectangle area, Rectangle screen)
        {
            return new Point(
                KeepInside(location.X, size.Width, area.Left, area.Right, screen.Left, screen.Right),
                KeepInside(location.Y, size.Height, area.Top, area.Bottom, screen.Top, screen.Bottom));
        }

        private static int KeepInside(int position, int size, int start, int end, int screenStart, int screenEnd)
        {
            if (size > end - start)
                return Math.Max(screenStart, Math.Min(start, screenEnd - size));

            int margin = size + 2 * ScreenMargin <= end - start ? ScreenMargin : 0;
            int first = start + margin;
            int last = end - margin - size;

            return Math.Max(first, Math.Min(position, last));
        }

        // A popup is owned by its window, and showing an owned window raises that window: with
        // another window or program in front the popup would pull its window up over it. So
        // nothing is shown unless that window is in front, and not over a minimized one.
        internal static bool CanShowOver(Form ownerForm)
        {
            if (ownerForm != null && (ownerForm.IsDisposed || ownerForm.WindowState == FormWindowState.Minimized))
                return false;

            return OwnerIsActive(ownerForm);
        }
    }

    /// <summary>
    /// Keeps a popup at the same place relative to its owner window: when the window is moved
    /// the popup goes with it.
    /// </summary>
    internal sealed class OwnerFollower
    {
        private Form _popup;
        private Form _owner;
        private Point _offset;

        // Starts following <paramref name="owner"/> from where the popup is now; called again
        // after the popup was put somewhere else.
        internal void Attach(Form popup, Form owner)
        {
            if (owner == null)
            {
                Detach();

                return;
            }

            if (_owner != owner)
            {
                Detach();
                _owner = owner;
                _owner.LocationChanged += OnOwnerLocationChanged;
            }

            _popup = popup;
            _offset = new Point(popup.Left - owner.Left, popup.Top - owner.Top);
        }

        internal void Detach()
        {
            if (_owner != null)
                _owner.LocationChanged -= OnOwnerLocationChanged;

            _owner = null;
            _popup = null;
        }

        private void OnOwnerLocationChanged(object sender, EventArgs e)
        {
            if (_popup == null || _popup.IsDisposed || _owner == null || _owner.IsDisposed)
                return;

            _popup.Location = new Point(_owner.Left + _offset.X, _owner.Top + _offset.Y);
        }
    }
}
