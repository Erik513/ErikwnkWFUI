using System;

namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// What a control keeps of its <see cref="UIColorSet"/>: a set that was given, or - for the primary look - a source that is asked
    /// for again each time the set is needed, so that the colors follow a change of the accent. Giving a set takes the source away, and
    /// giving a source takes the set away (the last one wins). The control repaints itself after it has changed it.
    /// </summary>
    internal sealed class ColorSetHolder
    {
        private UIColorSet _set;
        private Func<UIColorSet> _source;

        /// <summary>The set, or what the source says now; null when there is neither (the neutral look).</summary>
        public UIColorSet Value
        {
            get { return _source?.Invoke() ?? _set; }
            set
            {
                _set = value;
                _source = null;
            }
        }

        /// <summary>Asks <paramref name="source"/> for the set each time it is needed (the primary set, which follows the accent).</summary>
        public void Follow(Func<UIColorSet> source)
        {
            _source = source;
            _set = null;
        }
    }
}
