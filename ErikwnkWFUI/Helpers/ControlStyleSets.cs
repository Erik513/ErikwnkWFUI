using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>The <see cref="ControlStyles"/> that the controls share (SetStyle is protected, so a control passes the set to its own).</summary>
    internal static class ControlStyleSets
    {
        /// <summary>
        /// A control that draws everything itself, in one go and without flicker, and redraws when it is resized:
        /// <c>SetStyle(ControlStyleSets.OwnerDrawn | ControlStyles.SupportsTransparentBackColor, true)</c>. (OptimizedDoubleBuffer is what
        /// <c>DoubleBuffered = true</c> sets, so that need not be set on top.)
        /// </summary>
        internal const ControlStyles OwnerDrawn =
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw;
    }
}
