using System.Collections.Generic;
using System.Drawing;

namespace ErikwnkWFUI.Forms
{
    /// <summary>One block of content for <see cref="InfoPopupForm"/>: a header, a text, or both. A part left null or empty is not shown.</summary>
    public class InfoPopupSection
    {
        /// <summary>The header, in bold above the text; null or empty: none.</summary>
        public string Header { get; set; }
        /// <summary>The text; null or empty: none.</summary>
        public string Text { get; set; }

        /// <summary>The color of the header. Left empty, it is white, or black on a background too light for white.</summary>
        public Color HeaderColor { get; set; }

        /// <summary>The color of the text. Left empty, it is white, or black on a background too light for white.</summary>
        public Color TextColor { get; set; }

        /// <summary>Makes a block with a header and a text, either of which may be left null.</summary>
        /// <param name="header">The header.</param>
        /// <param name="text">The text.</param>
        public InfoPopupSection(string header, string text)
        {
            Header = header;
            Text = text;
        }

        // The blocks that have something to show, as copies: a part that is empty or only
        // spaces is dropped, a block without any part is left out.
        internal static InfoPopupSection[] Normalize(InfoPopupSection[] sections)
        {
            List<InfoPopupSection> kept = new List<InfoPopupSection>();

            if (sections != null)
            {
                foreach (InfoPopupSection section in sections)
                {
                    if (section == null)
                        continue;

                    bool hasHeader = !string.IsNullOrWhiteSpace(section.Header);
                    bool hasText = !string.IsNullOrWhiteSpace(section.Text);

                    if (hasHeader || hasText)
                    {
                        kept.Add(new InfoPopupSection(hasHeader ? section.Header : null, hasText ? section.Text : null)
                        {
                            HeaderColor = section.HeaderColor,
                            TextColor = section.TextColor
                        });
                    }
                }
            }

            return kept.ToArray();
        }

        internal static bool SameContent(InfoPopupSection[] first, InfoPopupSection[] second)
        {
            if (first.Length != second.Length)
                return false;

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i].Header != second[i].Header || first[i].Text != second[i].Text
                    || first[i].HeaderColor != second[i].HeaderColor || first[i].TextColor != second[i].TextColor)
                    return false;
            }

            return true;
        }
    }
}
