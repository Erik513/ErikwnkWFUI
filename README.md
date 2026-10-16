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

// A borderless, resizable window with ErikwnkWFUI's own title bar.
using (var form = new StyledForm("My App"))
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
- **`Forms/`** - `StyledForm` (the base every ErikwnkWFUI window builds on),
  plus ready-made dialogs and popups (`MessageBoxForm`, `ToastForm`,
  `InfoPopupForm`, `UpdateAvailableForm`, `StyledOptionsForm`).
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
one-off override (e.g. `ToggleSwitch.CheckedBackColor`,
`ListBox.SelectedBackColor`) - those work per-instance, any time,
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
project.) Pass the result to `StyledFormOptions.Icon` or
`TitleBarControl.IconImage`.

## Known limitations

- A `ComboBox` is themed except for the text field of its editable style
  (selection color) and the `Simple` style, which keeps the system's drawing.
- Theme, accent and language changes reach the windows that are open: the
  controls read their colors and texts again (see `ThemeChanged` above). What the
  application draws or colors itself has to follow by hand, and a color that was
  set on a control by hand stays as it was set.
- Right-to-left layouts are not supported.
