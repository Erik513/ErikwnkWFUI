# Changelog

All notable changes of ErikwnkWFUI. The old names that still work are marked `[Obsolete]`
in the code; how to change a project to the new ones is in [docs/MIGRATION.md](docs/MIGRATION.md).

## Unreleased - the cleanup of every control and the analysis of the whole project (October 2026)

Round one went through every control (naming, one factory pattern, the original behavior, light theme,
tests, comments). Round two (`docs/ANALYSIS.md`) went through the whole project, unit by unit: bugs,
documentation, the Showcase, tests of at least 85 % of the lines per control, and the table
`docs/CONTROLS.md`.

### Changed - names

- `StyledForm` -> `AppForm`, `StyledFormOptions` -> `FormOptions` (no `Type`; `CreateSettings` sets the
  version strip), `StyledFormType` is dropped, `StyledOptionsForm` -> `OptionsForm`.
- `ThemedLabel` -> `ScrollingLabel`; `Labels.CreateNormal` -> `CreateStandard`.
- `SliderBar` -> `TrackBar`; `VolumeSlider` is part of `TrackBar` now; new `VolumeControl`.
- `TextBoxes.CreateBorderstyleNone` -> `CreateBorderless`; `Buttons.CreateThemeToggle` ->
  `CreateThemeToggleStandard`; `UIColumn` -> `PropertyColumn`; `MessageBoxSize` -> `FormSize` or an own `Size`.
- Every factory follows one pattern: `CreateStandard(..., UIColorSet colors = null)` and `CreatePrimary(...)`
  (the accent); a test holds it for all 34 groups.

### Changed - behavior

- **MessageBox:** a box closed without a button returns the safe answer (No for Yes/No, it was Cancel;
  Abort for Abort/Retry/Ignore). The safe button has the focus and answers Enter from anywhere
  (`defaultButton: Button1` brings back Enter = Yes/OK). Escape closes a box that has only OK. The title
  follows the language live. The window grows for a long text (not when an own size is given).
- **ToggleSwitch:** `checkedState` -> `isChecked`; `CreateSmall` / `CreateLarge` -> `size:`; the colors come
  from a color set (`colors:`). `CreateStandard` is gray while on, `CreatePrimary` is the accent.
- **Language:** one setting. `UIStyles.Language` is `AppLocalization.Language`; `UIStyles.LanguageChanged`
  and `UIStyles.Localize` keep texts of the program in step.
- **Sizes:** the title bar (30 px, caption buttons 42 x 30), the ring (24), the slim bar (4) and the default
  button (30 x 30) are measures for 96 dpi and are scaled to the screen like the other controls.
- **Tab key:** `ProgressBar` and `SlimProgressBar` are no Tab stops any more.

### Added

- Color sets (`UIColorSet`) for every control; `UIStyles.Colors.Sets` and `CreateSet(color)`.
- `TextBoxes`: `CreatePrimary`, `CreateLockable`, `CreatePassword`, a painted placeholder, the full text as a
  tool tip of a cut-off text. `MessageBox`: `ShowWithCheckBox`, `RetryCancel`, `AbortRetryIgnore`, a help
  button (`HelpClicked`), details, button texts. `VersionPanel`, `MultiPanel`, `PropertyTable`,
  `TitledListBox`, `ProgressForm`, `OptionsForm`, `InputBox`, toasts and popups.
- Screen reader support (role, name, state, value) for the controls that draw themselves.
- `CS1591` is on: every public member has a comment; the XML file ships with the DLL.

### Fixed

- Colors: `OverlayDark/Medium/Light` were transparent (wrong order of the arguments of `FromArgb`).
- Theme: `ThemeWatcher` and `FocusReleaseWatcher` no longer fail when a window goes while a theme changes on
  another thread.
- Clipboard: a busy clipboard no longer throws in the grids and the list view, and a cut whose copy failed
  clears nothing. `RoundedRectangle` clamps a radius that is too large. `LayeredWindow` gives back its
  device contexts.
- Scroll bar sizes counted the screen scaling twice (`GetSystemMetricsForDpi` now).
- A negative dollar amount is written `-$5.00`; `UIStrings.Get(null)` is empty.
- Controls that did not look at the mouse again when they were enabled or shown (symbol button, progress
  bar text, text box button, combo box, numeric up-down, track bar thumb, tabs).
- A label whose posted frame went with its window no longer stands still; a disposed bar no longer stays on
  the animation clock (`net48`); the animation clock posts through the synchronization context (a control
  disposed meanwhile got its window made again on the wrong thread).
- The title bar no longer asks again and again for a form while it has a window but none yet.
- The labels of the check box, radio button and group box are readable on any surface.
- The designer writes colors and similar properties only where they were set (not every default), and
  properties that hold state or results are hidden from it.

### Tests and documents

- More than 4300 tests on `net8.0-windows` and `net48`, 95 % of the lines (it was 91 %); tests for the
  handles (no leak in 120 cycles of each of the 104 factories), the roles and names for a screen reader,
  500 check boxes, a list of 10 000 items, 50 toasts, and a smoke test that paints all 332 pages of the
  Showcase in both themes and languages.
- The examples of the README compile against the library. New: `docs/CONTROLS.md` (every control,
  property, default and comment, made by `tools\Update-ControlTable.ps1`), `docs/ANALYSIS.md`,
  `docs/ROUND-TWO.md`, and one HTML page of them (`tools/Export-DocsHtml.py`).

### Known limits

- Right-to-left layouts and the Windows text-size setting are not followed (README, "Known limitations").
- A look at the windows on a screen at 125, 150 and 200 % is still to be done on another PC.
- The `[Obsolete]` names stay until the projects that use them are changed; then they are removed.
