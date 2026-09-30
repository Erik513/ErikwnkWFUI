using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>Data for <see cref="TabControl.TabAdding"/>.</summary>
    public class TabAddingEventArgs : CancelEventArgs
    {
        public TabAddingEventArgs(TabPage tabPage)
        {
            TabPage = tabPage;
        }

        /// <summary>The page that is about to be added. Replace it to add a different one.</summary>
        public TabPage TabPage { get; set; }
    }

    /// <summary>Data for <see cref="TabControl.TabClosing"/>.</summary>
    public class TabClosingEventArgs : CancelEventArgs
    {
        public TabClosingEventArgs(TabPage tabPage)
        {
            TabPage = tabPage;
        }

        /// <summary>The page that is about to be closed.</summary>
        public TabPage TabPage { get; }
    }

    /// <summary>Data for <see cref="ReadOnlyTabControl.TabRightClick"/>.</summary>
    public class TabRightClickEventArgs : EventArgs
    {
        public TabRightClickEventArgs(TabPage tabPage, int tabIndex, Point location)
        {
            TabPage = tabPage;
            TabIndex = tabIndex;
            Location = location;
        }

        /// <summary>The page whose tab was clicked.</summary>
        public TabPage TabPage { get; }

        /// <summary>The index of that tab.</summary>
        public int TabIndex { get; }

        /// <summary>Where the click was, in the tab control's own coordinates.</summary>
        public Point Location { get; }
    }
}
