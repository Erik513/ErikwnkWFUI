using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// What a screen reader and the keyboard get from the controls of the library: the role (what it is), the name (the text, or the one that was
/// set), the state (unavailable) and a stop of the Tab key where there is something to use. The controls are found by reflection through the
/// <c>CreateStandard</c> factory of each group of <c>UIStyles</c>.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class AccessibilityTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private Control Make(string group, string text = "Text")
    {
        Type type = typeof(UIStyles).GetNestedTypes(BindingFlags.Public).Single(t => t.Name == group);
        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "CreateStandard" && !m.GetParameters().Any(p => !p.IsOptional && p.ParameterType != typeof(string)))
            .OrderBy(m => m.GetParameters().Length).First();

        Control control = (Control)method.Invoke(null, method.GetParameters().Select(p => p.ParameterType == typeof(string) && p.Position == 0 ? text : Type.Missing).ToArray())!;

        if (control.Width <= 0 || control.Height <= 0)
            control.Size = new Size(120, 40);

        _forms.Add(OffscreenForm.Show(control, new Size(Math.Max(control.Width + 20, 160), Math.Max(control.Height + 20, 80)), Color.Black, new Point(5, 5)));

        return control;
    }

    [Theory]
    [InlineData("Buttons", AccessibleRole.PushButton)]
    [InlineData("CheckBoxes", AccessibleRole.CheckButton)]
    [InlineData("RadioButtons", AccessibleRole.RadioButton)]
    [InlineData("ToggleSwitches", AccessibleRole.CheckButton)]
    [InlineData("ComboBoxes", AccessibleRole.ComboBox)]
    [InlineData("TrackBars", AccessibleRole.Slider)]
    [InlineData("ProgressBars", AccessibleRole.ProgressBar)]
    [InlineData("SlimProgressBars", AccessibleRole.ProgressBar)]
    [InlineData("Spinners", AccessibleRole.ProgressBar)]
    [InlineData("TextBoxes", AccessibleRole.Text)]
    public void AControl_TellsWhatItIs(string group, AccessibleRole role)
    {
        Control control = Make(group);

        Assert.Equal(role, control.AccessibilityObject.Role);
    }

    [Theory]
    [InlineData("Buttons")]
    [InlineData("CheckBoxes")]
    [InlineData("RadioButtons")]
    public void AControlWithAText_IsNamedByIt_AndAnOwnNameWins(string group)
    {
        Control control = Make(group, "Save");

        Assert.Equal("Save", control.AccessibilityObject.Name);

        control.AccessibleName = "Own";
        Assert.Equal("Own", control.AccessibilityObject.Name);
    }

    [Theory]
    [InlineData("ToggleSwitches")]
    [InlineData("TrackBars")]
    [InlineData("Spinners")]
    public void AControlWithoutAText_IsNamedByWhatTheProgramSays(string group)
    {
        Control control = Make(group);
        control.AccessibleName = "Sound";

        Assert.Equal("Sound", control.AccessibilityObject.Name);
    }

    [Theory]
    [InlineData("Buttons")]
    [InlineData("CheckBoxes")]
    [InlineData("RadioButtons")]
    [InlineData("ToggleSwitches")]
    [InlineData("TrackBars")]
    [InlineData("ComboBoxes")]
    [InlineData("TextBoxes")]
    public void ADisabledControl_IsUnavailable(string group)
    {
        Control control = Make(group);
        control.Enabled = false;

        Assert.True(control.AccessibilityObject.State.HasFlag(AccessibleStates.Unavailable), group + " is " + control.AccessibilityObject.State);
    }

    [Theory]
    [InlineData("Buttons")]
    [InlineData("CheckBoxes")]
    [InlineData("ToggleSwitches")]
    [InlineData("TrackBars")]
    [InlineData("ComboBoxes")]
    [InlineData("TextBoxes")]
    public void WhatCanBeUsedByTheKeyboard_HasAStopOfTheTabKey(string group)
    {
        Control control = Make(group);

        Assert.True(control.TabStop, group + " has no stop of the Tab key");
        Assert.True(control.CanFocus, group + " cannot take the focus");
    }

    [Fact]
    public void WhatOnlyShowsSomething_HasNoStopOfTheTabKey()
    {
        Assert.False(Make("Spinners").TabStop);
        Assert.False(Make("ProgressBars").TabStop);
    }
}
