namespace ErikwnkWFUI.Forms
{
    /// <summary>Where a <see cref="ToastForm"/> is shown inside the window it belongs to. The positions at an edge or in a corner keep a fixed distance from the edge, they never touch it.</summary>
    public enum ToastPosition
    {
        /// <summary>Centered, in the lower part of the window (three quarters down) - the usual place, used when no position is given.</summary>
        BelowCenter,

        /// <summary>In the middle of the window.</summary>
        Center,

        /// <summary>Centered, in the upper part of the window (a quarter down), above the middle - as far above it as <see cref="BelowCenter"/> is below.</summary>
        AboveCenter,

        /// <summary>Centered at the top.</summary>
        TopCenter,

        /// <summary>Centered at the bottom.</summary>
        BottomCenter,

        /// <summary>At the top left.</summary>
        TopLeft,

        /// <summary>At the top right.</summary>
        TopRight,

        /// <summary>At the bottom left.</summary>
        BottomLeft,

        /// <summary>At the bottom right.</summary>
        BottomRight
    }
}
