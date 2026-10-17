namespace ErikwnkWFUI.Controls
{
    /// <summary>Where the text of a <see cref="Spinner"/> is placed in relation to the ring.</summary>
    public enum SpinnerTextPosition
    {
        /// <summary>To the right of the ring (the default).</summary>
        Right,

        /// <summary>To the left of the ring.</summary>
        Left,

        /// <summary>Above the ring.</summary>
        Above,

        /// <summary>Below the ring.</summary>
        Below
    }

    /// <summary>How fast the arc of a <see cref="Spinner"/> turns: a turn in about 2, 1.2 or 0.7 seconds.</summary>
    public enum SpinnerSpeed
    {
        /// <summary>Slowly, a turn in about 2 seconds.</summary>
        Slow,

        /// <summary>The default, a turn in about 1.2 seconds.</summary>
        Medium,

        /// <summary>Fast, a turn in about 0.7 seconds.</summary>
        Fast
    }
}
