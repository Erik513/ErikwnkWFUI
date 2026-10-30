# Brief for the chat of a project: move it to the new ErikwnkWFUI

**For the AI in a project chat.** The user opens a chat in one of his projects and says only: *"Read
`C:\Users\Kosta\source\repos\ErikwnkWFUI\docs\MIGRATE-A-PROJECT.md` and migrate this project."* Everything
you need is here and in the files named here. Do not ask the user for what is written down.

## Working rules (the user's)

- Answer the user in **German**; code, comments and documents in English (as the project does it).
- **Commit only when the user says so.** Never push.
- Do not take the focus of the user's screen: build and run tests (hidden or off-screen), do not show windows
  on the real screen without asking first.
- Keep the PC responsive: no repeated full builds, announce a heavy step.
- Change **this project only**. Do not edit the library (`..\ErikwnkWFUI`) - the one exception is the feedback
  file below. If the library is wrong, write it down there.

## What happened

ErikwnkWFUI (the shared WinForms library, a sibling folder `..\ErikwnkWFUI`, referenced by a
`ProjectReference`, so the project always builds against the current source) was cleaned up control by control
and analysed as a whole. Names changed, some behavior changed. The library keeps the most renamed names as
`[Obsolete]` aliases (they build with a warning), the others are gone.

Read these, in this order (all in `C:\Users\Kosta\source\repos\ErikwnkWFUI`):

1. `docs\MIGRATION.md` - what to change (the renames, the changes of look and behavior, what can be simpler).
2. `CHANGELOG.md` - the full list of changes, if something is not clear.
3. `docs\CONTROLS.md` (every control, property and default) and `README.md` - the API, when you need to look
   something up. The sources are the truth: `ErikwnkWFUI\...`.

## Steps

1. **Find out** which projects and target frameworks there are (`*.csproj`, `net48` and/or `net8.0-windows`).
   Build once and keep the list of errors and of `[Obsolete]` warnings (`CS0618`). Do not run the program yet.
2. **Fix the errors** with the table "Gone (no alias)" of `MIGRATION.md`. Where it is not clear what the new name
   does, read the source or `CONTROLS.md` instead of guessing.
3. **Replace every obsolete name** (the table "Still works, but renamed") and build until there is **no
   `CS0618` warning** about ErikwnkWFUI left. Candidates found on 2026-10-30 are in the table below; run the
   search yourself again, it is the truth:
   `grep -rnw --include=*.cs "StyledForm\|StyledFormOptions\|StyledFormType\|StyledOptionsForm\|UIColumn\|CreateBorderstyleNone\|CreateThemeToggle\|MessageBoxSize" .`
4. **Look for the behavior changes** of section 2 of `MIGRATION.md` in the code (they compile, but act
   differently). The ones that need a decision of the user are: a `MessageBox.Show` with Yes/No (or
   Abort/Retry/Ignore) whose result is compared with `Cancel`, and code that relies on Enter = Yes (the box
   now defaults to the *safe* button; `defaultButton: Button1` brings back the old Enter). Fix what is clearly a
   bug; **ask the user** only for a real choice, once, with all questions together.
5. **Simplify** (section 3 of `MIGRATION.md`) **only what is safe and small**: hand-made settings windows,
   `InputBox`, own text-box lock buttons, own progress windows. Do not rewrite a screen that works. List the
   bigger chances in the report instead of doing them.
6. **Build both frameworks the project targets, run the project's tests** (if it has some), and look at the designer
   files: open and save a form only if it is quick; otherwise tell the user to do it once (the designer now writes
   only the properties that were set, so `Designer.cs` may change).
7. **Report** (in German, short): the files changed, what was renamed, the behavior decisions, the number of
   warnings left (must be 0 about ErikwnkWFUI), what the user should look at on the screen, and the **library
   findings** (next section). Then wait for "commit".

## Report what the library should know (the feedback file)

Everything that is wrong, missing or surprising **in the library** - a name that is hard to find, a property that
was needed and is gone, a behavior that looks like a bug, a rename that `MIGRATION.md` does not list, a sentence
of `MIGRATION.md` that is wrong - append to
`C:\Users\Kosta\source\repos\ErikwnkWFUI\docs\MIGRATION-FEEDBACK.md` (the only library file you touch), under
the heading of this project, one bullet each: *what you found, where (file:line in the project), what would
help*. Do not fix the library yourself. If nothing: write "nothing" under the heading.

## Projects that use the library (found on 2026-10-30)

All reference `..\..\ErikwnkWFUI\ErikwnkWFUI\ErikwnkWFUI.csproj`. Obsolete or removed names the search found
(candidates: check that the name really is the library's):

| Project (folder in `source\repos`) | Found |
|---|---|
| `DealOrNoDeal` | `StyledForm`, `StyledFormOptions`, `MessageBoxSize` |
| `LyricsSync` (`src\LyricsSync.WinForms`) | `StyledForm`, `StyledFormOptions`, `StyledFormType`, `UIColumn`, `MessageBoxSize`, `VersionStrip` (still exists) |
| `ReleaseUpdaterGui` | `StyledForm`, `StyledFormOptions`, `UIColumn`, `CreateBorderstyleNone`, `MessageBoxSize` |
| `SmartRoutine` (+ `SmartRoutine.Tests`) | `StyledForm`, `StyledFormOptions`, `UIColumn`, `CreateBorderstyleNone`, `MessageBoxSize` |
| `ViewerForTelegram` | `StyledForm`, `StyledFormOptions`, `UIColumn`, `CreateBorderstyleNone`, `SliderBar`, `VolumeSlider` (-> `TrackBar`), `VersionStrip` (still exists) |
| `WViewer` | `StyledFormType`, `StyledOptionsForm`, `CreateSmall` (switch -> `size:`), `MessageBoxSize`, `VersionStrip` (still exists) |

`ProjectPreset` is a template with its own `UIStyles` helper (not the library): leave it unless the user asks.

## When you are done

Say in German what is left for the user (look at the windows once, the designer files, the commit). The user
checks 7.5 (the windows at 125 / 150 / 200 % scaling) and the removal of the `[Obsolete]` aliases in the library
**after all projects are migrated**: tell him in one line if this project had no obsolete name left.
