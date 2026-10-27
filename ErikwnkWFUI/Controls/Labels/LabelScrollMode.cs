namespace ErikwnkWFUI.Controls
{
    /// <summary>Whether and how the text of a <see cref="ScrollingLabel"/> moves when it is too long for the label.</summary>
    public enum LabelScrollMode
    {
        /// <summary>The text does not move; one that is too long ends in an ellipsis (the default).</summary>
        None,

        /// <summary>The text moves to its end, rests there, moves back to its start, rests there, and so on.</summary>
        BackAndForth,

        /// <summary>The text moves on without rest and comes again behind a gap and a separator, one direction for good (<see cref="ScrollingLabel.ScrollDirection"/>). It moves whether the text fits or not; a short text keeps the room of the label (a word of 10 pixels in 50 comes back after 40 empty ones).</summary>
        Endless
    }

    /// <summary>Which way the text of a <see cref="ScrollingLabel"/> moves in the endless mode.</summary>
    public enum LabelScrollDirection
    {
        /// <summary>To the left: the text appears at the right edge and leaves at the left one (the default).</summary>
        Left,

        /// <summary>To the right: the text appears at the left edge and leaves at the right one.</summary>
        Right
    }

    /// <summary>The sign between the end of the text and its next appearance in the endless mode.</summary>
    public enum LabelScrollSeparator
    {
        /// <summary>A dot (the default).</summary>
        Dot,

        /// <summary>A vertical bar.</summary>
        Bar,

        /// <summary>A diamond.</summary>
        Diamond,

        /// <summary>No sign, only the gap.</summary>
        None
    }

    internal static class LabelScrollSeparatorExtensions
    {
        // The sign of each choice.
        internal static string ToText(this LabelScrollSeparator separator)
        {
            switch (separator)
            {
                case LabelScrollSeparator.Bar:
                    return "|";
                case LabelScrollSeparator.Diamond:
                    return "\u25C6";
                case LabelScrollSeparator.None:
                    return "";
                default:
                    return "\u2022";
            }
        }
    }
}
