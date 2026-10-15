using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The panel of <c>UIStyles.Panels</c>: the standard <see cref="Panel"/>, drawn without flicker and with the scroll bars in
    /// the style of the theme. Everything else - <c>AutoScroll</c>, <c>AutoSize</c>, <c>BackgroundImage</c>, <c>Padding</c> - is
    /// the standard control. Named <c>ThemedPanel</c> (not <c>Panel</c>) so that it does not clash with the standard class in
    /// the files that use both namespaces; the factories hand it back as a <see cref="Panel"/>.
    /// </summary>
    public class ThemedPanel : Panel
    {
        public ThemedPanel()
        {
            // The panel is repainted as a whole instead of in strips while it is resized (the standard panel flickers).
            DoubleBuffered = true;

            // The scroll bars of a panel with AutoScroll are the system's own.
            NativeControlTheme.Track(this);
        }
    }
}
