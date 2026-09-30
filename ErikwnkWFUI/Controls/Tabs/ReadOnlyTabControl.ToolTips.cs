using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Tooltips: the full name of a cut-off tab, what a scroll arrow does.
    public partial class ReadOnlyTabControl
    {
        // One tooltip for everything this control says about a tab or its
        // scroll arrows. Created on first use.
        private ToolTip _tabToolTip;
        private string _tabToolTipText = "";

        // The scroll arrows are a child window, so the tooltip cannot hook
        // into them the usual way: it is shown by hand after the same short
        // delay a tooltip has.
        private System.Windows.Forms.Timer _arrowToolTipTimer;
        private string _pendingArrowToolTipText = "";
        private Point _pendingArrowToolTipLocation;

        /// <summary>
        /// The text the tooltip over a tab shows - empty for none. By default
        /// the full name of a tab that is only partly in view (cut off at the
        /// scroll arrows); a derived control can add to it. Not used while
        /// <c>ShowToolTips</c> is on: then the standard control's own
        /// tooltips (<c>TabPage.ToolTipText</c>) are the ones in charge.
        /// </summary>
        protected virtual string GetTabToolTipText(int index)
        {
            if (index < 0 || index >= TabCount || !IsTabCutOff(index))
                return "";

            return TabPages[index].Text ?? "";
        }

        // A tab that is scrolled partly out of view - only while the strip
        // scrolls at all.
        private bool IsTabCutOff(int index)
        {
            if (_scrollButtons.GetBounds().IsEmpty)
                return false;

            Rectangle tab = GetTabRect(index);
            Rectangle visible = GetVisibleStripBounds();
            bool vertical = IsVertical();

            return vertical
                ? tab.Top < visible.Top || tab.Bottom > visible.Bottom
                : tab.Left < visible.Left || tab.Right > visible.Right;
        }

        private void UpdateTabToolTip(int index)
        {
            string text = ShowToolTips ? "" : GetTabToolTipText(index);

            if (text == _tabToolTipText)
                return;

            _tabToolTipText = text;
            EnsureTabToolTip().SetToolTip(this, text);
        }

        private ToolTip EnsureTabToolTip()
        {
            if (_tabToolTip == null)
            {
                _tabToolTip = UIToolTipFactory.CreateHoverToolTip(this);
            }

            return _tabToolTip;
        }

        // Told by the scroll arrows which half the mouse is over (None when
        // it left) - shows what pressing it does, after the usual delay.
        // Nothing for an arrow that cannot scroll any further.
        internal void OnScrollButtonHover(TabScrollButtons.Part part, bool horizontal, bool canScroll, Point location)
        {
            EnsureTabToolTip().Hide(this);

            if (_arrowToolTipTimer != null)
            {
                _arrowToolTipTimer.Stop();
            }

            _pendingArrowToolTipText = part == TabScrollButtons.Part.None || !canScroll || ShowToolTips
                ? ""
                : GetScrollArrowToolTipText(part, horizontal);

            if (_pendingArrowToolTipText.Length == 0)
                return;

            _pendingArrowToolTipLocation = location;

            if (_arrowToolTipTimer == null)
            {
                _arrowToolTipTimer = new System.Windows.Forms.Timer { Interval = 500 };
                _arrowToolTipTimer.Tick += (sender, e) =>
                {
                    _arrowToolTipTimer.Stop();

                    if (_pendingArrowToolTipText.Length > 0)
                    {
                        EnsureTabToolTip().Show(_pendingArrowToolTipText, this, _pendingArrowToolTipLocation, 4000);
                    }
                };
            }

            _arrowToolTipTimer.Start();
        }

        // What an arrow does: along a horizontal strip the first half scrolls
        // left and the second right, along a vertical one up and down.
        internal static string GetScrollArrowToolTipText(TabScrollButtons.Part part, bool horizontal)
        {
            bool first = part == TabScrollButtons.Part.First;

            if (horizontal)
                return UIStrings.Get(first ? "TabControl.ScrollLeft" : "TabControl.ScrollRight");

            return UIStrings.Get(first ? "TabControl.ScrollUp" : "TabControl.ScrollDown");
        }
    }
}
