namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// A ready-made design for a symbol button: which symbol it carries and in
    /// which colors. Pass one to <c>UIStyles.Buttons.CreateSymbol</c> and the
    /// button gets both. The library's own are in <c>UIStyles.Buttons.Presets</c>
    /// (Add, Delete, Save ...); your own are made with the constructor, or from
    /// one of those with <see cref="WithSymbol"/> / <see cref="WithColors"/>, and can
    /// be defined once and used everywhere.
    /// </summary>
    /// <example>
    /// <code>
    /// var add = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Add, "Add");
    ///
    /// // Your own preset, defined once:
    /// var heart = new UIButtonPreset(UIStyles.Symbols.Heart, UIStyles.Colors.Sets.Orange);
    /// var like = UIStyles.Buttons.CreateSymbol(heart, "Like");
    /// </code>
    /// </example>
    public sealed class UIButtonPreset
    {
        /// <summary>The symbol on the button, for example <c>UIStyles.Symbols.Add</c>.</summary>
        public string Symbol { get; }

        /// <summary>
        /// The colors of the button; none (null) for the neutral look that follows
        /// the theme.
        /// </summary>
        public UIColorSet Colors { get; }

        /// <param name="symbol">The symbol, for example <c>UIStyles.Symbols.Add</c>.</param>
        /// <param name="colors">A color set, or none for the neutral look.</param>
        public UIButtonPreset(string symbol, UIColorSet colors = null)
        {
            Symbol = symbol ?? "";
            Colors = colors;
        }

        /// <summary>The same design with another symbol.</summary>
        public UIButtonPreset WithSymbol(string symbol)
        {
            return new UIButtonPreset(symbol, Colors);
        }

        /// <summary>The same design in other colors (a set, or none for the neutral look).</summary>
        public UIButtonPreset WithColors(UIColorSet colors)
        {
            return new UIButtonPreset(Symbol, colors);
        }
    }
}
