using System;
using System.Collections.Generic;
using System.Globalization;
using ErikwnkCore;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Localization
{
    /// <summary>
    /// User-facing text for the handful of built-in dialogs/controls that ship
    /// their own copy (update prompt, title bar tooltips). Defaults to English so
    /// existing consumers see no change; set <see cref="Language"/> (e.g. via
    /// UIStyles.Language) to switch everything at once, at startup or live at
    /// runtime - LanguageChanged lets already-built controls (like the title
    /// bar's own tooltips) react immediately.
    /// </summary>
    internal static class UIStrings
    {
        private static UILanguage _language = UILanguage.English;

        // The language of the library and the language of the program's own texts (AppLocalization) are ONE setting: whichever is set, the
        // other follows, so nobody has to remember to set both. The library adopts what AppLocalization already has when it is first used.
        static UIStrings()
        {
            _language = ToUILanguage(AppLocalization.Language);
            AppLocalization.LanguageChanged += (sender, e) =>
            {
                UILanguage language = ToUILanguage(AppLocalization.Language);

                if (_language != language)
                    Language = language;
            };
        }

        private static UILanguage ToUILanguage(AppLanguage language)
        {
            return language == AppLanguage.German ? UILanguage.German : UILanguage.English;
        }

        public static UILanguage Language
        {
            get { return _language; }
            set
            {
                if (_language == value)
                    return;

                _language = value;
                AppLocalization.Language = value == UILanguage.German ? AppLanguage.German : AppLanguage.English;
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler LanguageChanged;

        /// <summary>
        /// The culture a number is written in, by the language of the library (not the one of the computer): the decimal sign and the
        /// way a percentage is written ("42%" in English, "42 %" in German).
        /// </summary>
        public static CultureInfo NumberCulture
        {
            get { return CultureInfo.GetCultureInfo(_language == UILanguage.German ? "de-DE" : "en-US"); }
        }

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["TitleBar.Minimize"] = "Minimize",
            ["TitleBar.Maximize"] = "Maximize",
            ["TitleBar.Restore"] = "Restore",
            ["TitleBar.Close"] = "Close",

            ["InputBox.Title"] = "Input",
            ["InputBox.Ok"] = "OK",
            ["InputBox.Cancel"] = "Cancel",
            ["OptionsForm.Title"] = "Options",
            ["OptionsForm.Save"] = "Save",
            ["OptionsForm.Cancel"] = "Cancel",
            ["OptionsForm.General"] = "General",
            ["OptionsForm.SwitchToLight"] = "Switch to light theme",
            ["OptionsForm.SwitchToDark"] = "Switch to dark theme",
            ["OptionsForm.AccentColor"] = "Accent color",
            ["OptionsForm.Language"] = "Language",
            ["OptionsForm.AlwaysOnTop"] = "Always on top",
            ["OptionsForm.StartWithWindows"] = "Start with Windows",
            ["OptionsForm.ResetToDefaults"] = "Reset to defaults",
            ["OptionsForm.Saved"] = "Settings saved",
            ["OptionsForm.DefaultsRestored"] = "Defaults restored",
            ["OptionsForm.Accent.Blue"] = "Blue",
            ["OptionsForm.Accent.Red"] = "Red",
            ["OptionsForm.Accent.Orange"] = "Orange",
            ["OptionsForm.Accent.Amber"] = "Amber",
            ["OptionsForm.Accent.Yellow"] = "Yellow",
            ["OptionsForm.Accent.Green"] = "Green",
            ["OptionsForm.Accent.Teal"] = "Teal",
            ["OptionsForm.Accent.Cyan"] = "Cyan",
            ["OptionsForm.Accent.Indigo"] = "Indigo",
            ["OptionsForm.Accent.Purple"] = "Purple",
            ["OptionsForm.Accent.Magenta"] = "Magenta",
            ["OptionsForm.Accent.Pink"] = "Pink",
            ["OptionsForm.Accent.Brown"] = "Brown",
            ["OptionsForm.Accent.Gray"] = "Gray",
            ["OptionsForm.Accent.BlackOrWhite"] = "Black/White",

            ["UpdateAvailable.Title"] = "Update available",
            ["UpdateAvailable.Message"] = "A new version is available.",
            ["UpdateAvailable.Later"] = "Later",
            ["UpdateAvailable.UpdateNow"] = "Update now",
            ["UpdateAvailable.Downloading"] = "Downloading update... {0}%",

            ["Update.Title"] = "Update",
            ["Update.DownloadFailedMessage"] = "The update download failed. Opening the release page instead.",
            ["Update.AvailableTooltip"] = "A new version is available - click to update",

            ["MessageBox.Cancel"] = "Cancel",

            ["TabControl.AddTab"] = "Add tab",
            ["TabControl.RenameTab"] = "Rename tab",
            ["TabControl.CloseTab"] = "Close tab",
            ["TabControl.NewTabTitle"] = "New tab",
            ["TabControl.LockedRenameAndClose"] = "This tab cannot be renamed or closed",
            ["TabControl.LockedRename"] = "This tab cannot be renamed",
            ["TabControl.LockedClose"] = "This tab cannot be closed",
            ["MultiPanel.PreviousPage"] = "Previous page",
            ["MultiPanel.NextPage"] = "Next page",
            ["VolumeControl.Mute"] = "Mute",
            ["VolumeControl.Unmute"] = "Unmute",
            ["VolumeControl.Volume"] = "Volume",
            ["ToggleSwitch.Toggle"] = "Switch",
            ["TextBox.Edit"] = "Edit",
            ["TextBox.Accept"] = "Save",
            ["TextBox.ShowPassword"] = "Show password",
            ["TextBox.HidePassword"] = "Hide password",
            ["TabControl.ScrollLeft"] = "Scroll tabs left",
            ["TabControl.ScrollRight"] = "Scroll tabs right",
            ["TabControl.ScrollUp"] = "Scroll tabs up",
            ["TabControl.ScrollDown"] = "Scroll tabs down",

            ["ListView.CopySelection"] = "Copy",
            ["ListView.CopyAll"] = "Copy all",
            ["ListView.CopySelectionWithHeader"] = "Copy with header",
            ["ListView.CopyAllWithHeader"] = "Copy all with header",
            ["ListView.SelectAll"] = "Select all",
            ["ListView.RowCopied"] = "Row copied",
            ["ListView.RowsCopied"] = "{0} rows copied",
            ["ListView.WithHeaderSuffix"] = " (with header)",

            ["DataGridView.WithHeaderSuffix"] = " (with header)",
            ["DataGridView.DeleteRow"] = "Delete row",
            ["DataGridView.DeleteRowHeader"] = "Del",
            ["DataGridView.AddRow"] = "Add row",
            ["DataGridView.EnumerationHeader"] = "#",
            ["DataGridView.RowNumber"] = "Row number",
            ["DataGridView.CellCopied"] = "Cell copied",
            ["DataGridView.CellsCopied"] = "{0} cells copied",
            ["DataGridView.CellCut"] = "Cell cut",
            ["DataGridView.CellsCut"] = "{0} cells cut",
            ["DataGridView.CellCleared"] = "Cell cleared",
            ["DataGridView.CellsCleared"] = "{0} cells cleared",
            ["DataGridView.RowPasted"] = "Row pasted",
            ["DataGridView.RowsPasted"] = "{0} rows pasted",
            ["DataGridView.CellsPasted"] = "{0} cells pasted",
            ["DataGridView.RowDeleted"] = "Row deleted",
            ["DataGridView.RowsDeleted"] = "{0} rows deleted",
            ["DataGridView.RowInserted"] = "Row inserted",
            ["DataGridView.RowCut"] = "Row cut",
            ["DataGridView.RowsCut"] = "{0} rows cut",
            ["DataGridView.ContextMenuCut"] = "Cut cells",
            ["DataGridView.ContextMenuCutRows"] = "Cut rows",
            ["DataGridView.ContextMenuCopySelection"] = "Copy",
            ["DataGridView.ContextMenuCopySelectionWithHeader"] = "Copy with header",
            ["DataGridView.ContextMenuCopyAll"] = "Copy all",
            ["DataGridView.ContextMenuCopyAllWithHeader"] = "Copy all with header",
            ["DataGridView.ContextMenuSelectAll"] = "Select all",
            ["DataGridView.ContextMenuPaste"] = "Paste",
            ["DataGridView.ContextMenuClear"] = "Delete cells",
            ["DataGridView.ContextMenuDeleteRows"] = "Delete rows",
            ["DataGridView.ContextMenuInsertRowAbove"] = "Insert row above",
            ["DataGridView.ContextMenuInsertRowBelow"] = "Insert row below",
        };

        private static readonly Dictionary<string, string> German = new Dictionary<string, string>
        {
            ["TitleBar.Minimize"] = "Minimieren",
            ["TitleBar.Maximize"] = "Maximieren",
            ["TitleBar.Restore"] = "Wiederherstellen",
            ["TitleBar.Close"] = "Schließen",

            ["InputBox.Title"] = "Eingabe",
            ["InputBox.Ok"] = "OK",
            ["InputBox.Cancel"] = "Abbrechen",
            ["OptionsForm.Title"] = "Optionen",
            ["OptionsForm.Save"] = "Speichern",
            ["OptionsForm.Cancel"] = "Abbrechen",
            ["OptionsForm.General"] = "Allgemein",
            ["OptionsForm.SwitchToLight"] = "Zum hellen Design wechseln",
            ["OptionsForm.SwitchToDark"] = "Zum dunklen Design wechseln",
            ["OptionsForm.AccentColor"] = "Akzentfarbe",
            ["OptionsForm.Language"] = "Sprache",
            ["OptionsForm.AlwaysOnTop"] = "Immer im Vordergrund",
            ["OptionsForm.StartWithWindows"] = "Mit Windows starten",
            ["OptionsForm.ResetToDefaults"] = "Auf Standard zurücksetzen",
            ["OptionsForm.Saved"] = "Einstellungen gespeichert",
            ["OptionsForm.DefaultsRestored"] = "Standardwerte wiederhergestellt",
            ["OptionsForm.Accent.Blue"] = "Blau",
            ["OptionsForm.Accent.Red"] = "Rot",
            ["OptionsForm.Accent.Orange"] = "Orange",
            ["OptionsForm.Accent.Amber"] = "Bernstein",
            ["OptionsForm.Accent.Yellow"] = "Gelb",
            ["OptionsForm.Accent.Green"] = "Grün",
            ["OptionsForm.Accent.Teal"] = "Blaugrün",
            ["OptionsForm.Accent.Cyan"] = "Cyan",
            ["OptionsForm.Accent.Indigo"] = "Indigo",
            ["OptionsForm.Accent.Purple"] = "Lila",
            ["OptionsForm.Accent.Magenta"] = "Magenta",
            ["OptionsForm.Accent.Pink"] = "Rosa",
            ["OptionsForm.Accent.Brown"] = "Braun",
            ["OptionsForm.Accent.Gray"] = "Grau",
            ["OptionsForm.Accent.BlackOrWhite"] = "Schwarz/Weiß",

            ["UpdateAvailable.Title"] = "Update verfügbar",
            ["UpdateAvailable.Message"] = "Eine neue Version ist verfügbar.",
            ["UpdateAvailable.Later"] = "Später",
            ["UpdateAvailable.UpdateNow"] = "Jetzt aktualisieren",
            ["UpdateAvailable.Downloading"] = "Update wird heruntergeladen... {0} %",

            ["Update.Title"] = "Update",
            ["Update.DownloadFailedMessage"] = "Der Update-Download ist fehlgeschlagen. Die Release-Seite wird stattdessen geöffnet.",
            ["Update.AvailableTooltip"] = "Eine neue Version ist verfügbar - zum Aktualisieren klicken",

            ["MessageBox.Cancel"] = "Abbrechen",

            ["TabControl.AddTab"] = "Tab hinzufügen",
            ["TabControl.RenameTab"] = "Tab umbenennen",
            ["TabControl.CloseTab"] = "Tab schließen",
            ["TabControl.NewTabTitle"] = "Neuer Tab",
            ["TabControl.LockedRenameAndClose"] = "Dieser Tab kann nicht umbenannt oder geschlossen werden",
            ["TabControl.LockedRename"] = "Dieser Tab kann nicht umbenannt werden",
            ["TabControl.LockedClose"] = "Dieser Tab kann nicht geschlossen werden",
            ["MultiPanel.PreviousPage"] = "Vorherige Seite",
            ["MultiPanel.NextPage"] = "Nächste Seite",
            ["VolumeControl.Mute"] = "Stumm schalten",
            ["VolumeControl.Unmute"] = "Ton einschalten",
            ["VolumeControl.Volume"] = "Lautstärke",
            ["ToggleSwitch.Toggle"] = "Umschalten",
            ["TextBox.Edit"] = "Bearbeiten",
            ["TextBox.Accept"] = "Speichern",
            ["TextBox.ShowPassword"] = "Passwort anzeigen",
            ["TextBox.HidePassword"] = "Passwort verbergen",
            ["TabControl.ScrollLeft"] = "Tabs nach links scrollen",
            ["TabControl.ScrollRight"] = "Tabs nach rechts scrollen",
            ["TabControl.ScrollUp"] = "Tabs nach oben scrollen",
            ["TabControl.ScrollDown"] = "Tabs nach unten scrollen",

            ["ListView.CopySelection"] = "Kopieren",
            ["ListView.CopyAll"] = "Alles kopieren",
            ["ListView.CopySelectionWithHeader"] = "Kopieren mit Kopfzeile",
            ["ListView.CopyAllWithHeader"] = "Alles kopieren mit Kopfzeile",
            ["ListView.SelectAll"] = "Alles auswählen",
            ["ListView.RowCopied"] = "Zeile kopiert",
            ["ListView.RowsCopied"] = "{0} Zeilen kopiert",
            ["ListView.WithHeaderSuffix"] = " (mit Kopfzeile)",

            ["DataGridView.WithHeaderSuffix"] = " (mit Kopfzeile)",
            ["DataGridView.DeleteRow"] = "Zeile löschen",
            ["DataGridView.DeleteRowHeader"] = "Entf",
            ["DataGridView.AddRow"] = "Zeile hinzufügen",
            ["DataGridView.EnumerationHeader"] = "#",
            ["DataGridView.RowNumber"] = "Zeilennummer",
            ["DataGridView.CellCopied"] = "Zelle kopiert",
            ["DataGridView.CellsCopied"] = "{0} Zellen kopiert",
            ["DataGridView.CellCut"] = "Zelle ausgeschnitten",
            ["DataGridView.CellsCut"] = "{0} Zellen ausgeschnitten",
            ["DataGridView.CellCleared"] = "Zelle geleert",
            ["DataGridView.CellsCleared"] = "{0} Zellen geleert",
            ["DataGridView.RowPasted"] = "Zeile eingefügt",
            ["DataGridView.RowsPasted"] = "{0} Zeilen eingefügt",
            ["DataGridView.CellsPasted"] = "{0} Zellen eingefügt",
            ["DataGridView.RowDeleted"] = "Zeile gelöscht",
            ["DataGridView.RowsDeleted"] = "{0} Zeilen gelöscht",
            ["DataGridView.RowInserted"] = "Zeile eingefügt",
            ["DataGridView.RowCut"] = "Zeile ausgeschnitten",
            ["DataGridView.RowsCut"] = "{0} Zeilen ausgeschnitten",
            ["DataGridView.ContextMenuCut"] = "Zellen ausschneiden",
            ["DataGridView.ContextMenuCutRows"] = "Zeilen ausschneiden",
            ["DataGridView.ContextMenuCopySelection"] = "Kopieren",
            ["DataGridView.ContextMenuCopySelectionWithHeader"] = "Kopieren mit Kopfzeile",
            ["DataGridView.ContextMenuCopyAll"] = "Alles kopieren",
            ["DataGridView.ContextMenuCopyAllWithHeader"] = "Alles kopieren mit Kopfzeile",
            ["DataGridView.ContextMenuSelectAll"] = "Alles auswählen",
            ["DataGridView.ContextMenuPaste"] = "Einfügen",
            ["DataGridView.ContextMenuClear"] = "Zellen löschen",
            ["DataGridView.ContextMenuDeleteRows"] = "Zeilen löschen",
            ["DataGridView.ContextMenuInsertRowAbove"] = "Zeile oberhalb hinzufügen",
            ["DataGridView.ContextMenuInsertRowBelow"] = "Zeile unterhalb hinzufügen",
        };

        public static string Get(string key)
        {
            Dictionary<string, string> table = Language == UILanguage.German ? German : English;

            string value;
            if (table.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }
    }
}
