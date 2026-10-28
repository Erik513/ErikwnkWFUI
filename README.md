# ErikwnkWFUI

A dark-themed WinForms UI control library: borderless windows with a custom
title bar, styled buttons/labels/lists/tables/dialogs, and a small theming
system (accent color + light/dark base palette). Targets `net48` and
`net8.0-windows`.

## Adding it to a project

Reference `ErikwnkWFUI.dll` (built from `ErikwnkWFUI/ErikwnkWFUI.csproj`) the
same way as any other library - project reference if you have the source
checked out alongside your app, or copy the built DLL (see
`ErikwnkWFUI/bin/<Debug|Release>/<net48|net8.0-windows>/ErikwnkWFUI.dll`)
into your own project and add a plain assembly reference. IntelliSense
tooltips come from `ErikwnkWFUI.xml`, built next to the DLL - keep the two
together.

## Quick start

```csharp
using ErikwnkWFUI;
using ErikwnkWFUI.Forms;

// A resizable window with ErikwnkWFUI's own title bar (UIStyles.Forms.CreateDialogStandard and CreateSettingsStandard make dialogs).
using (var form = UIStyles.Forms.CreateStandard("My App"))
{
    var button = UIStyles.Buttons.CreatePrimary("Click me");
    button.Location = new Point(20, 20);
    form.ContentPanel.Controls.Add(button);

    Application.Run(form);
}
```

Everything goes through `UIStyles` - `UIStyles.Buttons.CreateStandard(...)`,
`UIStyles.Labels.CreateStandard(...)`, `UIStyles.ToggleSwitches.CreateStandard(...)`,
and so on. Start there and let IntelliSense guide you through what's
available; each nested class (`Buttons`, `Labels`, `Panels`, ...) has a
one-line summary of what it's for.

## Project layout

- **`UIStyles`** (root namespace) - the one class you're meant to call
  directly. Everything below is effectively an implementation detail behind
  it.
