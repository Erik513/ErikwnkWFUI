namespace ErikwnkWFUI.Controls
{
    /// <summary>How fast the text of a <see cref="ThemedLabel"/> moves while it scrolls: 25, 45 or 90 pixels per second.</summary>
    public enum LabelScrollSpeed
    {
        /// <summary>Slowly, for a text that is read while it moves.</summary>
        Slow,

        /// <summary>The default.</summary>
        Medium,

        /// <summary>Fast, for a long text or little time.</summary>
        Fast
    }

    internal static class LabelScrollSpeedExtensions
    {
        // The speed of each level, in pixels per second.
        internal static float ToPixelsPerSecond(this LabelScrollSpeed speed)
        {
            switch (speed)
            {
                case LabelScrollSpeed.Slow:
                    return 25f;
                case LabelScrollSpeed.Fast:
                    return 90f;
                default:
                    return 45f;
            }
        }
    }
}
