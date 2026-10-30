# Whole-project analysis: progress

One row per unit, one unit per turn of the work (see [ROUND-TWO.md](ROUND-TWO.md) for the process and the rules). A unit is done when every box is
ticked and the commit is made. Tick a box with `x`; write `-` where a check does not apply (and say why in the commit message).

The checks of a unit:

- **Bugs** - bug hunt: states, events and their order, dispose and static events, threads, theme / language / DPI, keyboard and accessibility,
  extreme values (empty, null, huge, 0), disposed in an event, the real screen paint path.
- **Refactor** - duplicated code, file size, names. Report first; implement after the user's OK.
- **Docs** - `<summary>` on every public member (switch `CS1591` on for the unit), `<remarks>`, the README section is right, examples compile.
- **Showcase** - the page shows every property of the control's table in `CONTROLS.md` (or says why not); row order Standard, Primary,
  Standard(colors), others.
- **Tests >= 85 %** - line coverage of the unit's files (`tools\Run-Coverage.ps1`) is at least 85 %, the edge cases are covered; what is left out is
  left out on purpose (said in the commit message).
- **Table** - the unit's section of `CONTROLS.md` is right (`tools\Update-ControlTable.ps1`, `docs\control-notes.json` for what the code cannot say).

Coverage before the work: the column "Cov. before" is the first full run of `tools\Run-Coverage.ps1` (2026-10-29, 3918 tests, whole library 90.7 % = 17762 of 19587 lines); per unit it is the sum of the files of the unit and its factory, so the 85 % target is judged on the unit.

## Phase 0 - tools and starting point

