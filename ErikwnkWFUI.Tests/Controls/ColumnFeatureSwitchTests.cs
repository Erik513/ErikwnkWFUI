using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>
/// ColumnFeatureSwitch backs BOTH ListView's and ReadOnlyDataGridView's own
/// Allow*/Set*Reorderable/Resizable pairs - this used to be re-derived,
/// nearly verbatim, once per control per feature (4 places: ListView's own
/// reordering and resizing, ReadOnlyDataGridView's own reordering and
/// resizing). Tested directly here instead, via InternalsVisibleTo (see
/// ErikwnkWFUI/InternalsVisibleTo.cs) rather than reflection - it's a
/// standalone utility type, not one of the controls' own private members,
/// so there's no real public/protected surface being widened by referencing
/// it directly. Each control's own test file now keeps just ONE minimal
/// test confirming its own property actually delegates to a working switch,
/// not the full default/override/re-enable matrix this covers once here.
/// </summary>
public class ColumnFeatureSwitchTests
{
    [Fact]
    public void IsAllowed_DefaultsToTrueForEveryColumn()
    {
        ColumnFeatureSwitch featureSwitch = new ColumnFeatureSwitch();

        Assert.True(featureSwitch.IsAllowed(0));
        Assert.True(featureSwitch.IsAllowed(1));
    }

    [Fact]
    public void SetAllowed_False_DisablesOnlyThatColumn()
    {
        ColumnFeatureSwitch featureSwitch = new ColumnFeatureSwitch();

        featureSwitch.SetAllowed(0, false);

        Assert.False(featureSwitch.IsAllowed(0));
        Assert.True(featureSwitch.IsAllowed(1));
    }

    [Fact]
    public void SetAllowed_True_ReEnablesAPreviouslyDisabledColumn()
    {
        ColumnFeatureSwitch featureSwitch = new ColumnFeatureSwitch();
        featureSwitch.SetAllowed(0, false);

        featureSwitch.SetAllowed(0, true);

        Assert.True(featureSwitch.IsAllowed(0));
    }

    [Fact]
    public void AllowedByDefaultFalse_OverridesEveryColumnRegardlessOfPerColumnSetting()
    {
        ColumnFeatureSwitch featureSwitch = new ColumnFeatureSwitch();
        // Even a column explicitly re-enabled per-column must still be
        // disabled once the master switch itself is off - the master
        // switch always wins.
        featureSwitch.SetAllowed(0, true);

        featureSwitch.AllowedByDefault = false;

        Assert.False(featureSwitch.IsAllowed(0));
        Assert.False(featureSwitch.IsAllowed(1));
    }
}
