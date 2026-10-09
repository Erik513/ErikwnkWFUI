namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// How the text of a <see cref="ThemedLabel"/> moves when it is too long for the label, all in one value
    /// to hand to <c>UIStyles.Labels</c> (or to <see cref="ThemedLabel.Scroll"/>):
    /// <c>LabelScroll.Endless()</c>, <c>LabelScroll.BackAndForth(LabelScrollSpeed.Fast)</c>. It cannot be
    /// changed; one value can serve any number of labels.
    /// </summary>
    public sealed class LabelScroll
    {
        private LabelScroll(
            LabelScrollMode mode,
            LabelScrollSpeed speed,
            LabelScrollPause pause,
            LabelScrollDirection direction,
            LabelScrollSeparator separator,
            bool pauseOnHover)
        {
            Mode = mode;
            Speed = speed;
            Pause = pause;
            Direction = direction;
            Separator = separator;
            PauseOnHover = pauseOnHover;
        }

        /// <summary>The text does not move (the default of a label).</summary>
        public static LabelScroll None { get; } = new LabelScroll(
            LabelScrollMode.None, LabelScrollSpeed.Medium, LabelScrollPause.Medium, LabelScrollDirection.Left, LabelScrollSeparator.Dot, false);

        /// <summary>
        /// The text moves to its end, rests, moves back to its start, rests, and so on. It begins with a
        /// rest at the start, then moves to the left.
        /// </summary>
        /// <param name="speed">How fast it moves; medium by default.</param>
        /// <param name="pause">How long it rests at the start and at the end; medium by default.</param>
        /// <param name="pauseOnHover">Whether the text stands still while the mouse is on the label (to read it); off by default.</param>
        public static LabelScroll BackAndForth(
            LabelScrollSpeed speed = LabelScrollSpeed.Medium,
            LabelScrollPause pause = LabelScrollPause.Medium,
            bool pauseOnHover = false)
        {
            return new LabelScroll(LabelScrollMode.BackAndForth, speed, pause, LabelScrollDirection.Left, LabelScrollSeparator.Dot, pauseOnHover);
        }

        /// <summary>The text moves on without rest and comes again behind a gap and a separator.</summary>
        /// <param name="direction">Which way it moves; to the left by default.</param>
        /// <param name="separator">The sign between the end of the text and its next appearance; a dot by default.</param>
        /// <param name="speed">How fast it moves; medium by default.</param>
        /// <param name="pauseOnHover">Whether the text stands still while the mouse is on the label (to read it); off by default.</param>
        public static LabelScroll Endless(
            LabelScrollDirection direction = LabelScrollDirection.Left,
            LabelScrollSeparator separator = LabelScrollSeparator.Dot,
            LabelScrollSpeed speed = LabelScrollSpeed.Medium,
            bool pauseOnHover = false)
        {
            return new LabelScroll(LabelScrollMode.Endless, speed, LabelScrollPause.Medium, direction, separator, pauseOnHover);
        }

        /// <summary>Whether and how the text moves.</summary>
        public LabelScrollMode Mode { get; }

        /// <summary>How fast the text moves.</summary>
        public LabelScrollSpeed Speed { get; }

        /// <summary>How long the text rests at its start and at its end (back and forth only).</summary>
        public LabelScrollPause Pause { get; }

        /// <summary>Which way the text moves (endless only).</summary>
        public LabelScrollDirection Direction { get; }

        /// <summary>The sign between the end of the text and its next appearance (endless only).</summary>
        public LabelScrollSeparator Separator { get; }

        /// <summary>Whether the text stands still while the mouse is on the label.</summary>
        public bool PauseOnHover { get; }
    }
}
