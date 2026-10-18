using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The range, the steps and the value, and how the user changes it.
    public partial class TrackBar
    {
        private double _value;
        private double _minimum;
        private double _maximum = 1.0;
        private double? _smallChange;
        private double? _largeChange;

        // The range cannot be empty: the top is at least this far above the bottom.
        private const double MinimumRange = 0.0001;

        /// <summary>The bottom of the range (0 by default). A <see cref="Value"/> below it is raised to it; not a number and infinity are ignored.</summary>
        [DefaultValue(0.0)]
        public double Minimum
        {
            get { return _minimum; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                _minimum = Math.Min(value, _maximum - MinimumRange);
                RangeChanged();
            }
        }

        /// <summary>The top of the range (1 by default); it is always above <see cref="Minimum"/>. A <see cref="Value"/> above it is lowered to it; not a number and infinity are ignored.</summary>
        [DefaultValue(1.0)]
        public double Maximum
        {
            get { return _maximum; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                _maximum = Math.Max(value, _minimum + MinimumRange);
                RangeChanged();
            }
        }

        // The range has changed: the value stays in it, the popup is measured again, and the thumb moves.
        private void RangeChanged()
        {
            _value = Clamp(_value, _minimum, _maximum);
            _valuePopup?.MarkWidthStale();
            Invalidate();
        }

        /// <summary>How far an arrow key or a step of the mouse wheel moves the value. 0 or less (the default) is a twentieth of the range.</summary>
        public double SmallChange
        {
            get { return _smallChange ?? _maximum / 20.0 - _minimum / 20.0; }
            set { _smallChange = value > 0 ? value : (double?)null; }
        }

        private bool ShouldSerializeSmallChange()
        {
            return _smallChange.HasValue;
        }

        private void ResetSmallChange()
        {
            _smallChange = null;
        }

        /// <summary>How far Page Up and Page Down move the value. 0 or less (the default) is a tenth of the range.</summary>
        public double LargeChange
        {
            get { return _largeChange ?? _maximum / 10.0 - _minimum / 10.0; }
            set { _largeChange = value > 0 ? value : (double?)null; }
        }

        private bool ShouldSerializeLargeChange()
        {
            return _largeChange.HasValue;
        }

        private void ResetLargeChange()
        {
            _largeChange = null;
        }

        /// <summary>
        /// The value, from <see cref="Minimum"/> to <see cref="Maximum"/> (one outside is limited to it, infinity is the end of the range,
        /// not a number is ignored). Setting it from code updates the display without raising <see cref="ValueChanged"/>.
        /// </summary>
        [DefaultValue(0.0)]
        public double Value
        {
            get { return _value; }
            set
            {
                if (double.IsNaN(value))
                    return;

                double clamped = Clamp(value, _minimum, _maximum);
                if (Math.Abs(clamped - _value) < double.Epsilon)
                {
                    return;
                }
                _value = clamped;
                Invalidate();
                AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            }
        }

        // How far along the track the value is, from 0 to 1.
        private double Fraction
        {
            // (Halved first: the width of a range from the lowest to the highest number would be too big for a double.)
            get { return (_value / 2 - _minimum / 2) / (_maximum / 2 - _minimum / 2); }
        }

        /// <summary>Pixel x of the thumb centre - for positioning something (e.g. a value popup) over it.</summary>
        protected int ThumbCenterX
        {
            get
            {
                return TrackLeft + (int)Math.Round(Fraction * TrackWidth);
            }
        }

        // v limited to min..max (the callers leave out a v that is not a number before).
        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void SetValueFromUser(double v)
        {
            if (double.IsNaN(v))
            {
                return;
            }

            double clamped = Clamp(v, _minimum, _maximum);
            if (Math.Abs(clamped - _value) < double.Epsilon)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }
    }
}
