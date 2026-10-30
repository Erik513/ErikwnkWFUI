r"""Writes the column "Cov. after" of the table of units in docs/ANALYSIS.md from the last run of tools\Run-Coverage.ps1.

A unit is the sum of the lines of the files of its controls and its factory; a group is the first part of a file name (ListBox.cs, ListBox.Drag.cs
-> ListBox), as in coverage\summary.md. The groups of each unit are listed in UNITS below: a new control or file needs a line there.

    py -3 -I tools\Update-UnitCoverage.py
"""
import collections
import glob
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIBRARY = '/ErikwnkWFUI/ErikwnkWFUI/'

UNITS = {
    '1.1': 'UIColors UIThemes UIColorSet UIAccentColors UIFonts UIColorTheme',
    '1.2': 'ThemeWatcher ThemeColor ColorSetHolder ColorSetSource SurfaceColor InputField FocusReleaseWatcher OutsideClickFilter',
    '1.3': 'AnimationClock AnimationSettings ClipboardAccess Dpi EntryAssemblyName EntryAssemblyVersion FrameTicker LayeredWindow ParentBackground RoundedRectangle WindowPlacementStore WindowsAutoStart',
    '1.4': 'NativeControlTheme',
    '1.5': 'UIStrings AppCurrency',
    '1.6': 'UIStyles',
    '2.1': 'UIButtonFactory ButtonLook ButtonLookBehavior CaptionButton SymbolButton UIButtonPreset',
    '2.2': 'ScrollingLabel LabelScroll LabelScrollMode LabelScrollPause LabelScrollSpeed UILabelFactory',
    '2.3': 'CheckBox BoxLabel CheckGlyph BoxLook BoxState UICheckBoxFactory',
    '2.4': 'RadioButton RadioGroup UIRadioButtonFactory',
    '2.5': 'GroupBox UIGroupBoxFactory',
    '2.6': 'ToggleSwitch UIToggleSwitchFactory',
    '2.7': 'ProgressBar ProgressBarPainting ProgressTextScroller UIProgressBarFactory',
    '2.8': 'SlimProgressBar UISlimProgressBarFactory MarqueeAnimator',
    '2.9': 'Spinner UISpinnerFactory',
    '3.1': 'TextField LockableTextBox PasswordTextBox UITextBoxFactory TextDragDropHelper',
    '3.2': 'ComboBox UIComboBoxFactory',
    '3.3': 'NumericUpDown UINumericUpDownFactory',
    '3.4': 'TrackBar ValuePopup UITrackBarFactory',
    '3.5': 'VolumeControl UIVolumeControlFactory',
    '4.1': 'Panel CardPanel PanelFrame UIPanelFactory',
    '4.2': 'FlowLayoutPanel UIFlowLayoutPanelFactory',
    '4.3': 'TableLayoutPanel UITableLayoutPanelFactory',
    '4.4': 'MultiPanel UIMultiPanelFactory',
    '4.5': 'VersionPanel UIVersionPanelFactory',
    '4.6': 'TabControl ReadOnlyTabControl TabEventArgs TabScrollButtons UITabControlFactory',
    '4.7': 'ContextMenuStrip UIContextMenuStripFactory',
    '4.8': 'FullTextToolTip ToolTipActivationFix ToolTipWidth UIToolTipFactory',
    '4.9': 'TitleBarControl',
    '5.1': 'ListBox TitledListBox UIListBoxFactory UITitledListBoxFactory ItemOrderMath',
    '5.2': 'ListView UIListViewFactory',
    '5.3': 'DataGridView ReadOnlyDataGridView UIDataGridViewFactory ColumnDragState ColumnFeatureSwitch ColumnHeaderPainting ColumnLayoutMath CopyConfirmationDisplay SelectionOutsideClick SortComparerRegistry SortCycle',
    '5.4': 'PropertyTable PropertyRow PropertyColumn UIColumn UIPropertyTableFactory',
    '6.1': 'AppForm FormOptions FormSize FormDragHandle BorderlessResizeHandler UIFormFactory ObsoleteNames',
    '6.2': 'MessageBox MessageBoxForm',
    '6.3': 'InputBoxForm',
    '6.4': 'ProgressForm ProgressBox',
    '6.5': 'OptionsForm UIAppearance',
    '6.6': 'ToastForm Toaster UIToastFactory',
    '6.7': 'InfoPopupForm InfoPopupSection PopupFormBase PopupSupport UIInfoPopupFactory PopupDelay PopupDuration PopupTransparency',
    '6.8': 'UpdatePrompt UpdateAvailableForm AppUpdater',
}


def groups():
    found = sorted(glob.glob(os.path.join(ROOT, 'coverage', 'raw', '**', '*.cobertura.xml'), recursive=True), key=os.path.getmtime)
    if not found:
        sys.exit('No coverage result: run tools\\Run-Coverage.ps1 first.')

    files = {}
    for cls in ET.parse(found[-1]).iter('class'):
        name = cls.get('filename').replace('\\', '/')
        lines = files.setdefault(name, {})
        for line in cls.iter('line'):
            number = int(line.get('number'))
            lines[number] = max(lines.get(number, 0), int(line.get('hits')))

    result = collections.defaultdict(lambda: [0, 0])
    for name, lines in files.items():
        if LIBRARY not in name:
            continue
        group = name.split('/')[-1].split('.')[0]
        result[group][0] += len(lines)
        result[group][1] += sum(1 for hits in lines.values() if hits > 0)

    return result


def main():
    covered = groups()
    values = {}
    for unit, names in UNITS.items():
        total = hit = 0
        for name in names.split():
            if name not in covered:
                print('warning: unit %s: no lines for %s' % (unit, name))
                continue
            total += covered[name][0]
            hit += covered[name][1]
        values[unit] = '%.1f %%' % (100.0 * hit / total) if total else '-'

    path = os.path.join(ROOT, 'docs', 'ANALYSIS.md')
    with open(path, 'rb') as stream:
        raw = stream.read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    text = raw[3:].decode('utf-8') if bom else raw.decode('utf-8')
    crlf = '\r\n' in text
    lines = text.replace('\r\n', '\n').split('\n')

    out = []
    for line in lines:
        cells = line.split('|')
        if line.startswith('| # | Phase | Unit |') and 'Cov. after' not in line:
            line = line.replace('| Cov. before |', '| Cov. before | Cov. after |')
        elif line.startswith('|---|---|---|') and line.count('|') == 12:
            line = '|---|---|---|---:|---:|---|---|---|---|---|---|---|'
        elif re.match(r'^\| \d+\.\d+ \| \d', line):
            number = cells[1].strip()
            if len(cells) == 13:
                cells.insert(5, ' ' + values.get(number, '-') + ' ')
            elif len(cells) == 14:
                cells[5] = ' ' + values.get(number, '-') + ' '
            line = '|'.join(cells)
        out.append(line)

    result = '\n'.join(out)
    if crlf:
        result = result.replace('\n', '\r\n')
    with open(path, 'wb') as stream:
        stream.write((b'\xef\xbb\xbf' if bom else b'') + result.encode('utf-8'))

    for unit in sorted(values, key=lambda u: tuple(int(p) for p in u.split('.'))):
        print(unit, values[unit])


main()
