# Round two: the whole-project analysis (handover)

This file is the handover for a **new chat**. Round one (the per-control cleanup, 12 steps for each control, plus MessageBox, the update prompt,
RadioButton, GroupBox and the user's small fixes) is finished and committed. Round two goes through the whole library once more, **one unit at a
time**, before anything is pushed and before the migration of the user's own projects.

Start with [ANALYSIS.md](ANALYSIS.md) (the plan as a table and the place where progress is ticked off), then read this file to the end.

## What the user wants out of it

1. Possible errors found (bug hunt), refactoring, documentation / comments / README checked, the Showcase checked, test coverage and edge cases covered.
2. A table that can be called up and is kept current: for every control what can be configured and how: [CONTROLS.md](CONTROLS.md), generated.
3. The analysis split into single steps, one control after the other (not everything in one run).
4. Everything tidied **before** `docs/MIGRATION.md` is written and before the push. The migration describes the final state, so it comes last.

Decided by the user (2026-10-29): `CONTROLS.md` is enough for the table (no extra Showcase page); the coverage target is **85 % of the lines per
control**; the work runs in a **new chat**; the order is the one in ANALYSIS.md (foundations, simple controls, input, containers, lists and tables,
windows, across the project, before the push).

## How the new chat should start

The user pastes something like: "Read docs/ROUND-TWO.md and docs/ANALYSIS.md and start with phase 0." Then:

1. Read this file, `ANALYSIS.md`, the memory index (`MEMORY.md` in the user's Claude memory folder, see the end) and the memory notes it points to.
2. Do phase 0 step by step (one step per turn, report, wait).
3. Then the units in ANALYSIS.md order, **one unit per turn**.

## The working rules (from the user, do not break them)

- **Reply in German** (the user writes German). Code, comments and docs in English.
- **Commit only when the user says "commit"** (or "commit und weiter"). Report the result and wait. End commit messages with
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` (or the attribution the session gives).
- **Feedback first** when the user asks to *check*, *analyse* or for an *opinion*: report findings and proposals, implement after the user's OK. A decision
  the user states as a requirement ("muss", "soll") is implemented at once. Clear bugs found on the way are fixed straight away; design choices and
  behavior changes are asked.
- **One control (unit) per turn.** After each turn the user answers "commit und weiter".
- **Keep the PC responsive:** the user works on the same machine. Build with `-m:2 -nodeReuse:false`, do not rebuild "to be sure", announce heavy steps
  (a full test run, a coverage run, a screenshot), never start anything that opens an activating window, and ask before a capture of the real screen.
- **Test scope:** while working on a unit run only its tests (`--filter`), one target. **Never run the whole suite on your own.** The full runs are
  only phase 0.2, 0.3 and 8.4 (announced) or when the user asks. A failing unrelated test is investigated by running its class, not the suite.
- All tests run on a hidden desktop (below); a test form must never be activated.
- Fix the Showcase by looking at it (render it, below), not by guessing. `DrawToBitmap` is not the paint path of the real screen (lessons below).
- Prefer generic helpers that serve every control to control-specific ones; comments short, precise, human; judge a feature by its general usefulness,
  never by what the user's own projects use.
- Showcase conventions: per control the rows `CreateStandard`, `CreatePrimary`, `CreateStandard(colors: Yellow)`, then the others; ONE row = ONE factory
  method; the disabled look in the second / last column; short row names; the Showcase uses the library's own controls (UIStyles factories).

## The check of one unit (six checks)

1. **Bugs.** Hover / pressed / focus / disabled / hidden / no window handle yet; events and their order; `Dispose` and the static events
   (`UIColors.ThemeChanged`, `UIStrings.LanguageChanged`: every subscription needs its unsubscribe); threads (`InvokeRequired`); theme, language and
   DPI changes while the control is shown; keyboard and accessibility; empty, `null`, huge and 0 values; a control that is disposed or removed
   inside one of its own events; `Control.Visible` is false for every child of a window that was never shown (use an own state field); look at
   the real-screen paint path for self-drawn controls.
2. **Refactor.** Duplicated code (also across controls), file size (over 500 lines), names, dead code. **Report first**, implement after the user's OK.
3. **Docs.** An XML `<summary>` on every public member of the unit (turn `CS1591` on for it), `<remarks>` where behavior is not obvious, the README
   section and the Showcase text are true.
4. **Showcase.** Open the page (render it), go through `CONTROLS.md` for the unit: is every property, factory parameter and variant shown, or is
   it said why not? Row order and clarity (`showcase-clarity-review` in the memory).
5. **Tests.** `tools\Run-Coverage.ps1 -Filter "FullyQualifiedName~<tests of the unit>"` then `-Summarize -Uncovered <Name>` lists the lines no test
   reaches. Add tests for the gaps and the edge cases until the unit has **85 %** of its lines, or say why a part is left out.
6. **Table and commit.** `tools\Update-ControlTable.ps1` (the test `ControlTableTests` fails while `CONTROLS.md` is out of date), tick the row in
   `ANALYSIS.md`, report, wait for "commit".

The report of a unit, in German: what was found (bugs with the cause), what was changed, what is proposed (refactoring), coverage before and after,
what is left open.

## The tools

- `tools\Run-IsolatedTests.ps1` runs `dotnet test <arguments>` on a hidden desktop, so test windows can neither take the focus nor cover anything:
  `& .\tools\Run-IsolatedTests.ps1 -Arguments "ErikwnkWFUI.Tests -f net8.0-windows --no-build --filter FullyQualifiedName~Controls.CheckBox" -TimeoutSeconds 600`
  (the arguments come after `dotnet test`; avoid `|` inside a filter, run two filters one after the other). A run right after another one sometimes
  prints no summary: run it again alone.
- `tools\Run-Coverage.ps1` runs the tests with the built-in Microsoft collector (`--collect "Code Coverage;Format=cobertura"`; the coverlet collector
  cannot instrument the WinForms assembly) and writes `coverage\summary.md` (not committed): per control the lines, the covered lines and the percent,
  those under the target first. `-Summarize` reads the last result without a run; `-Summarize -Uncovered ListBox` lists the line ranges of one
  control that no test reaches. A *control* is every file that starts with its name (`ListBox.cs`, `ListBox.Drag.cs`).
- `tools\Update-ControlTable.ps1` regenerates `CONTROLS.md` (generator: `ErikwnkWFUI.Tests\Docs\ControlTableGenerator.cs`, text that the code cannot
  say: `docs\control-notes.json` with the ignored standard properties and the factory class of a control whose name differs).
- `build-and-test.ps1` (repo root): builds both targets, runs the tests, rebuilds the Showcase (`-SkipTests`, `-SkipShowcase`). Close a running Showcase
  first: it locks `ErikwnkWFUI.dll` (the user sometimes has it open in the debugger; then build the library and the tests only and say so).
- **Rendering for a look** (no screen capture): a temporary test file (`ErikwnkWFUI.Tests\Forms\TmpShotTests.cs`, delete it afterwards) that makes the controls
  on an `OffscreenForm` / `AppFormHost`, calls `UIColors.ApplyTheme(...)` for light and dark and saves `Form.DrawToBitmap` as a PNG in the scratchpad;
  zoom with a nearest-neighbor `Graphics.DrawImage`. For a Showcase page: load `ErikwnkWFUI.Showcase.dll` with `Assembly.LoadFrom`, create `ShowcaseForm`,
  select the tab (and a page of a `MultiPanel`) and `DrawToBitmap`; the Showcase must be built first. Read the PNG.
- **Real screen path** (only when needed, and ask the user first): a "QuietForm" (shown without activation, `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`) on the
  visible desktop for about a second and `CopyFromScreen`; `SetFocus(hwnd)` gives a control the focus without taking the foreground; `SetCursorPos` for
  hover, put back afterwards. Never show an activating form: it took the user's keystrokes once.

## Conventions of the repository

- ErikwnkWFUI: sources are **CRLF with a BOM** (`.gitattributes`: `*.cs text eol=crlf`); the repository stores LF. README and `.csproj` files are LF.
  ErikwnkCore (`..\ErikwnkCore`, a separate repository): **LF without a BOM**. Both targets: `net48` and `net8.0-windows` (library), tests on `net8.0-windows`.
- A control: `Controls\<Kind>\X.cs` (parts as `X.Part.cs`), a factory in `Factories\UIXFactory.cs` (internal), the public entry in `UIStyles.Xs.cs`.
  `CreateStandard(..., UIColorSet colors = null)` and `CreatePrimary(...)` (= the primary color set, follows the accent) everywhere; a Compact variant
  where a cell needs the box alone.
- Editing files from PowerShell: write a small `Ed($path, $old, $new)` that detects the file's line ending and BOM and fails when `$old` is missing; a
  file written with LF into a CRLF repository or the other way round makes mixed endings. `String.Replace` with three arguments does not exist in
  Windows PowerShell 5.1. Before every `dotnet test` check that it has a `--filter`.

## Lessons (traps that were found; check the other controls for them)

1. `Control.Visible` is false for every child of a window that was never shown: never base a layout made in the constructor on `child.Visible`.
2. Assigning `Form.CancelButton` gives that button `DialogResult.Cancel`: a real click closes a modal window at once. Test cancel buttons with
   `PerformClick()` (the real path), and reset `DialogResult` to `None` where the window must stay.
3. A frame that the system paints as part of a window (`BorderStyle.FixedSingle`) is overpainted by its scroll bar on hover. Solved before by painting the
   frame in `WM_NCPAINT` / `WM_PRINT` (ListBox, TextField) or as a panel around the control (TitledListBox, the release notes of the update prompt).
4. `DrawToBitmap` (`WM_PRINT`) is not the screen path: four bugs of the TextBox only showed on the screen (the native placeholder, the system's line in the
   non-client strip after `WM_PAINT`, a single line erasing a symbol after click / key / focus messages, a native tooltip made at mouse enter).
5. `UseSystemPasswordChar` recreates the window (the typed text vanished): use `PasswordChar` / `EM_SETPASSWORDCHAR`. Changing `TextBox.ScrollBars`
   also recreates the handle: do it only when it must change.
6. A text box follows the theme by itself even without a handle; a `ThemeWatcher` hears the theme only while the control **has a handle**: a control that is
   hidden or collapsed must work its colors out again when it is shown.
7. Colors: text on a colored fill is white unless white has less than 1.5:1 (the user finds white readable on yellow and orange); neutral and pale fills
   take the best of black and white. The contrast checker of the tests is deliberately not too strict (`ColorContrastHelper.RequiredRatio`).
8. Windows that stand over an owner: `ShowDialog(owner)` throws for a disposed owner; a toast needs a window.

## Open items (from round one; fold them into the units where they belong)

- `UIStyles.TextBoxes.CreateBorderless(colors:)` (a set can only color the text and the selection of a box without a frame).
- Other self-drawn controls on the **real screen** paint path (see lesson 4); the 2 px resize zone of `AppForm`; snapping and layouts of `AppForm`;
  the faint muted gray in the light theme (disabled text); `UILanguage` / `AppLanguage` to unify; `UIStyles.cs` and `UIStyles.Localize.cs` both
  subscribe to `LanguageChanged` (doubled or two jobs?); one shared helper for "measure the text, let the window grow up to the screen, then scroll"
  that is written three times (`InputBoxForm`, `ProgressForm`, `MessageBoxForm`); a Showcase "playground" whose comboboxes build a factory call, a
  live control and a copy button.
- The hang of the full run (phases 0.1 and 1.3): a test that makes a control and does not dispose it leaves it on `UIStrings.LanguageChanged`; a later language switch from another test thread touches its window and blocks. Found and fixed so far: InputBoxTests, MessageBoxFactoryTests, ProgressFormTests (0.1), TextBoxEdgeCaseTests (1.3). Rule: every control a test makes is disposed (host or `using`). Proposal still open: `UIStrings` calls the handlers of controls whose window belongs to another thread with `BeginInvoke`, so a switch from any thread is safe and a forgotten control cannot block it.
- `ListBoxEdgeCaseTests.ADoubleClick_KeepsTheItemSelected_AndRaisesDoubleClick` failed once in a full run and passes alone (3x): WinForms checks `WindowFromPoint` on a mouse up, so a window of a parallel test over the point may take the click. Look at it in unit 5.1.
- `ClipboardAccess`: the 9 lines of `SystemClipboard` (the wrapper of the real clipboard) are never run by a test on purpose; that file group stays under 85 %.
- `ProgressBox.Run` has no colors parameter (`ProgressForm.BarColors` exists since round one; the title bar color set has none for the box).
- **SelfUpdater / updater (ErikwnkCore):** no checksum or signature of the download (needs a hash in the release, for example a `.sha256` asset) and no
  https / host check of `downloadUrl`; programs in `C:\Program Files` cannot be swapped by a normal user (`Move-Item` fails silently, the old exe
  restarts); `GitHubUpdateChecker` takes the first `.exe` asset; `UpdateVersionParser.Normalize` drops the 4th number; `%TEMP%\{app}_update` is
  predictable. (Done: apostrophe-safe swap script, `MZ` and length check, `x.y.0` display, http(s) only for the release page.)
- Controls for later (not in this round unless the user wants them): `DateTimePicker`, `MonthCalendar`, `TreeView`, `MenuStrip`, `ToolStrip`,
  `StatusStrip`, `SplitContainer`, `LinkLabel`, `RichTextBox`, `MaskedTextBox`; a `CheckedListBox` as a variant of `ListBox`; a segmented
  (button-like) choice; a collapsible `GroupBox`.
- Right-to-left is not supported library-wide (say so in the README).

### The `[Obsolete]` aliases (keep until the user says his projects are migrated)

`UIStyles.TextBoxes.CreateBorderstyleNone` (-> `CreateBorderless`), `UIColumn` (-> `PropertyColumn`, file `Controls\PropertyTable\UIColumn.cs`),
`StyledForm` (-> `AppForm`), `StyledFormOptions` (-> `FormOptions`), `StyledFormType`, `StyledOptionsForm` (-> `OptionsForm`) (all four in
`Forms\ObsoleteNames.cs`). Each has a test that pins the old name (`TextBoxFactoryTests`, `PropertyColumnTests`, `FormNameTests`) that goes away with it.
The user's projects that use them are listed in the memory note `obsolete-aliases-to-remove`. Do not remove an alias before the user says so.

## The end of round two

`CHANGELOG.md`, then `docs/MIGRATION.md` (the memory note `migration-guide-plan` has the running list of API and behavior changes: controls, naming,
what can be simplified in the user's projects), then the user pushes. Any API change made during round two is added to the migration list at once.

## Where the memory lives

The user's Claude memory for this repository is `C:\Users\Kosta\.claude\projects\C--Users-Kosta-source-repos-ErikwnkWFUI\memory\` (`MEMORY.md` is
the index that a session loads). It holds the working rules, the lessons, the per-control notes and the plans quoted here. This file and
`ANALYSIS.md` are the repository's copy of what a new session needs; if something here and in the memory differ, ask the user.