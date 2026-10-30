# Moving a project to this version

For the projects that use ErikwnkWFUI (each one is changed in its own chat). Go from top to bottom: first
what stops the build, then what only looks or behaves differently, then what can be made simpler.
The full list of changes is in [../CHANGELOG.md](../CHANGELOG.md).

## 1. Build the project and follow the compiler

Names with an `[Obsolete]` alias still compile (with a warning); the others are gone.

### Gone (no alias): rename them

| Old | New |
|---|---|
| `ThemedLabel` | `ScrollingLabel` |
| `UIStyles.Labels.CreateNormal` | `CreateStandard` |
| `SliderBar`, `VolumeSlider` | `TrackBar` (the volume slider is the same control; for a mute button plus slider use `VolumeControl`) |
| `ToggleSwitch` factory `checkedState:` | `isChecked:` |
| `ToggleSwitches.CreateSmall` / `CreateLarge` | `CreateStandard(size: ...)` |
| `CheckedBackColor`, `UncheckedBackColor`, `KnobColor` of the switch | `colors:` (a `UIColorSet`) or `Colors` |

### Still works, but renamed (`[Obsolete]`): change them when you are there

| Old | New |
|---|---|
| `UIStyles.TextBoxes.CreateBorderstyleNone` | `CreateBorderless` |
| `UIStyles.Buttons.CreateThemeToggle` | `CreateThemeToggleStandard` |
| `UIColumn` | `PropertyColumn` (`UIColumn.Percent/Absolute/Auto(...)` return a `PropertyColumn`; a variable declared as `UIColumn` breaks when the alias goes) |
| `StyledForm` | `AppForm` |
| `StyledFormOptions` | `FormOptions` |
| `StyledFormType` | dropped: `FormOptions.CreateStandard / CreateDialog / CreateSettings` (or `UIStyles.Forms.Create...`) |
| `StyledOptionsForm` | `OptionsForm` |
| `MessageBoxSize` | `FormSize`, or an own `Size`, in `MessageBox.Show` |

Search the project for each old name (`grep -rn "StyledForm\|UIColumn\|CreateBorderstyleNone\|MessageBoxSize"`).

## 2. What looks or behaves differently

Nothing to change in the code, but look at the windows once.

- **MessageBox:** a box closed without a button now returns the *safe* answer (No for Yes/No - it was
  Cancel; Abort for Abort/Retry/Ignore). The safe button has the focus and answers Enter from anywhere.
  Look for `MessageBox.Show` with Yes/No whose result is compared with `Cancel`, and for code that relies
  on Enter = Yes (`defaultButton: Button1` brings it back).
- **Windows (`AppForm`):** open centered (over the owner if there is one), a resizable one has a least size
  of 260 x 140 (set `MinimumSize` after the constructor to change it), a drop shadow, flat caption buttons
  that never take the focus, the version strip is `BackgroundDark` in every window, a maximized frameless
  window respects the taskbar.
- **PropertyTable:** the frame of every editor cell is visible now; row labels wrap onto two lines; a row
  grows with a tall editor; row heights are scaled to the screen.
- **Check boxes, radio buttons, group boxes:** the text is readable on any surface; a `ForeColor` you set is used.
- **Sizes:** the title bar, the ring, the slim bar and the default button follow the screen scaling.
- **Tab key:** progress bars are no Tab stops.
- **Language:** one setting. `UIStyles.Language` and `AppLocalization.Language` follow each other; your own
  texts follow a switch when you give them as functions (`PropertyTable.AddRow(() => text, ...)`,
  `UIStyles.Localize(control, () => text)`).
- **Designer files:** the designer now writes a color or similar property only where you set it. Open and
  save a form once; the `Designer.cs` gets shorter.

## 3. What can be made simpler

- `AppForm.VersionStrip` and `VersionLabel` still exist; `FormOptions.CreateSettings` fills the version text,
  and the stand-alone strip with the update button is `VersionPanel`.
- A text box that is locked with a button: `UIStyles.TextBoxes.CreateLockable`; a password box:
  `CreatePassword`; the placeholder is painted (`Placeholder`), no code for focus events needed.
- Two hand-made settings windows (PropertyTable plus Save and Cancel): `OptionsForm`.
- A window that asks for a text or a password: `InputBox.Show(...)`; a window with a bar and a status text:
  `ProgressForm`.
- A window with the version and an update button: `UIStyles.Forms.CreateSettingsStandard(...)` and
  `VersionPanel`.
- Colors: instead of single colors per state give a color set, `colors: UIStyles.Colors.Sets.Green` or
  `UIStyles.Colors.CreateSet(color)`, to any `CreateStandard`.
- Remember the theme, accent and language: `UIStyles.RememberAppearance()` once at the start.
- Windows that remember their place: `FormOptions.RememberBoundsKey`.

## 4. Last item: remove the aliases from the library

When every project is changed, the `[Obsolete]` names are removed from ErikwnkWFUI together with their
tests (list in [ROUND-TWO.md](ROUND-TWO.md), "The `[Obsolete]` aliases"; the places of use are in the notes
of the maintainer). Do not remove them before the last project builds without the warning.

## Limits to know

Right-to-left layouts and the Windows text-size setting are not followed (README, "Known limitations").
