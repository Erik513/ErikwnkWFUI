namespace ErikwnkWFUI.Controls
{
    /// <summary>How long the text of a <see cref="ThemedLabel"/> rests at its start and at its end while it scrolls: 1, 2.2 or 4 seconds.</summary>
    public enum LabelScrollPause
    {
        /// <summary>A short rest.</summary>
        Short,

        /// <summary>The default.</summary>
        Medium,

        /// <summary>A long rest, for text that is read at the ends.</summary>
        Long
    }

    internal static class LabelScrollPauseExtensions
    {
        // The rest of each level, in milliseconds.
        internal static int ToMilliseconds(this LabelScrollPause pause)
        {
            switch (pause)
            {
                case LabelScrollPause.Short:
                    return 1000;
                case LabelScrollPause.Long:
                    return 4000;
                default:
                    return 2200;
            }
        }
    }
}
