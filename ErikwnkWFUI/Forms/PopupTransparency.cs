namespace ErikwnkWFUI.Forms
{
    /// <summary>How see-through a popup (<see cref="InfoPopupForm"/>, <see cref="ToastForm"/>) is.</summary>
    public enum PopupTransparency
    {
        /// <summary>Not transparent at all.</summary>
        Opaque,

        /// <summary>Slightly transparent.</summary>
        Low,

        /// <summary>The default: clearly transparent, the window behind still shows through.</summary>
        Medium,

        /// <summary>Strongly transparent.</summary>
        High
    }

    internal static class PopupTransparencyExtensions
    {
        // The Form.Opacity of each level.
        internal static double ToOpacity(this PopupTransparency transparency)
        {
            switch (transparency)
            {
                case PopupTransparency.Opaque:
                    return 1.0;
                case PopupTransparency.Low:
                    return 0.90;
                case PopupTransparency.High:
                    return 0.65;
                default:
                    return 0.80;
            }
        }
    }
}
