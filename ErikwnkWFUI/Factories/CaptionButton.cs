using System.Windows.Forms;

namespace ErikwnkWFUI.Factories
{
    // A button of the title bar: it takes no focus, neither from a click nor from the Tab key, as the buttons of a native title bar.
    internal sealed class CaptionButton : Button
    {
        public CaptionButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
    }
}