- **`Controls/`** - the actual custom controls (`ListBox`,
  `ListView`, `ToggleSwitch`, `MultiPanel`, `TitleBarControl`, ...), for when you
  need to reach past what a factory method gives you (e.g. subscribing to
  an event, or a property `UIStyles` doesn't proxy). In sub folders by kind:
  `Inputs`, `Labels`, `Lists`, `Panels`, `Progress`, `PropertyTable`, `Tables`,
  `Tabs`, `Support`.
- **`Forms/`** - `AppForm` and `FormOptions` (the base every ErikwnkWFUI window
  builds on, made by `UIStyles.Forms`), plus ready-made dialogs and popups (`MessageBoxForm`, `ToastForm`,
  `InfoPopupForm`, `UpdateAvailableForm`, `OptionsForm`).
- **`Factories/`** - internal. `UIStyles` is the only supported public
  entry point to these; if you're referencing anything under
  `ErikwnkWFUI.Factories` directly, use the equivalent `UIStyles.*` instead.
- **`Styles/`** - the color and font tables `UIStyles` reads from
  (`UIColors`, `UIFonts`, `UIThemes`, `UIAccentColors`).
- **`Localization/`** - the texts the library shows itself in German and
  English (`UIStrings`) and `AppCurrency`.
- **`Helpers/`** - low-level plumbing (borderless resize hit-testing,
  window dragging, rounded shapes, scroll bars in the style of the theme, DPI
  scaling) used internally by the controls above; not something you'd normally
  use directly.

## Color sets

Every `CreateStandard` takes an optional `colors` parameter - a color set
(`UIStyles.Colors.Sets.Green`, `UIStyles.Colors.CreateSet(color)`, or simply a
`Color`) that colors the control in all its states; hover, pressed, disabled and
the text on each are worked out for you. Without it the control is the neutral
gray, and `CreatePrimary` is the same control in the current accent.

```csharp
var green = UIStyles.ComboBoxes.CreateStandard(colors: UIStyles.Colors.Sets.Green);
var accent = UIStyles.ComboBoxes.CreatePrimary();
```

## Labels

`UIStyles.Labels` has `CreateStandard` (body text), `CreatePrimary` (the same in the
accent color), `CreateTitle` (bold, larger, centered) and `CreateMuted` (quiet text);
`CreateStandard`, `CreateTitle` and `CreateMuted` also take a color set. A text that is
too long can move instead of ending in an ellipsis, back and forth or endlessly, by
handing a `LabelScroll` to the factory:

```csharp
var back = UIStyles.Labels.CreateStandard(text, scroll: LabelScroll.BackAndForth());
var ticker = UIStyles.Labels.CreateStandard(text,
    scroll: LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot));

// a banner: several texts, the same room after each, the separator in the middle of it
var banner = UIStyles.Labels.CreatePrimary("", scroll: LabelScroll.Endless(new[] { "News", "Sale", "Closed on Monday" }));
```

The text moves smoothly on the beat of the display, back and forth stands still when the text fits (endless keeps going, a short text
keeps the room of the label), and not at
all where Windows has the animations switched off.

## Text boxes

`UIStyles.TextBoxes` makes the standard `TextBox`, with a quiet frame of the library: `CreateStandard` (the frame has the text color while the box has the
focus), `CreatePrimary` (the accent), `CreateStandard(colors: ...)` (a color set or a single color) and `CreateBorderless` (no frame). The fill and the
text follow the theme; a box that is `ReadOnly` has a slightly dimmed fill (less than a disabled one). A placeholder is passed to the factory
(`CreateStandard("", "Search")`); it is painted over the box and never is its `Text`, so a `PasswordChar` does not mask it and no `TextChanged`
comes for it (the `PlaceholderText` of .NET 5 and later is the same placeholder). A text that is wider than the box is the tooltip of the box
(`ToolTipWhenCut` of the `TextField`, on by default).

Two boxes have a button inside the frame, at the right end:

- `CreateLockable` is locked (read-only) until the pencil is used - it unlocks the box, gives it the focus and selects the text; the check mark or the
  Enter key accepts the text and locks it again, Escape gives the old text back, F2 or Enter on the locked box start the editing. It makes a
  `LockableTextBox` with `IsLocked`, `LockedChanged`, `EditAccepted`, `EditCancelled` and `BeginEdit()`, `AcceptEdit()`, `CancelEdit()`.
- `CreatePassword` hides what is typed; the eye shows the password with a click (or Alt+F8) and hides it again with the next one.

```csharp
var name = UIStyles.TextBoxes.CreateLockable("My playlist");
name.EditAccepted += (s, e) => Save(name.Text);

var password = UIStyles.TextBoxes.CreatePassword(placeholder: "Password", colors: UIStyles.Colors.Sets.Green);
```

## Property table

`UIStyles.PropertyTables` makes a table of "label: editor" rows, grouped into sections - the usual shape of a settings page: `CreateStandard`
(no line at the section headers), `CreatePrimary` (a line in the accent color at the left edge of each header) and `CreateStandard(colors: ...)`
(a color set or a single color). `AddSection(title)` adds a header; `AddRow(label, editors...)` adds a row whose editors share the width, or
`AddRow(label, PropertyColumn.Absolute(a, 120), PropertyColumn.Percent(b, 30f), PropertyColumn.Auto(c))` for widths of your own (the old name
`UIColumn` still works, but is obsolete). The editors are placed by their kind: a button, check box, toggle switch, spinner or volume control keeps
its size at the left, a number box is as wide as it is, and the others (text box, combo box, track bar, panel ...) fill the cell.

- A row is 42 px high, or as high as its highest editor needs (`AddRow(label, rowHeight, ...)` gives a minimum). Row heights, the label column and
  the room around the editors are given for 96 dpi and scaled to the screen.
- `LabelWidth` sets the width of the label column (130 by default). A longer label wraps onto a second line; what still does not fit is the tooltip
  of the label.
- `AddRow` and `AddSection` return a `PropertyRow`: `Visible = false` hides the row (the rows below move up), `Enabled = false` locks it - for an
  option that depends on another one.
- `ClearRows()` removes all rows. The editors are only detached, not disposed, so they keep their state and can be added again - this is how a table
  is rebuilt after a language switch.

```csharp
var table = UIStyles.PropertyTables.CreatePrimary();
table.LabelWidth = 160;
table.AddSection("General");
table.AddRow("Name", UIStyles.TextBoxes.CreateStandard("My playlist"));
var advanced = UIStyles.ToggleSwitches.CreateStandard(isChecked: false);
table.AddRow("Advanced", advanced);
PropertyRow details = table.AddRow("Details", UIStyles.TextBoxes.CreateStandard());
details.Visible = false;
advanced.CheckedChanged += (s, e) => details.Visible = advanced.Checked;
```

## Windows

`AppForm` is the window of the library: its own title bar instead of the native one, no frame, resizable by its edges, a drop shadow, and the
work area of the screen when it is maximized. `UIStyles.Forms` makes one: `CreateStandard` (resizable, with minimize, maximize and close),
`CreateDialogStandard` (a fixed size, only the close button, Escape closes it) and `CreateSettingsStandard` (a dialog with the version of the
program in a strip at the bottom); each also as `...Primary` (the title bar in a very dark shade of the accent) and, for the Standard ones, with
`colors:` (the title bar in a color set). Put the content into `ContentPanel`; a window of your own derives from `AppForm` and hands
`FormOptions` to the constructor.

- A window opens in the middle of the screen, or over its owner (`Show(owner)`, `ShowDialog(owner)`); a resizable one has a least size
  (`FormOptions.MinimumSize`, 260 x 140 at 96 dpi). `StartPosition` can be changed.
- The thin edge around the window is black (`BorderColor` changes it), the version strip is a darker gray than the content. `TitleBarBackColor`
  colors the bar alone; `TitleTextAlign`, `ShowMinimizeButton`, `ShowMaximizeButton` and `ShowCloseButton` shape the bar. The title keeps clear of the
  buttons when the window gets narrow. `FormTitle` sets the title of the window and of the bar at once.
- Right-click the title bar, or press Alt+Space, for the system menu (`ShowSystemMenu` shows it from code).
- `RememberBoundsKey` makes a window come back where it was (position, size, maximized), as long as that is still on a screen; a fixed-size window
  remembers only its position. `CloseEnabled = false` greys out the close button and keeps the user from closing the window (Alt+F4, the taskbar,
  Escape) while the program can still close it - for an update that is being installed.
- `Borderless = false` keeps the native frame of Windows (and has no title bar of the library, no edge, no shadow).

```csharp
using var form = UIStyles.Forms.CreateSettingsStandard("Settings");
form.ContentPanel.Controls.Add(UIStyles.Labels.CreateStandard("Hello"));
form.RememberBoundsKey = "Settings";
form.Show(owner);
```

The old names `StyledForm`, `StyledFormOptions`, `StyledFormType` and `StyledOptionsForm` still work but are obsolete: they are `AppForm`, `FormOptions`
(its presets `CreateStandard`, `CreateDialog` and `CreateSettings` are the same) and `OptionsForm`; the type is not needed any more.

## Panels

`UIStyles.Panels` are the surfaces a layout is built from: `CreateStandard` (the usual
background), `CreatePrimary` (a very dark shade of the accent), `CreateStandard(colors)`,
`CreateDark`, `CreateMedium`, `CreateElevated`, `CreateTransparent`, and cards
(`CreateCardStandard`, `CreateCardPrimary`) with rounded corners and a thin line.
`UIStyles.TableLayoutPanels` and `UIStyles.FlowLayoutPanels` have the same surfaces for
tables and rows of controls (a flow panel with a background is as big as its content).

A panel has no frame by default; `BorderStyle = FixedSingle` (a thin frame, `BorderColor` for another
color) is the same on the panel, the table and the flow panel. A table can also have lines
around and between its cells (`CellBorderStyle`), of which only the ones between the cells
or only the ones around the table can be shown (`CellBorderLines`) and which can be
colored (`CellBorderColor`, `InnerCellBorderColor`, `OuterCellBorderColor`). The panels
are the standard controls in every other way; the scroll bars take the style of the
theme. A card does not scroll itself - put a panel with `AutoScroll` in it.

```csharp
var surface = UIStyles.Panels.CreateElevated();
var card = UIStyles.Panels.CreateCardStandard();
var table = (TableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
table.CellBorderLines = CellBorderLines.InnerOnly;
```

`UIStyles.MultiPanels` has several pages of which one is shown at a time, like a book
(flat or as a card): a header with the page count (`ShowPageCount`), the title of the
page and two arrows, and `AddPage(title)` gives each page for its controls.
`UIStyles.VersionPanels` is a narrow strip with the version of the program and, while an
update is known, a small update button (`ShowUpdate` or `CheckForUpdateAsync`).

```csharp
var book = UIStyles.MultiPanels.CreateCardStandard();
book.ShowPageCount = true;
var first = book.AddPage("Profile");
first.Controls.Add(UIStyles.Labels.CreateStandard("Name"));
book.AddPage("Settings");

var version = UIStyles.VersionPanels.CreateStandard();
await version.CheckForUpdateAsync(appUpdater, TimeSpan.FromSeconds(5));
```

## Popups

- **`UIStyles.Toasts`** (`ToastForm.ShowToast(message, window)`) - a small message
  that closes itself, in the neutral gray or a color set (`ShowStandard`,
  `ShowPrimary`). It belongs to its window (moves with it, never takes the focus)
  and is shown only while that window is in front; a toast that cannot be shown
  waits and appears when the window is active again (at most three, for one
  minute). Optional: a title and own text colors (`InfoPopupSection`), a symbol
  (`ToastIcon`: check mark, info, warning, error, or any `UIStyles.Symbols`
  symbol), a place in the window (`ToastPosition`, below the middle by default),
  transparency, duration and delay. A click closes it, and it stays while the mouse
  is on it. `UIStyles.Toasts.CreateStandard` keeps all these settings as
  properties for repeated use.
- **`UIStyles.InfoPopups`** - a popup with blocks of a header and a text, as a
  replacement for the standard tooltip. `popup.AttachTo(control, "Text")` makes it
  the tooltip of a control (one popup can serve many); or set the content with
  `SetContent` and show it where you want (`ShowNextTo`, `ShowAtMouse`,
  `ShowCenteredAbove`). Transparency, how long it stays and the delay before it
  shows are set with `PopupTransparency`, `PopupDuration` and `PopupDelay`.

## Progress bars and spinners

`UIStyles.ProgressBars` (a bar with a border) and `UIStyles.SlimProgressBars` (a thin strip for tight places, such
as a status line) have `CreateStandard` (gray, or the color of a color set), `CreatePrimary` (the accent),
`CreateGreen` and `CreateStatus` (the fill goes from red over yellow to green with the value). Both offer what the
standard `ProgressBar` does (`Minimum`, `Maximum`, `Value`, `Step`, `PerformStep`, the styles Blocks and Marquee for an
action of unknown duration) and more: a `Text` (a message about the state) and `ShowPercentage`, both off by default
(the text stands on the left, the percentage on the right; `TextAlign`, `PercentageAlign` and, on the slim bar, `TextPosition`
move them). A text that is too long ends in an ellipsis or moves with `TextScroll` (as in a label); a tooltip shows it whole.

```csharp
var bar = UIStyles.ProgressBars.CreatePrimary();
bar.Text = "Uploading";
bar.ShowPercentage = true;
bar.Value = 42;

var strip = UIStyles.SlimProgressBars.CreateStatus();   // under a text, as thin as it is
```

`UIStyles.Spinners` are loading circles: `CreateStandard(size, thickness, colors)`, `CreatePrimary`, `CreateGreen` and
`CreateStatus`, and the `CreateProgress*` versions (40 px by default) that also write the `Value` in the centre.
The size and the stroke width are the first two parameters of every method. The arc turns while the load is unknown;
with `IsIndeterminate = false` it fills up to the `Value` instead. `Text` (at the `TextPosition`), `Speed`, `IsSpinning`
and `HideWhenStopped` do what their names say.

```csharp
var wait = UIStyles.Spinners.CreatePrimary(40, 5);
wait.Text = "Loading data...";
wait.Width = 200;

var load = UIStyles.Spinners.CreateProgressStatus();
load.Value = 73;
```

`UIStyles.VolumeControls` is a ready-made volume control for audio: a mute button (a loudspeaker, crossed out while muted) with a
`TrackBar` beside it, as on video sites. A click on the button (or the key M on the slider) mutes and brings the volume back the next
time, the slider moved to 0 mutes and moved away from 0 turns the sound on again, and the slider shows the percentage in a popup (or,
with `showPercentage: true`, as a number beside it). The mouse wheel over the button turns the volume like the wheel over the slider. Read
`EffectiveVolume` (0 while muted) for what the audio output gets and react to `VolumeChanged`.

A player is coupled with one line: it implements `IAudioVolume` of ErikwnkCore (or is derived from the ready-made `AudioVolumeState`, which limits
the volume, raises the event only on a change and has `EffectiveVolume`), and `volume.Target = player;` (or `target: player` in the factory call) does the rest - the control sets the player
and follows it when it changes by itself.

```csharp
var volume = UIStyles.VolumeControls.CreatePrimary(0.6);
volume.VolumeChanged += (s, e) => player.Volume = (float)volume.EffectiveVolume;
```

People do not hear loudness linearly (a plain linear gain at 50 % already sounds almost as loud as at 100 %), so an audio output usually gets a
curve of the slider: `AudioVolumeState.ToGain(volume)` (or `state.Gain()`) squares it, `ToGain(volume, 3)` makes a cube.

`UIStyles.TrackBars` is the slider alone (any range, steps, keys, wheel, a popup with the value).

`UIStyles.ToggleSwitches` is an on/off switch, an alternative to a check box: `CreateStandard` is gray while it is on, `CreatePrimary` follows
the accent, and `colors:` takes a color set or a single color (`Color.Red` for a switch that is always a danger); `size:` makes it smaller or
larger (45 x 25 at 96 dpi by default, scaled to the screen). A click toggles it and the knob glides; the space bar does too (Tab to it), and a
screen reader is told a check button with its state - set `AccessibleName`, as there is no text on the switch (put a label beside it).
`CheckedChanged` is raised for every change, also when code sets `Checked`.

```csharp
var dark = UIStyles.ToggleSwitches.CreatePrimary(isChecked: false, "Dark mode on", "Dark mode off");
dark.AccessibleName = "Dark mode";
dark.CheckedChanged += (s, e) => UIStyles.Colors.ApplyTheme(dark.Checked ? UIThemes.Dark : UIThemes.Light);
```

Everything that moves runs on one beat of the display (as the moving text of a label) and stands still where
Windows has the animations switched off or while the control is disabled; it starts again when they are switched on.

## Symbol buttons

A button that shows a symbol instead of a caption always comes from
`UIStyles.Buttons.CreateSymbol`, in one of two ways.

**With a ready-made preset** - the preset already says which symbol the button
carries and in which colors; you add the tooltip and, optionally, the size:

```csharp
var add = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Add, "Add");
var del = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Delete, "Delete", new Size(48, 48));
```

`UIStyles.Buttons.Presets` has a preset for every symbol of `UIStyles.Symbols`, named
like the symbol (Add, Delete, Save, Play, Warning, Lock ...); a few carry the name of
what they are for instead (Login, OpenFile, Browse). Your own are made
once with `new UIButtonPreset(symbol, colors)` and used everywhere.

**With a symbol and colors of your choice** - any symbol from `UIStyles.Symbols`
(or one of your own), in the neutral look or in the colors of a color set:

```csharp
var browse = UIStyles.Buttons.CreateSymbol(
    UIStyles.Symbols.Folder, "Choose folder", new Size(50, 30), UIStyles.Colors.Sets.Yellow);
var back = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Back, "Back");
var heart = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Heart, "Like", colors: Color.Orange);
```

A preset is nothing but a symbol plus a color set, so both ways give the same
button. The symbol is drawn exactly in the middle and grows with the height of
the button; hover, pressed and disabled looks come from the color set and are the
same in Dark and Light. To draw a symbol yourself (in a label, a menu), use
`UIStyles.Fonts.Icon` - or the font `UIStyles.Symbols.GetFontName(symbol)` names
for the few symbols that come from the Windows icon font - through a control that
draws with `TextRenderer`. The Showcase's Symbols tab shows every symbol.

## Theming

Two independent knobs. Both can be changed **at any time, also while windows
are shown** - the controls of the library follow by themselves (a color set
on a control by hand stays as it was set):

```csharp
// Accent color: replaces the default blue everywhere (buttons, toggle
// switches, selection highlights, ...). Pick from UIAccentColors, or pass
// any Color.
UIStyles.Colors.SetAccent(UIAccentColors.Purple);

// Base theme: backgrounds/text/borders. Defaults to UIThemes.Dark
// (ErikwnkWFUI's original dark gray palette) - switch to UIThemes.Light
// for a light theme. Combine freely with SetAccent.
UIStyles.Colors.ApplyTheme(UIThemes.Light);

// Raised after either change - only needed for what the application draws
// or colors itself.
UIStyles.Colors.ThemeChanged += (sender, e) => { /* ... */ };
```

Most controls also expose their own colors as regular properties for a
one-off override (e.g. `ListBox.SelectedBackColor`; a switch, a slider or a progress bar takes a
color set instead: `UIStyles.ToggleSwitches.CreateStandard(colors: ...)`) - those work per-instance, any time,
independent of the two calls above.

## Localization

```csharp
UIStyles.Language = UILanguage.German;
```

Switches the text ErikwnkWFUI's own built-in dialogs ship (title bar
tooltips, the update-available prompt, message box buttons, ...) between
English (default) and German. Same "set before building UI" caveat as
theming - already-built dialogs won't relabel themselves, except the title
bar's tooltips, which do update live.

## Icons/logo

Load your own app's icon or logo from an embedded resource rather than a
loose file next to the exe - a loose file can go missing or get left behind
by a self-update, an embedded resource can't:

```csharp
Image logo = UIIcons.LoadEmbedded(Assembly.GetExecutingAssembly(), "MyApp.Assets.Logo.png");
```

(Requires the image's Build Action set to `Embedded Resource` in your
project.) Pass the result to `FormOptions.Icon` or
`TitleBarControl.IconImage`.

## Known limitations

- A `ComboBox` is themed except for the text field of its editable style
  (selection color) and the `Simple` style, which keeps the system's drawing.
- Theme, accent and language changes reach the windows that are open: the
  controls read their colors and texts again (see `ThemeChanged` above). What the
  application draws or colors itself has to follow by hand, and a color that was
  set on a control by hand stays as it was set.
- Right-to-left layouts are not supported.
