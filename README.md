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
`UIStyles.Labels.CreateNormal(...)`, `UIStyles.ToggleSwitches.CreateStandard(...)`,
and so on. Start there and let IntelliSense guide you through what's
available; each nested class (`Buttons`, `Labels`, `Panels`, ...) has a
one-line summary of what it's for.

## Project layout

- **`UIStyles`** (root namespace) - the one class you're meant to call
  directly. Everything below is effectively an implementation detail behind
  it.
- **`Controls/`** - the actual custom controls (`ListBox`,
  `ListView`, `ToggleSwitch`, `TitleBarControl`, ...), for when you
  need to reach past what a factory method gives you (e.g. subscribing to
  an event, or a property `UIStyles` doesn't proxy).
- **`Forms/`** - `StyledForm` (the base every ErikwnkWFUI window builds on),
  plus ready-made dialogs (`MessageBoxForm`, `InfoPopupForm`,
  `UpdateAvailableForm`, `StyledOptionsForm`).
- **`Factories/`** - internal. `UIStyles` is the only supported public
  entry point to these; if you're referencing anything under
  `ErikwnkWFUI.Factories` directly, use the equivalent `UIStyles.*` instead.
- **`Styles/`** - the color/font/string tables `UIStyles` reads from
  (`UIColors`, `UIFonts`, `UIThemes`, `UIAccentColors`, `UIStrings`).
- **`Helpers/`** - low-level plumbing (borderless resize hit-testing,
  window dragging, sleep prevention) used internally by the controls above;
  not something you'd normally use directly.

## Symbol buttons

A button that shows a symbol instead of a caption always comes from
`UIStyles.Buttons.CreateSymbol`, in one of two ways.

**With a ready-made preset** - the preset already says which symbol the button
carries and in which colors; you add the tooltip and, optionally, the size:

```csharp
var add = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Add, "Add");
var del = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Delete, "Delete", new Size(48, 48));
```

`UIStyles.Buttons.Presets` has Add, Save, Delete, Close, Edit, Refresh, Login,
Search, Settings, Back, Forward, Up, Down, Home, OpenFile and Browse. Your own are made
once with `new UIButtonPreset(symbol, colors)` and used everywhere.

**With a symbol and colors of your choice** - any symbol from `UIStyles.Symbols`
(or one of your own), in the neutral look or in the colors of a color set:

```csharp
var browse = UIStyles.Buttons.CreateSymbol(
    UIStyles.Symbols.Folder, "Choose folder", new Size(50, 30), UIStyles.Colors.Sets.Yellow);
var back = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Back, "Back");
var star = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Star, "Favorite", colors: Color.Orange);
```

A preset is nothing but a symbol plus a color set, so both ways give the same
button. The symbol is drawn exactly in the middle and grows with the height of
the button; hover, pressed and disabled looks come from the color set and are the
same in Dark and Light. To draw a symbol yourself (in a label, a menu), use
`UIStyles.Fonts.Icon` - or the font `UIStyles.Symbols.GetFontName(symbol)` names
for the few symbols that come from the Windows icon font - through a control that
draws with `TextRenderer`. The Showcase's Symbols tab shows every symbol.

## Theming

Two independent knobs, both **set once, before building any UI** - already-
built controls read their colors at construction time and won't pick up a
change made afterward:

```csharp
// Accent color: replaces the default blue everywhere (buttons, toggle
// switches, selection highlights, ...). Pick from UIAccentColors, or pass
// any Color.
UIStyles.Colors.SetAccent(UIAccentColors.Purple);

// Base theme: backgrounds/text/borders. Defaults to UIThemes.Dark
// (ErikwnkWFUI's original dark gray palette) - switch to UIThemes.Light
// for a light theme. Combine freely with SetAccent.
UIStyles.Colors.ApplyTheme(UIThemes.Light);
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

- `UIStyles.ComboBoxes.CreateStandard` and `UIStyles.NumericUpDowns.CreateStandard`
  only theme part of the control - a `ComboBox`'s dropdown list and a
  `NumericUpDown`'s spinner buttons are native Windows chrome and stay
  system-colored regardless of the active theme/accent.
- Theme, accent, and language changes only affect controls built *after*
  the change - nothing here live-updates already-open windows.
