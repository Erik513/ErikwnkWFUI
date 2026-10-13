using System.Collections.Generic;
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
            Controls.ComboBox comboBox = Create(comboBoxStyle);
            comboBox.Colors = colors;

            return comboBox;
        }

        // The Standard one with the accent: the selected row in the primary color set.
        public static ComboBox CreatePrimary(
            ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
        {
            Controls.ComboBox comboBox = Create(comboBoxStyle);
            comboBox.FollowColors(() => UIColors.PrimarySet);

            return comboBox;
        }

        private static Controls.ComboBox Create(ComboBoxStyle comboBoxStyle)
        {
            Controls.ComboBox comboBox = new Controls.ComboBox
            {
                Font = UIFonts.Normal,
                DropDownStyle = comboBoxStyle
            };

            Controls.InputField.FollowBackground(comboBox);
            Controls.ThemeWatcher.FollowForeColor(comboBox, () => UIColors.TextPrimary);

            return comboBox;
        }

        // See UIStyles.ComboBoxes.ReplaceItems. The selection is kept by POSITION: the case this is for is a language switch,
        // where the same choices come back in the same order with new texts. SelectedIndexChanged is not suppressed - the
        // Items.Clear() and the restoring of the index can each raise it once (a handler sees the -1 in between).
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