| # | Step | Done |
|---|---|---|
| 0.1 | Make the test suite reliable: find the cause of the hanging run of the `ErikwnkWFUI.Tests.Forms` namespace (state shared between tests: message filters, the toast queue, static events) | [x] |
| 0.2 | One full run of the whole suite on the hidden desktop (announce it first; it is the only full run besides the last one) | [x] 3917 of 3918 green in 1 m 35 s (net8.0-windows); the one red test was fixed |
| 0.3 | One full run of `tools\Run-Coverage.ps1`; write the "before" numbers into the column below | [x] 3918 of 3918 green in 1 m 49 s; 90.7 % of 19587 lines; under 85 %: ListView 61.3, FormDragHandle 42.4, AppUpdater 43.8, CheckGlyph 53.5, TitledListBox 79.8, ReadOnlyDataGridView 83.7, ListBox 84.6; unused (0 %): AppCurrency, ColumnHeaderPainting, TextDragDropHelper, UIColumn (obsolete alias), UIIcons |
| 0.4 | Count the warnings of both targets (the library has none; the tests have none) and the files over 500 lines; write both down | [x] 0 warnings: library net48, library net8.0-windows, tests net8.0-windows (full rebuild). With CS1591 on: 638 public members without a summary (Controls/Inputs 123, Tables 73, Lists 70, UIStyles.Colors 56, Styles/UIColors 52, Tabs 40, Progress 32, Panels 28, ...). Files over 500 lines: 13 in the library (ReadOnlyDataGridView 878, UIColors 750, PropertyTable 727, ProgressForm 698, TitleBarControl 657, ListBox 647, ToastForm 624, ReadOnlyDataGridView.Columns 577, ReadOnlyTabControl.Drawing 548, ReadOnlyTabControl 540, InfoPopupForm 515, UIStyles.Buttons 512, MultiPanel 509), 12 in the tests (largest OptionsFormTests 1073, ProgressFormTests 907), 1 in the Showcase (ShowcaseForm.Basics 551) |
| 0.5 | Look at `CONTROLS.md` once as a whole: is anything missing in the generator (a kind of member, a default that shows `-` but could be shown)? | [x] Fixed: `Size`?` and `Func<Double, String>` written as C# (`Size?`, `Func<object, string>`). Done with the user's OK: the 8 types without a parameterless constructor (ProgressBar, ScrollingLabel, AppForm, InputBoxForm, MessageBoxForm, OptionsForm, ProgressForm, UpdateAvailableForm) show the defaults of their standard factory; new section "Other public types" (types and enumerations with their values). `UIColors` and `UIIcons` have no summary (units 1.1, 2.1). `ignored`/`notes` in control-notes.json exist for 3 controls only: each unit fills its own in the Table check |

## Phases 1 to 6 - the units

| # | Phase | Unit | Cov. before | Bugs | Refactor | Docs | Showcase | Tests >= 85 % | Table | Commit |
|---|---|---|---:|---|---|---|---|---|---|---|
| 1.1 | 1 Foundations | `UIColors`, `UIThemes`, `UIColorSet`, `UIAccentColors`, `UIFonts` | 99.3 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 1.2 | 1 | `ThemeWatcher`, `ThemeColor`, `ColorSetHolder`, `SurfaceColor`, `InputField`, `FocusReleaseWatcher`, `OutsideClickFilter` | 97.4 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 1.3 | 1 | Helpers (`Dpi`, `AnimationClock`, `FrameTicker`, `AnimationSettings`, `RoundedRectangle`, `LayeredWindow`, `WindowPlacementStore`, ...) | 72.7 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 1.4 | 1 | Native (`NativeMethods`, `NativeControlTheme`) | 80.3 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 1.5 | 1 | Localization (`UIStrings`, `AppCurrency`, `AppLocalization`; both languages complete, no unused keys) | 95.5 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 1.6 | 1 | `UIStyles` as a whole: the same names and parameter order everywhere, the Standard / Primary / colors pattern, the factory classes without a gap (a script lists them) | 90.5 % | [x] | [x] | [x] | - | [x] | [x] | [x] |
| 2.1 | 2 Simple controls | Buttons and symbols (`Button` factories, `SymbolButton`, `UIButtonPreset`, `UIStyles.Symbols`, theme toggle) | 98.5 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.2 | 2 | Labels (`Label`, `ScrollingLabel`) | 98.7 % | [x] | [ ] | [x] | [x] | [x] | [x] | [x] |
| 2.3 | 2 | `CheckBox`, `BoxLabel`, `CheckGlyph` | 91.5 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.4 | 2 | `RadioButton`, `RadioGroup` | 91.4 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.5 | 2 | `GroupBox` | 87.1 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.6 | 2 | `ToggleSwitch` | 100 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.7 | 2 | `ProgressBar` | 97.5 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.8 | 2 | `SlimProgressBar` | 97.1 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 2.9 | 2 | `Spinner` | 98.3 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 3.1 | 3 Input | `TextField`, `LockableTextBox`, `PasswordTextBox` (text boxes) | 95.0 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 3.2 | 3 | `ComboBox` | 95.6 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 3.3 | 3 | `NumericUpDown` | 98.0 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 3.4 | 3 | `TrackBar` (with `ValuePopup`) | 96.4 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 3.5 | 3 | `VolumeControl` | 97.9 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.1 | 4 Containers | `Panel`, `CardPanel`, `PanelFrame` | 100 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.2 | 4 | `FlowLayoutPanel` | 94.4 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.3 | 4 | `TableLayoutPanel` (with `CellBorderLines`) | 99.3 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.4 | 4 | `MultiPanel` | 100 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.5 | 4 | `VersionPanel` | 98.8 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.6 | 4 | `TabControl`, `ReadOnlyTabControl` | 94.1 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.7 | 4 | `ContextMenuStrip` | 98.3 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 4.8 | 4 | ToolTips (`UIStyles.ToolTips`, `FullTextToolTip`, `ToolTipActivationFix`) | 98.1 % | [x] | [x] | [x] | - | [x] | - | [x] |
| 4.9 | 4 | `TitleBarControl` | 98.4 % | [x] | [x] | [x] | [x] | [x] | - | [x] |
| 5.1 | 5 Lists and tables | `ListBox`, `TitledListBox` | 89.4 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 5.2 | 5 | `ListView` | 88.4 % | [x] | [x] | [x] | [x] | [x] | [x] | [x] |
| 5.3 | 5 | `DataGridView`, `ReadOnlyDataGridView` (with the column helpers) | 85.1 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 5.4 | 5 | `PropertyTable`, `PropertyRow`, `PropertyColumn` | 94.4 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.1 | 6 Windows | `AppForm`, `FormOptions`, `FormSize`, `FormDragHandle`, `BorderlessResizeHandler` | 83.6 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.2 | 6 | `MessageBox`, `MessageBoxForm` | 94.2 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.3 | 6 | `InputBox`, `InputBoxForm` | 95.1 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.4 | 6 | `ProgressForm`, `ProgressBox` | 95.9 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.5 | 6 | `OptionsForm`, `OptionsPreset`, `UIAppearance` | 97.6 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.6 | 6 | `ToastForm`, `Toaster` | 99.4 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.7 | 6 | `InfoPopupForm`, `InfoPopupSection`, `PopupFormBase`, `PopupSupport` | 98.2 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 6.8 | 6 | Update: `UpdatePrompt`, `UpdateAvailableForm`, `AppUpdater` (and `SelfUpdater` of ErikwnkCore: see the open items) | 87.3 % | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |

## Phase 7 - across the whole project

| # | Step | Done |
|---|---|---|
| 7.1 | Read the whole README and the Showcase Guide page as a stranger: every example compiles and is right, every sentence is still true | [ ] |
| 7.2 | Showcase shell: tabs, theme and accent bar, the order of the pages, the Guide; the pages build without an exception in both themes and both languages | [ ] |
| 7.3 | `net48` equal to `net8.0-windows`: build and the whole test project on both targets | [ ] |
| 7.4 | GDI and window handle leaks: tests that create and dispose each control a few hundred times and compare `GetGuiResources` / handle counts | [ ] |
| 7.5 | DPI 125 / 150 / 200 % on the real screen (ask first; the QuietForm method in ROUND-TWO.md) for the self-drawn controls | [ ] |
| 7.6 | Accessibility: `AccessibleName` / role / state of the controls and keyboard reach (Tab order, mnemonics) | [ ] |
| 7.7 | Performance with many controls (a table of 500 check boxes, a list with 10 000 items, many toasts) | [ ] |
| 7.8 | The right-to-left and large-font behavior is written down (it is not supported library-wide: say so in the README) | [ ] |

## Phase 8 - before the push

| # | Step | Done |
|---|---|---|
| 8.1 | Remove dead code, files and texts that the analysis found | [ ] |
| 8.2 | Warnings and XML-doc warnings (`CS1591` on for the whole library) at 0 on both targets | [ ] |
| 8.3 | Decide with the user about the `[Obsolete]` aliases (list in ROUND-TWO.md); remove them only when the user says his projects are migrated | [ ] |
| 8.4 | One full run of the whole suite on both targets, and one full coverage run: "after" next to "before" | [ ] |
| 8.5 | `CHANGELOG.md` | [ ] |
| 8.6 | Write `docs/MIGRATION.md` (last, so that it describes the final state) | [ ] |
| 8.7 | `CONTROLS.md` current (`tools\Update-ControlTable.ps1`), README current, then the user pushes | [ ] |