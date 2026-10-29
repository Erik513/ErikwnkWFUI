using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        // The ready-made symbol buttons of UIStyles.Buttons, in a file of their own (UIStyles.Buttons.cs has the factories).
        public static partial class Buttons
        {
            /// <summary>
            /// Ready-made designs - there is one for every symbol of <see cref="Symbols"/>, and the
            /// buttons most programs need (Add, Delete, Save, Browse ...) come first. Each one is a symbol with
            /// its own fixed colors (green to add, save or confirm, red to delete, close or stop, blue to edit, reload
            /// or send, yellow to choose a file or folder, dark gray for navigation and everything
            /// without a color of its own), the same in Dark and
            /// Light and with any accent. Hand one to
            /// <see cref="CreateSymbol(UIButtonPreset, string, Size?)"/> to get the button. A preset
            /// you would like a little different is a copy away
            /// (<see cref="UIButtonPreset.WithColors"/>, <see cref="UIButtonPreset.WithSymbol"/>), and
            /// your own presets are made with <see cref="UIButtonPreset"/>.
            /// </summary>
            public static class Presets
            {
                // The symbol of a preset is white in every state. The fills are those of
                // the set, only a fill too bright for a white symbol (3:1) is darkened a
                // little. The yellow keeps its bright fills: white on yellow is its look.
                //
                // Worked out once per set: the presets of one color share the result (there are a few
                // sets and about eighty presets, and every one of them is made when the first is used).
                private static readonly System.Collections.Generic.Dictionary<UIColorSet, UIColorSet> WhiteOnSets =
                    new System.Collections.Generic.Dictionary<UIColorSet, UIColorSet>();

                private static UIColorSet WhiteOn(UIColorSet set)
                {
                    UIColorSet result;

                    if (!WhiteOnSets.TryGetValue(set, out result))
                    {
                        result = ReferenceEquals(set, Colors.Sets.Yellow)
                            ? UIColors.WithText(set, UIColors.White)
                            : UIColors.WithText(set, UIColors.White, 3.0);

                        WhiteOnSets.Add(set, result);
                    }

                    return result;
                }

                /// <summary>Adding an entry: a plus on green.</summary>
                public static UIButtonPreset Add { get; } = new UIButtonPreset(Symbols.Add, WhiteOn(Colors.Sets.Green));

                /// <summary>Saving: a disk on green.</summary>
                public static UIButtonPreset Save { get; } = new UIButtonPreset(Symbols.Save, WhiteOn(Colors.Sets.Green));

                /// <summary>Deleting an entry: a trash can on red.</summary>
                public static UIButtonPreset Delete { get; } = new UIButtonPreset(Symbols.Delete, WhiteOn(Colors.Sets.Red));

                /// <summary>Closing a window or dialog: a cross on red.</summary>
                public static UIButtonPreset Close { get; } = new UIButtonPreset(Symbols.Close, WhiteOn(Colors.Sets.Red));

                /// <summary>Editing an entry: a pencil on blue.</summary>
                public static UIButtonPreset Edit { get; } = new UIButtonPreset(Symbols.Edit, WhiteOn(Colors.Sets.Blue));

                /// <summary>Reloading: a circular arrow on blue.</summary>
                public static UIButtonPreset Refresh { get; } = new UIButtonPreset(Symbols.Refresh, WhiteOn(Colors.Sets.Blue));

                /// <summary>Logging in: a person on blue.</summary>
                public static UIButtonPreset Login { get; } = new UIButtonPreset(Symbols.User, WhiteOn(Colors.Sets.Blue));

                /// <summary>Syncing two sides: two arrows chasing each other, on blue.</summary>
                public static UIButtonPreset Sync { get; } = new UIButtonPreset(Symbols.Sync, WhiteOn(Colors.Sets.Blue));

                /// <summary>Updating or restoring: a circular arrow against the clock, on green.</summary>
                public static UIButtonPreset Update { get; } = new UIButtonPreset(Symbols.Update, WhiteOn(Colors.Sets.Green));

                /// <summary>Searching: a magnifier on dark gray.</summary>
                public static UIButtonPreset Search { get; } = new UIButtonPreset(Symbols.Search, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening the settings: a gear on dark gray.</summary>
                public static UIButtonPreset Settings { get; } = new UIButtonPreset(Symbols.Settings, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going back: an arrow to the left on dark gray.</summary>
                public static UIButtonPreset Back { get; } = new UIButtonPreset(Symbols.Back, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going forward: an arrow to the right on dark gray.</summary>
                public static UIButtonPreset Forward { get; } = new UIButtonPreset(Symbols.Forward, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one level up: an arrow up on dark gray.</summary>
                public static UIButtonPreset Up { get; } = new UIButtonPreset(Symbols.Up, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one level down: an arrow down on dark gray.</summary>
                public static UIButtonPreset Down { get; } = new UIButtonPreset(Symbols.Down, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going to the start: a house on dark gray.</summary>
                public static UIButtonPreset Home { get; } = new UIButtonPreset(Symbols.Home, WhiteOn(Colors.Sets.Dark));

                /// <summary>Choosing a file: a document on yellow.</summary>
                public static UIButtonPreset OpenFile { get; } = new UIButtonPreset(Symbols.File, WhiteOn(Colors.Sets.Yellow));

                /// <summary>Choosing a folder: a folder on yellow.</summary>
                public static UIButtonPreset Browse { get; } = new UIButtonPreset(Symbols.Folder, WhiteOn(Colors.Sets.Yellow));

                // ---- one for every other symbol, named like the symbol ----

                /// <summary>Taking an item out of a list: a minus, on red.</summary>
                public static UIButtonPreset Remove { get; } = new UIButtonPreset(Symbols.Remove, WhiteOn(Colors.Sets.Red));

                /// <summary>Cancelling: a heavy cross, on red.</summary>
                public static UIButtonPreset Cancel { get; } = new UIButtonPreset(Symbols.Cancel, WhiteOn(Colors.Sets.Red));

                /// <summary>Confirming: a check mark, on green.</summary>
                public static UIButtonPreset Check { get; } = new UIButtonPreset(Symbols.Check, WhiteOn(Colors.Sets.Green));

                /// <summary>Undoing the last step: a curved arrow back, on dark gray.</summary>
                public static UIButtonPreset Undo { get; } = new UIButtonPreset(Symbols.Undo, WhiteOn(Colors.Sets.Dark));

                /// <summary>Redoing a step: a curved arrow forward, on dark gray.</summary>
                public static UIButtonPreset Redo { get; } = new UIButtonPreset(Symbols.Redo, WhiteOn(Colors.Sets.Dark));

                /// <summary>Cutting: scissors, on dark gray.</summary>
                public static UIButtonPreset Cut { get; } = new UIButtonPreset(Symbols.Cut, WhiteOn(Colors.Sets.Dark));

                /// <summary>Copying: two overlapping sheets, on dark gray.</summary>
                public static UIButtonPreset Copy { get; } = new UIButtonPreset(Symbols.Copy, WhiteOn(Colors.Sets.Dark));

                /// <summary>Pasting: a clipboard with a sheet, on dark gray.</summary>
                public static UIButtonPreset Paste { get; } = new UIButtonPreset(Symbols.Paste, WhiteOn(Colors.Sets.Dark));

                /// <summary>Printing: a printer, on dark gray.</summary>
                public static UIButtonPreset Print { get; } = new UIButtonPreset(Symbols.Print, WhiteOn(Colors.Sets.Dark));

                /// <summary>Downloading: an arrow down onto a line, on blue.</summary>
                public static UIButtonPreset Download { get; } = new UIButtonPreset(Symbols.Download, WhiteOn(Colors.Sets.Blue));

                /// <summary>Uploading: an arrow up from a line, on blue.</summary>
                public static UIButtonPreset Upload { get; } = new UIButtonPreset(Symbols.Upload, WhiteOn(Colors.Sets.Blue));

                /// <summary>Sharing: a box with an arrow out of it, on blue.</summary>
                public static UIButtonPreset Share { get; } = new UIButtonPreset(Symbols.Share, WhiteOn(Colors.Sets.Blue));

                /// <summary>Filtering: a funnel, on dark gray.</summary>
                public static UIButtonPreset Filter { get; } = new UIButtonPreset(Symbols.Filter, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sorting: lines of different length, on dark gray.</summary>
                public static UIButtonPreset Sort { get; } = new UIButtonPreset(Symbols.Sort, WhiteOn(Colors.Sets.Dark));

                /// <summary>Zooming in: a magnifier with a plus, on dark gray.</summary>
                public static UIButtonPreset ZoomIn { get; } = new UIButtonPreset(Symbols.ZoomIn, WhiteOn(Colors.Sets.Dark));

                /// <summary>Zooming out: a magnifier with a minus, on dark gray.</summary>
                public static UIButtonPreset ZoomOut { get; } = new UIButtonPreset(Symbols.ZoomOut, WhiteOn(Colors.Sets.Dark));

                /// <summary>Full screen: arrows to the corners, on dark gray.</summary>
                public static UIButtonPreset Fullscreen { get; } = new UIButtonPreset(Symbols.Fullscreen, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sending: a paper plane, on blue.</summary>
                public static UIButtonPreset Send { get; } = new UIButtonPreset(Symbols.Send, WhiteOn(Colors.Sets.Blue));

                /// <summary>A document: the outlined page with lines of text (emoji font), on dark gray.</summary>
                public static UIButtonPreset Document { get; } = new UIButtonPreset(Symbols.Document, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening a folder: an open folder, on dark gray.</summary>
                public static UIButtonPreset FolderOpen { get; } = new UIButtonPreset(Symbols.FolderOpen, WhiteOn(Colors.Sets.Dark));

                /// <summary>Opening a menu: three lines, on dark gray.</summary>
                public static UIButtonPreset Menu { get; } = new UIButtonPreset(Symbols.Menu, WhiteOn(Colors.Sets.Dark));

                /// <summary>Marking something that goes up, or sorting upward: a solid triangle, on dark gray.</summary>
                public static UIButtonPreset TriangleUp { get; } = new UIButtonPreset(Symbols.TriangleUp, WhiteOn(Colors.Sets.Dark));

                /// <summary>Marking something that goes down, or sorting downward: a solid triangle, on dark gray.</summary>
                public static UIButtonPreset TriangleDown { get; } = new UIButtonPreset(Symbols.TriangleDown, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one page back: a small arrow to the left, on dark gray.</summary>
                public static UIButtonPreset ChevronLeft { get; } = new UIButtonPreset(Symbols.ChevronLeft, WhiteOn(Colors.Sets.Dark));

                /// <summary>Going one page on: a small arrow to the right, on dark gray.</summary>
                public static UIButtonPreset ChevronRight { get; } = new UIButtonPreset(Symbols.ChevronRight, WhiteOn(Colors.Sets.Dark));

                /// <summary>Collapsing: a small arrow up, on dark gray.</summary>
                public static UIButtonPreset ChevronUp { get; } = new UIButtonPreset(Symbols.ChevronUp, WhiteOn(Colors.Sets.Dark));

                /// <summary>Expanding: a small arrow down, on dark gray.</summary>
                public static UIButtonPreset ChevronDown { get; } = new UIButtonPreset(Symbols.ChevronDown, WhiteOn(Colors.Sets.Dark));

                /// <summary>Starting: a triangle, on green.</summary>
                public static UIButtonPreset Play { get; } = new UIButtonPreset(Symbols.Play, WhiteOn(Colors.Sets.Green));

                /// <summary>Pausing: two bars, on dark gray.</summary>
                public static UIButtonPreset Pause { get; } = new UIButtonPreset(Symbols.Pause, WhiteOn(Colors.Sets.Dark));

                /// <summary>Stopping: a square, on red.</summary>
                public static UIButtonPreset Stop { get; } = new UIButtonPreset(Symbols.Stop, WhiteOn(Colors.Sets.Red));

                /// <summary>Recording: a circle, on red.</summary>
                public static UIButtonPreset Record { get; } = new UIButtonPreset(Symbols.Record, WhiteOn(Colors.Sets.Red));

                /// <summary>Skipping to the previous item, on dark gray.</summary>
                public static UIButtonPreset SkipBack { get; } = new UIButtonPreset(Symbols.SkipBack, WhiteOn(Colors.Sets.Dark));

                /// <summary>Skipping to the next item, on dark gray.</summary>
                public static UIButtonPreset SkipForward { get; } = new UIButtonPreset(Symbols.SkipForward, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sound on: a loudspeaker, on dark gray.</summary>
                public static UIButtonPreset Volume { get; } = new UIButtonPreset(Symbols.Volume, WhiteOn(Colors.Sets.Dark));

                /// <summary>Sound off: a crossed loudspeaker, on dark gray.</summary>
                public static UIButtonPreset Mute { get; } = new UIButtonPreset(Symbols.Mute, WhiteOn(Colors.Sets.Dark));

                /// <summary>Music: a note, on purple.</summary>
                public static UIButtonPreset Music { get; } = new UIButtonPreset(Symbols.Music, WhiteOn(Colors.Sets.Purple));

                /// <summary>Random order: crossing arrows, on dark gray.</summary>
                public static UIButtonPreset Shuffle { get; } = new UIButtonPreset(Symbols.Shuffle, WhiteOn(Colors.Sets.Dark));

                /// <summary>Repeating: arrows in a loop, on dark gray.</summary>
                public static UIButtonPreset Repeat { get; } = new UIButtonPreset(Symbols.Repeat, WhiteOn(Colors.Sets.Dark));

                /// <summary>Information: an i, on blue.</summary>
                public static UIButtonPreset Info { get; } = new UIButtonPreset(Symbols.Info, WhiteOn(Colors.Sets.Blue));

                /// <summary>A warning: a triangle with an exclamation mark, on orange.</summary>
                public static UIButtonPreset Warning { get; } = new UIButtonPreset(Symbols.Warning, WhiteOn(Colors.Sets.Orange));

                /// <summary>An error: a no-entry sign, on red.</summary>
                public static UIButtonPreset Error { get; } = new UIButtonPreset(Symbols.Error, WhiteOn(Colors.Sets.Red));

                /// <summary>Help: a question mark, on blue.</summary>
                public static UIButtonPreset Help { get; } = new UIButtonPreset(Symbols.Help, WhiteOn(Colors.Sets.Blue));

                /// <summary>Starting something at once: a lightning bolt, on dark gray.</summary>
                public static UIButtonPreset Lightning { get; } = new UIButtonPreset(Symbols.Lightning, WhiteOn(Colors.Sets.Dark));

                /// <summary>Liking: a heart, on pink.</summary>
                public static UIButtonPreset Heart { get; } = new UIButtonPreset(Symbols.Heart, WhiteOn(Colors.Sets.Pink));

                /// <summary>Locked: a closed padlock, on dark gray.</summary>
                public static UIButtonPreset Lock { get; } = new UIButtonPreset(Symbols.Lock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Unlocked: an open padlock, on dark gray.</summary>
                public static UIButtonPreset Unlock { get; } = new UIButtonPreset(Symbols.Unlock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Signing out: an arrow leaving a frame, on dark gray.</summary>
                public static UIButtonPreset Logout { get; } = new UIButtonPreset(Symbols.Logout, WhiteOn(Colors.Sets.Dark));

                /// <summary>Switching off: the power sign, on red.</summary>
                public static UIButtonPreset Power { get; } = new UIButtonPreset(Symbols.Power, WhiteOn(Colors.Sets.Red));

                /// <summary>Mail: an envelope, on blue.</summary>
                public static UIButtonPreset Mail { get; } = new UIButtonPreset(Symbols.Mail, WhiteOn(Colors.Sets.Blue));

                /// <summary>Dates: a calendar, on blue.</summary>
                public static UIButtonPreset Calendar { get; } = new UIButtonPreset(Symbols.Calendar, WhiteOn(Colors.Sets.Blue));

                /// <summary>Time: an alarm clock, on dark gray.</summary>
                public static UIButtonPreset Clock { get; } = new UIButtonPreset(Symbols.Clock, WhiteOn(Colors.Sets.Dark));

                /// <summary>Notifications: a bell, on orange.</summary>
                public static UIButtonPreset Bell { get; } = new UIButtonPreset(Symbols.Bell, WhiteOn(Colors.Sets.Orange));

                /// <summary>A link: a chain, on blue.</summary>
                public static UIButtonPreset Link { get; } = new UIButtonPreset(Symbols.Link, WhiteOn(Colors.Sets.Blue));

                /// <summary>Pinning: a pin, on dark gray.</summary>
                public static UIButtonPreset Pin { get; } = new UIButtonPreset(Symbols.Pin, WhiteOn(Colors.Sets.Dark));

                /// <summary>The web: a globe, on blue.</summary>
                public static UIButtonPreset Globe { get; } = new UIButtonPreset(Symbols.Globe, WhiteOn(Colors.Sets.Blue));

                /// <summary>Showing or watching: an eye, on dark gray.</summary>
                public static UIButtonPreset Eye { get; } = new UIButtonPreset(Symbols.Eye, WhiteOn(Colors.Sets.Dark));

                /// <summary>The light theme: a sun, on orange.</summary>
                public static UIButtonPreset Sun { get; } = new UIButtonPreset(Symbols.Sun, WhiteOn(Colors.Sets.Orange));

                /// <summary>The dark theme: a moon, on dark gray.</summary>
                public static UIButtonPreset Moon { get; } = new UIButtonPreset(Symbols.Moon, WhiteOn(Colors.Sets.Dark));

                /// <summary>Minimizing a window: a bar at the bottom, on dark gray.</summary>
                public static UIButtonPreset Minimize { get; } = new UIButtonPreset(Symbols.Minimize, WhiteOn(Colors.Sets.Dark));

                /// <summary>Maximizing a window: a single frame, on dark gray.</summary>
                public static UIButtonPreset Maximize { get; } = new UIButtonPreset(Symbols.Maximize, WhiteOn(Colors.Sets.Dark));

                /// <summary>Bringing a maximized window back: two frames, on dark gray.</summary>
                public static UIButtonPreset Restore { get; } = new UIButtonPreset(Symbols.Restore, WhiteOn(Colors.Sets.Dark));
            }
        }
    }
}
