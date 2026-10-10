using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIComboBoxFactory
    {
        public static ComboBox CreateStandard(
            ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList,
            UIColorSet colors = null)
        {
            Controls.ComboBox comboBox = new Controls.ComboBox
            {
                Font = UIFonts.Normal,
                DropDownStyle = comboBoxStyle,
                Colors = colors
            };

            Controls.ThemeWatcher.FollowBackColor(comboBox, () => UIColors.BackgroundMedium);
            Controls.ThemeWatcher.FollowForeColor(comboBox, () => UIColors.TextPrimary);

            return comboBox;
        }

        // The Standard one with the accent: the selected row in the primary color set.
        public static ComboBox CreatePrimary(
            ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
        {
            Controls.ComboBox comboBox = (Controls.ComboBox)CreateStandard(comboBoxStyle);
            comboBox.FollowColors(() => UIColors.PrimarySet);

            return comboBox;
        }

        // Preserves selection by POSITION (same index in the new list as
        // before), not by value/identity - the common case this is for is
        // re-localization, where the same N choices get rebuilt with new
        // display text in the same order, and "same index" is the only
        // notion of "same item" that still makes sense once the old and new
        // items aren't equal to each other. A caller whose items carry their
        // own stable identity (e.g. an enum wrapped in a display object, the
        // way DJ-mode/generation-mode combo boxes elsewhere in this codebase
        // already do) and needs identity-based matching across a genuine
        // reordering should still just save/restore SelectedIndex (or a
        // matching key) by hand instead. Doesn't suppress SelectedIndexChanged
        // during the rebuild - Items.Clear() and the SelectedIndex restore
        // below can each raise it once, since there's no supported way to
        // detach every external subscriber generically; a handler reacting
        // to the transient -1 in between is the one thing this can't avoid.
        public static void ReplaceItems(ComboBox comboBox, IEnumerable<object> items, bool keepSelection = true)
        {
            if (comboBox == null || items == null)
                return;

            int previousSelectedIndex = keepSelection ? comboBox.SelectedIndex : -1;

            comboBox.BeginUpdate();
            try
            {
                comboBox.Items.Clear();
                foreach (object item in items)
                    comboBox.Items.Add(item);

                if (previousSelectedIndex >= 0 && previousSelectedIndex < comboBox.Items.Count)
                    comboBox.SelectedIndex = previousSelectedIndex;
            }
            finally
            {
                comboBox.EndUpdate();
            }
        }
    }
}