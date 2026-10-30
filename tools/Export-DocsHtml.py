"""Makes one readable HTML page of the important Markdown files (README.md, docs/*.md): docs/html/ErikwnkWFUI-docs.html, for a reader that has no Markdown preview.

    py -3 tools/Export-DocsHtml.py

At the left a directory of the files and their sections, in the middle the file, at the right the contents of the page that is shown. The first page is an
overview with charts, worked out from the table of docs/ANALYSIS.md (the progress of the units, the checks, the coverage of the tests). The page is
generated (docs/html/ is not under version control), needs no network, and follows the light or dark setting of the system. Links between the files and
the tables of contents work. Only what the files use of Markdown is understood: headings, paragraphs, lists (nested), tables, fenced code, block quotes,
rules, and bold / italic / code / links inline.
"""
import datetime
import html
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'docs', 'html')
FILES = ['README.md', 'docs/ANALYSIS.md', 'docs/CONTROLS.md', 'docs/ROUND-TWO.md', 'docs/MIGRATION.md', 'CHANGELOG.md']

HEADINGS = []   # (level, anchor, text) of the file that is being made
DOC = ''        # the id of the file that is being made: every anchor of the page starts with it, so that two files can have the same heading

STYLE = """
:root { color-scheme: light dark;
  --bg:#f4f6fa; --panel:#ffffff; --fg:#1b2330; --muted:#5d6b7e; --line:#dde3ec; --soft:#eef2f8; --link:#1d5fd1; --accent:#2f6bff;
  --good:#1f9d57; --warn:#d98a00; --bad:#d64545; --open:#c9d2df; --shadow:0 1px 2px rgba(20,30,50,.06), 0 4px 16px rgba(20,30,50,.05); }
@media (prefers-color-scheme: dark) { :root {
  --bg:#0e131b; --panel:#151c27; --fg:#e4eaf3; --muted:#93a1b5; --line:#263041; --soft:#1a2230; --link:#6ea2ff; --accent:#5b8dff;
  --good:#3ccf83; --warn:#f0b13a; --bad:#ff7070; --open:#33404f; --shadow:0 1px 2px rgba(0,0,0,.4); } }
* { box-sizing:border-box; }
html { scroll-behavior:smooth; }
body { background:var(--bg); color:var(--fg); font:15px/1.6 "Segoe UI Variable", "Segoe UI", system-ui, sans-serif; margin:0; }
.layout { display:grid; grid-template-columns:272px minmax(0,1fr) 250px; gap:0; max-width:1900px; margin:0 auto; align-items:start; }
aside.nav, aside.toc { position:sticky; top:0; height:100vh; overflow:auto; padding:20px 14px 40px; font-size:13.5px; }
aside.nav { border-right:1px solid var(--line); background:var(--panel); }
aside.toc { padding-left:18px; }
.brand { display:flex; align-items:center; gap:10px; font-weight:650; font-size:15px; margin:0 6px 16px; }
.brand i { width:28px; height:28px; border-radius:8px; background:linear-gradient(135deg,var(--accent),#7a5cff); display:inline-block; }
.brand small { display:block; font-weight:400; color:var(--muted); font-size:12px; }
aside details { margin:2px 0; }
aside summary { cursor:pointer; padding:6px 8px; border-radius:8px; font-weight:600; list-style:none; display:flex; align-items:center; gap:8px; }
aside summary::-webkit-details-marker { display:none; }
aside summary::before { content:"\\25B8"; color:var(--muted); font-size:11px; transition:transform .15s; }
aside details[open] > summary::before { transform:rotate(90deg); }
aside summary a { color:var(--fg); flex:1; }
aside summary:hover, aside .nav-list a:hover, aside.toc a:hover { background:var(--soft); }
aside .nav-list { list-style:none; margin:2px 0 8px 18px; padding:0 0 0 8px; border-left:1px solid var(--line); }
aside .nav-list a, aside.toc a { display:block; padding:3px 8px; color:var(--muted); border-radius:6px; }
aside a:hover { text-decoration:none; color:var(--fg); }
a.on, aside.toc a.on { color:var(--accent) !important; background:var(--soft); font-weight:600; }
aside.toc .head { font-size:11.5px; text-transform:uppercase; letter-spacing:.07em; color:var(--muted); margin:6px 8px 8px; }
aside.toc a.l3 { padding-left:20px; font-size:12.5px; }
main { padding:28px 36px 90px; min-width:0; }
.doc { display:none; } .doc.current { display:block; }
h1,h2,h3,h4 { line-height:1.25; margin:1.8em 0 .6em; scroll-margin-top:16px; }
h1 { font-size:2em; margin-top:.2em; letter-spacing:-.01em; }
h2 { font-size:1.4em; padding-bottom:.3em; border-bottom:1px solid var(--line); }
h3 { font-size:1.15em; } h4 { font-size:1.02em; color:var(--muted); }
a { color:var(--link); text-decoration:none; } a:hover { text-decoration:underline; }
p, li { max-width:110ch; }
code { font:12.5px/1.4 Consolas, "Cascadia Mono", monospace; background:var(--soft); border:1px solid var(--line); border-radius:5px; padding:1px 5px; }
pre { background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:14px 16px; overflow:auto; box-shadow:var(--shadow); }
pre code { background:none; border:0; padding:0; font-size:12.8px; }
.tablewrap { overflow-x:auto; margin:14px 0; border:1px solid var(--line); border-radius:10px; background:var(--panel); box-shadow:var(--shadow); }
table { border-collapse:collapse; width:100%; font-size:14px; }
th, td { padding:7px 12px; vertical-align:top; text-align:left; border-bottom:1px solid var(--line); }
th { background:var(--soft); font-weight:600; position:sticky; top:0; white-space:nowrap; }
tbody tr:last-child td { border-bottom:0; }
tbody tr:hover td { background:var(--soft); }
blockquote { margin:14px 0; padding:4px 16px; border-left:4px solid var(--accent); background:var(--panel); border-radius:0 8px 8px 0; color:var(--muted); }
hr { border:0; border-top:1px solid var(--line); margin:28px 0; }
ul, ol { padding-left:1.5em; } li { margin:.25em 0; }
.chk { display:inline-flex; width:20px; height:20px; border-radius:6px; border:1.5px solid var(--open); align-items:center; justify-content:center; font-size:13px; line-height:1; vertical-align:middle; }
.chk.on { background:var(--good); border-color:var(--good); color:#fff; }
.chk.na { border-style:dashed; color:var(--muted); }
/* the overview */
.cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(190px,1fr)); gap:14px; margin:18px 0 22px; }
.card { background:var(--panel); border:1px solid var(--line); border-radius:14px; padding:16px 18px; box-shadow:var(--shadow); }
.card .big { font-size:2.1em; font-weight:700; letter-spacing:-.02em; line-height:1.1; }
.card .lab { color:var(--muted); font-size:13px; margin-top:4px; }
.card .sub { font-size:12.5px; color:var(--muted); margin-top:8px; }
.panels { display:grid; grid-template-columns:repeat(auto-fit,minmax(380px,1fr)); gap:16px; margin-bottom:16px; }
.panel { background:var(--panel); border:1px solid var(--line); border-radius:14px; padding:18px 20px; box-shadow:var(--shadow); min-width:0; }
.panel h3 { margin:0 0 12px; font-size:1.02em; }
.panel.wide { grid-column:1 / -1; }
.donut { display:flex; align-items:center; gap:22px; flex-wrap:wrap; }
.legend { display:flex; flex-direction:column; gap:6px; font-size:13.5px; }
.legend span::before { content:""; display:inline-block; width:11px; height:11px; border-radius:3px; margin-right:8px; background:var(--c); vertical-align:-1px; }
.bars .row { display:grid; grid-template-columns:150px 1fr 56px; align-items:center; gap:12px; margin:9px 0; font-size:13.5px; }
.bars .track { display:flex; height:14px; border-radius:8px; overflow:hidden; background:var(--open); }
.bars .track i { display:block; height:100%; }
.bars .num { color:var(--muted); text-align:right; font-variant-numeric:tabular-nums; }
.units { display:flex; flex-wrap:wrap; gap:5px; margin-top:6px; }
.unit { width:46px; padding:5px 0; text-align:center; border-radius:8px; font-size:12px; font-weight:600; background:var(--open); color:var(--fg); cursor:default; }
.unit.done { background:var(--good); color:#fff; } .unit.part { background:var(--warn); color:#fff; }
svg text { fill:var(--muted); font:11px "Segoe UI", sans-serif; }
.chart { width:100%; height:auto; display:block; }
.note { color:var(--muted); font-size:12.5px; margin-top:8px; }
@media (max-width: 1350px) { .layout { grid-template-columns:260px minmax(0,1fr); } aside.toc { display:none; } }
@media (max-width: 800px) { .layout { display:block; } aside.nav { position:static; height:auto; max-height:45vh; } main { padding:18px 16px 60px; } .bars .row { grid-template-columns:100px 1fr 50px; } }
"""


def doc_id(rel):
    return os.path.splitext(os.path.basename(rel))[0]


def slug(text):
    text = re.sub(r'<[^>]+>', '', text).lower()
    text = re.sub(r'[^\w\- ]', '', text, flags=re.UNICODE)
    return text.strip().replace(' ', '-')


def target(href, source):
    """A link to a Markdown file of this set points at its place on the page."""
    if href.startswith('#'):
        return '#' + DOC + '--' + href[1:]
    if re.match(r'^[a-z]+:', href):
        return href
    path, _, anchor = href.partition('#')
    if path.lower().endswith('.md'):
        full = os.path.normpath(os.path.join(os.path.dirname(source), path))
        rel = os.path.relpath(full, ROOT).replace('\\', '/')
        if rel in FILES:
            return '#' + doc_id(rel) + ('--' + anchor if anchor else '')
        return 'file:///' + full.replace('\\', '/') + ('#' + anchor if anchor else '')
    return href


def inline(text, source):
    parts = re.split(r'(`+)(.+?)\1', text)
    out = []
    # re.split with two groups gives: text, ticks, code, text, ...
    for i in range(0, len(parts), 3):
        out.append(inline_plain(parts[i], source))
        if i + 2 < len(parts):
            out.append('<code>' + html.escape(parts[i + 2]) + '</code>')
    return ''.join(out)


def inline_plain(text, source):
    text = html.escape(text, quote=False)
    text = re.sub(r'&lt;(br\s*/?)&gt;', '<br>', text)
    text = re.sub(r'\[([^\]]+)\]\(([^)\s]+)\)', lambda m: '<a href="%s">%s</a>' % (html.escape(target(html.unescape(m.group(2)), source)), m.group(1)), text)
    text = re.sub(r'\*\*(.+?)\*\*', r'<strong>\1</strong>', text)
    text = re.sub(r'(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])', r'<em>\1</em>', text)
    return text


def cell_html(cell, source):
    """A table cell; a tick box is drawn as one."""
    if cell == '[x]':
        return '<span class="chk on" title="done">✓</span>'
    if cell == '[ ]':
        return '<span class="chk" title="open"></span>'
    if cell == '-':
        return '<span class="chk na" title="does not apply">–</span>'
    done = re.match(r'^\[x\]\s+(.*)$', cell)
    if done:
        return '<span class="chk on" title="done">✓</span> ' + inline(done.group(1), source)
    return inline(cell, source)


def cells(line):
    line = line.strip()
    if line.startswith('|'):
        line = line[1:]
    if line.endswith('|') and not line.endswith('\\|'):
        line = line[:-1]
    return [c.strip().replace('\\|', '|') for c in re.split(r'(?<!\\)\|', line)]


def is_rule(line):
    return bool(re.match(r'^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$', line))


LIST_ITEM = re.compile(r'^(\s*)([-*+]|\d+[.)])\s+(.*)$')


def render(lines, source):
    out = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        stripped = line.strip()
        if not stripped:
            i += 1
            continue
        if stripped.startswith('<!--'):
            while i < n and '-->' not in lines[i]:
                i += 1
            i += 1
            continue
        fence = re.match(r'^(\s*)(```+|~~~+)\s*([\w+#-]*)', line)
        if fence:
            marker = fence.group(2)
            body = []
            i += 1
            while i < n and not lines[i].strip().startswith(marker):
                body.append(lines[i])
                i += 1
            i += 1
            out.append('<pre><code>' + html.escape('\n'.join(body)) + '</code></pre>')
            continue
        heading = re.match(r'^(#{1,6})\s+(.*?)\s*#*\s*$', line)
        if heading:
            level = len(heading.group(1))
            text = inline(heading.group(2), source)
            out.append('<h%d id="%s--%s">%s</h%d>' % (level, DOC, slug(heading.group(2)), text, level))
            HEADINGS.append((level, DOC + '--' + slug(heading.group(2)), re.sub(r'<[^>]+>', '', text)))
            i += 1
            continue
        if re.match(r'^\s*([-*_])(\s*\1){2,}\s*$', line):
            out.append('<hr>')
            i += 1
            continue
        if '|' in line and i + 1 < n and is_rule(lines[i + 1]) and '|' in lines[i + 1]:
            head = cells(line)
            i += 2
            rows = []
            while i < n and lines[i].strip() and '|' in lines[i]:
                rows.append(cells(lines[i]))
                i += 1
            html_table = ['<div class="tablewrap"><table><thead><tr>' + ''.join('<th>%s</th>' % inline(c, source) for c in head) + '</tr></thead><tbody>']
            for row in rows:
                row = (row + [''] * len(head))[:max(len(head), len(row))]
                html_table.append('<tr>' + ''.join('<td>%s</td>' % cell_html(c, source) for c in row) + '</tr>')
            html_table.append('</tbody></table></div>')
            out.append(''.join(html_table))
            continue
        if stripped.startswith('>'):
            body = []
            while i < n and lines[i].strip().startswith('>'):
                body.append(re.sub(r'^\s*>\s?', '', lines[i]))
                i += 1
            out.append('<blockquote>' + render(body, source) + '</blockquote>')
            continue
        item = LIST_ITEM.match(line)
        if item:
            block = []
            while i < n:
                if not lines[i].strip():
                    # a blank line continues the list only when an indented line or another item follows
                    j = i
                    while j < n and not lines[j].strip():
                        j += 1
                    if j < n and (LIST_ITEM.match(lines[j]) or lines[j].startswith('  ')) and j - i <= 1:
                        block.append('')
                        i = j
                        continue
                    break
                if not (LIST_ITEM.match(lines[i]) or lines[i].startswith(' ')):
                    break
                block.append(lines[i])
                i += 1
            out.append(render_list(block, source))
            continue
        para = []
        while i < n and lines[i].strip() and not re.match(r'^(#{1,6}\s|```|~~~|>\s?|\s*([-*+]|\d+[.)])\s)', lines[i]) \
                and not ('|' in lines[i] and i + 1 < n and is_rule(lines[i + 1])):
            para.append(lines[i].strip())
            i += 1
        if not para:
            para.append(lines[i].strip())
            i += 1
        out.append('<p>' + inline(' '.join(para), source) + '</p>')
    return '\n'.join(out)


def render_list(block, source):
    first = LIST_ITEM.match(block[0])
    ordered = first.group(2)[0].isdigit()
    base = len(first.group(1))
    items = []
    current = None
    for line in block:
        m = LIST_ITEM.match(line)
        if m and len(m.group(1)) <= base:
            current = [m.group(3)]
            items.append(current)
        elif current is not None:
            current.append(line)
    tag = 'ol' if ordered else 'ul'
    parts = ['<%s>' % tag]
    for item in items:
        text_lines = []
        rest = []
        nested = False
        for line in item[1:]:
            if nested or LIST_ITEM.match(line):
                nested = True
                rest.append(line)
            elif line.strip():
                text_lines.append(line.strip())
            else:
                rest.append(line)
        text = inline(' '.join([item[0]] + text_lines), source)
        inner = ''
        if rest and any(r.strip() for r in rest):
            indent = min(len(r) - len(r.lstrip()) for r in rest if r.strip())
            inner = render([r[indent:] if r.strip() else '' for r in rest], source)
        parts.append('<li>%s%s</li>' % (text, ('\n' + inner) if inner else ''))
    parts.append('</%s>' % tag)
    return '\n'.join(parts)


# ---- the overview: worked out from the tables of ANALYSIS.md ----

PHASE_NAMES = {0: 'Tools and starting point', 7: 'Across the project', 8: 'Before the push', 1: 'Foundations', 2: 'Simple controls', 3: 'Input', 4: 'Containers', 5: 'Lists and tables', 6: 'Windows'}
CHECKS = ['Bugs', 'Refactor', 'Docs', 'Showcase', 'Tests', 'Table', 'Commit']


def read_units():
    """The rows of the table of units: number, name, coverage, the seven ticks (True, False, None where a check does not apply)."""
    path = os.path.join(ROOT, 'docs', 'ANALYSIS.md')
    if not os.path.exists(path):
        return [], []
    units, steps = [], []
    with open(path, encoding='utf-8-sig') as stream:
        for line in stream:
            if not re.match(r'^\|\s*\d+\.\d+\s*\|', line):
                continue
            c = cells(line)
            number = c[0]
            if len(c) >= 12:
                # c[3] is the coverage before the work, c[4] the one after it (the last run: tools/Update-UnitCoverage.py).
                coverage = re.match(r'^([\d.,]+)\s*%', c[4])
                ticks = [True if t == '[x]' else None if t == '-' else False for t in c[5:12]]
                name = re.sub(r'`', '', c[2])
                units.append({'n': number, 'phase': int(number.split('.')[0]), 'name': name,
                              'cov': float(coverage.group(1).replace(',', '.')) if coverage else None, 'ticks': ticks})
            elif len(c) >= 3:
                steps.append({'n': number, 'phase': int(number.split('.')[0]), 'done': c[-1].startswith('[x]')})
    return units, steps


def unit_state(unit):
    if unit['ticks'][6]:
        return 'done'
    if any(t for t in unit['ticks'][:6]):
        return 'part'
    return 'open'


def donut(counts, total):
    radius, circumference = 52, 2 * 3.14159265 * 52
    colors = [('done', 'var(--good)'), ('part', 'var(--warn)'), ('open', 'var(--open)')]
    segments = []
    offset = 0.0
    for key, color in colors:
        length = circumference * counts[key] / total if total else 0
        segments.append('<circle cx="70" cy="70" r="%d" fill="none" stroke="%s" stroke-width="16" stroke-dasharray="%.2f %.2f" '
                        'stroke-dashoffset="%.2f" transform="rotate(-90 70 70)"/>' % (radius, color, length, circumference - length, -offset))
        offset += length
    percent = round(100.0 * counts['done'] / total) if total else 0
    return ('<svg viewBox="0 0 140 140" width="150" height="150" role="img" aria-label="units done">%s'
            '<text x="70" y="68" text-anchor="middle" style="font:700 26px Segoe UI;fill:var(--fg)">%d%%</text>'
            '<text x="70" y="88" text-anchor="middle">of the units</text></svg>' % (''.join(segments), percent))


def coverage_chart(units):
    rows = [u for u in units if u['cov'] is not None]
    if not rows:
        return ''
    width, height, left, bottom, top = 1100, 250, 34, 34, 12
    slot = (width - left - 8) / float(len(rows))
    plot = height - bottom - top

    def y(value):
        return top + plot * (1 - value / 100.0)

    parts = ['<svg class="chart" viewBox="0 0 %d %d" role="img" aria-label="coverage of the tests per unit">' % (width, height)]
    for tick in (0, 25, 50, 75, 100):
        parts.append('<line x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke="var(--line)"/><text x="%d" y="%.1f" text-anchor="end">%d</text>' % (
            left, width, y(tick), y(tick), left - 6, y(tick) + 4, tick))
    for index, unit in enumerate(rows):
        x = left + index * slot + slot * 0.18
        w = slot * 0.64
        color = 'var(--good)' if unit['cov'] >= 85 else 'var(--bad)'
        state = unit_state(unit)
        parts.append('<g><title>%s %s: %.1f %%</title><rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" rx="3" fill="%s" opacity="%s"/></g>' % (
            unit['n'], html.escape(unit['name']), unit['cov'], x, y(unit['cov']), w, y(0) - y(unit['cov']), color, '1' if state == 'done' else '.45'))
        parts.append('<text x="%.1f" y="%d" text-anchor="middle" transform="rotate(-60 %.1f %d)" style="font-size:10px">%s</text>' % (
            x + w / 2, height - bottom + 14, x + w / 2, height - bottom + 14, unit['n']))
    parts.append('<line x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke="var(--accent)" stroke-dasharray="5 4" stroke-width="1.5"/>'
                 '<text x="%d" y="%.1f" text-anchor="end" style="fill:var(--accent)">target 85 %%</text>' % (left, width, y(85), y(85), width - 4, y(85) - 5))
    parts.append('</svg>')
    return ''.join(parts)


def overview():
    units, steps = read_units()
    if not units:
        return '<h1 id="OVERVIEW--overview">Overview</h1><p>docs/ANALYSIS.md has no table of units.</p>'
    counts = {'done': 0, 'part': 0, 'open': 0}
    for u in units:
        counts[unit_state(u)] += 1
    total = len(units)
    covs = [u['cov'] for u in units if u['cov'] is not None]
    finished = [u['cov'] for u in units if u['cov'] is not None and unit_state(u) == 'done']
    below = [u for u in units if u['cov'] is not None and u['cov'] < 85]
    steps_done = sum(1 for s in steps if s['done'])
    nxt = next((u for u in units if unit_state(u) != 'done'), None)

    out = ['<h1 id="OVERVIEW--overview">Overview</h1>',
           '<p class="note" style="margin-top:-6px">Made %s from the files; worked out from the table of <a href="#ANALYSIS">ANALYSIS.md</a>. '
           'The page loads itself again every 20 seconds while it is shown.</p>' % datetime.datetime.now().strftime('%d.%m.%Y %H:%M')]
    cards = [
        ('%d / %d' % (counts['done'], total), 'units finished', 'every check ticked and committed'),
        ('%.1f %%' % (sum(finished) / len(finished)) if finished else '-', 'coverage of the finished units', 'target 85 % of the lines per unit'),
        ('%d / %d' % (sum(1 for u in units for t in u['ticks'] if t), sum(1 for u in units for t in u['ticks'] if t is not None)), 'checks ticked', 'bugs, refactor, docs, showcase, tests, table, commit'),
        ('%d / %d' % (steps_done, len(steps)), 'steps of phases 0, 7 and 8', 'tools, across the project, before the push'),
    ]
    out.append('<div class="cards">' + ''.join('<div class="card"><div class="big">%s</div><div class="lab">%s</div><div class="sub">%s</div></div>' % c for c in cards) + '</div>')
    if nxt:
        out.append('<div class="card" style="margin-bottom:16px"><div class="lab">Next unit</div><div class="big" style="font-size:1.4em">%s &nbsp;%s</div></div>' % (
            nxt['n'], html.escape(nxt['name'])))

    out.append('<div class="panels">')
    out.append('<div class="panel"><h3>Units</h3><div class="donut">%s<div class="legend">'
               '<span style="--c:var(--good)">%d finished</span><span style="--c:var(--warn)">%d under way</span><span style="--c:var(--open)">%d not begun</span></div></div></div>' % (
                   donut(counts, total), counts['done'], counts['part'], counts['open']))
    bars = []
    for phase in sorted(set(u['phase'] for u in units) | set(s['phase'] for s in steps)):
        # Phases 1 to 6 are units with seven checks, phases 0, 7 and 8 are single steps: a step is finished when it is ticked.
        group = [u for u in units if u['phase'] == phase] or [s for s in steps if s['phase'] == phase]
        n = float(len(group))
        done = sum(1 for g in group if (unit_state(g) == 'done' if 'ticks' in g else g['done']))
        part = sum(1 for g in group if 'ticks' in g and unit_state(g) == 'part')
        bars.append('<div class="row"><span>%d &middot; %s</span><div class="track"><i style="width:%.1f%%;background:var(--good)"></i>'
                    '<i style="width:%.1f%%;background:var(--warn)"></i></div><span class="num">%d / %d</span></div>' % (
                        phase, PHASE_NAMES.get(phase, ''), 100 * done / n, 100 * part / n, done, len(group)))
    out.append('<div class="panel bars"><h3>Phases</h3>%s</div>' % ''.join(bars))

    checks = []
    for index, name in enumerate(CHECKS):
        applicable = [u for u in units if u['ticks'][index] is not None]
        ticked = sum(1 for u in applicable if u['ticks'][index])
        n = float(len(applicable)) or 1.0
        checks.append('<div class="row"><span>%s</span><div class="track"><i style="width:%.1f%%;background:var(--accent)"></i></div><span class="num">%d / %d</span></div>' % (
            name, 100 * ticked / n, ticked, len(applicable)))
    out.append('<div class="panel bars"><h3>The checks</h3>%s</div>' % ''.join(checks))

    tiles = ''.join('<span class="unit %s" title="%s %s">%s</span>' % (unit_state(u), u['n'], html.escape(u['name']), u['n']) for u in units)
    tiles += ''.join('<span class="unit %s" title="step %s">%s</span>' % ('done' if s['done'] else 'open', s['n'], s['n']) for s in steps)
    out.append('<div class="panel"><h3>Every unit and step</h3><div class="units">%s</div><p class="note">Green: finished. Amber: begun. Grey: not begun. Point at a unit for its name.</p></div>' % tiles)

    out.append('<div class="panel wide"><h3>Coverage of the tests per unit (lines, in %%)</h3>%s'
               '<p class="note">The coverage of each unit after the work (the last full run). Faint bars: the unit is not finished.%s</p></div>' % (
                   coverage_chart(units),
                   ' Under the target: %s.' % ', '.join('%s (%.1f %%)' % (u['n'], u['cov']) for u in below) if below else ''))
    out.append('</div>')
    out.append('<h2 id="OVERVIEW--the-files">The files</h2><div class="cards">'
               '<a class="card" href="#README"><div class="lab">README.md</div><div class="sub">What the library is, how to use it.</div></a>'
               '<a class="card" href="#ANALYSIS"><div class="lab">ANALYSIS.md</div><div class="sub">The progress, unit by unit.</div></a>'
               '<a class="card" href="#CONTROLS"><div class="lab">CONTROLS.md</div><div class="sub">Every control and its settings.</div></a>'
               '<a class="card" href="#ROUND-TWO"><div class="lab">ROUND-TWO.md</div><div class="sub">The process and the rules of the analysis.</div></a></div>')
    return '\n'.join(out)


def build(rel):
    global DOC
    DOC = doc_id(rel)
    del HEADINGS[:]
    path = os.path.join(ROOT, rel)
    with open(path, encoding='utf-8-sig') as stream:
        lines = stream.read().replace('\r\n', '\n').split('\n')
    title = next((re.sub(r'^#+\s*', '', l) for l in lines if l.startswith('# ')), rel)
    body = render(lines, path)
    sections = [(a, h, level) for level, a, h in HEADINGS if level in (2, 3)]
    return DOC, rel, title, body, sections


SCRIPT = """
var REFRESH_SECONDS = 20;
(function () {
  var docs = document.querySelectorAll('.doc');
  var toc = document.querySelector('aside.toc .list');
  var headings = [];
  function show(id) {
    var target = id ? document.getElementById(id) : null;
    var doc = target ? target.closest('.doc') : null;
    if (!doc) doc = docs[0];
    docs.forEach(function (d) { d.classList.toggle('current', d === doc); });
    document.querySelectorAll('aside.nav details').forEach(function (d) { d.open = d.getAttribute('data-doc') === doc.id; });
    buildToc(doc);
    if (target && target !== doc) target.scrollIntoView({ behavior: 'auto' }); else window.scrollTo(0, 0);
    spy();
  }
  // The contents of the page that is shown: its sections, at the right.
  function buildToc(doc) {
    toc.innerHTML = '';
    headings = [];
    doc.querySelectorAll('h2, h3').forEach(function (h) {
      var a = document.createElement('a');
      a.href = '#' + h.id; a.textContent = h.textContent; a.className = h.tagName === 'H3' ? 'l3' : '';
      toc.appendChild(a);
      headings.push({ h: h, a: a });
    });
  }
  function spy() {
    var current = null;
    headings.forEach(function (x) { if (x.h.getBoundingClientRect().top < 120) current = x; });
    headings.forEach(function (x) { x.a.classList.toggle('on', x === current); });
    document.querySelectorAll('aside.nav a.on').forEach(function (a) { a.classList.remove('on'); });
    var doc = document.querySelector('.doc.current');
    document.querySelectorAll('aside.nav details[data-doc="' + doc.id + '"] .nav-list a').forEach(function (a) {
      a.classList.toggle('on', current && a.getAttribute('href') === '#' + current.h.id);
    });
  }
  function fromHash() { return decodeURIComponent(location.hash.slice(1)); }
  // A click on a link of the page is handled here (the address is set as well, so that the back button works).
  document.addEventListener('click', function (e) {
    var a = e.target.closest ? e.target.closest('a[href^="#"]') : null;
    if (!a) return;
    e.preventDefault();
    var id = decodeURIComponent(a.getAttribute('href').slice(1));
    try { history.pushState(null, '', '#' + id); } catch (x) { }
    show(id);
  });
  window.addEventListener('popstate', function () { show(fromHash()); });
  window.addEventListener('hashchange', function () { show(fromHash()); });
  window.addEventListener('scroll', spy, { passive: true });
  // The page is a file: it cannot watch itself, so it loads itself again now and then (while it is shown) and stays where it was.
  var KEY = 'erikwnkwfui-docs-scroll';
  try {
    var saved = JSON.parse(sessionStorage.getItem(KEY) || 'null');
    sessionStorage.removeItem(KEY);
    show(fromHash());
    if (saved && saved.hash === location.hash) window.scrollTo(0, saved.y);
  } catch (x) { show(fromHash()); }
  setInterval(function () {
    if (document.hidden) return;
    try { sessionStorage.setItem(KEY, JSON.stringify({ hash: location.hash, y: window.scrollY })); } catch (x) { }
    location.reload();
  }, REFRESH_SECONDS * 1000);
})();
"""


def main():
    built = [build(f) for f in FILES if os.path.exists(os.path.join(ROOT, f))]
    DOCS_FIRST = ('OVERVIEW', 'Overview', overview(), [('OVERVIEW--the-files', 'The files', 2)])
    nav = ['<div class="brand"><i></i><div>ErikwnkWFUI<small>documents</small></div></div>']
    nav.append('<details data-doc="OVERVIEW"><summary><a href="#OVERVIEW">Overview</a></summary></details>')
    for doc, rel, title, body, sections in built:
        items = ''.join('<li><a href="#%s">%s</a></li>' % (a, html.escape(h)) for a, h, level in sections if level == 2)
        nav.append('<details data-doc="%s"><summary><a href="#%s">%s</a></summary>%s</details>' % (
            doc, doc, html.escape(os.path.basename(rel)), '<ul class="nav-list">' + items + '</ul>' if items else ''))
    docs = ['<section class="doc" id="OVERVIEW">\n%s\n</section>' % DOCS_FIRST[2]]
    docs += ['<section class="doc" id="%s">\n%s\n</section>' % (doc, body) for doc, rel, title, body, sections in built]
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith('.html'):
            os.remove(os.path.join(OUT, old))
    page = ('<!doctype html>\n<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
            '<title>ErikwnkWFUI - documents</title><style>%s</style></head><body><div class="layout"><aside class="nav">%s</aside>'
            '<main>%s</main><aside class="toc"><div class="head">On this page</div><div class="list"></div></aside></div><script>%s</script></body></html>\n') % (
                STYLE.replace('\\\\', '\\'), ''.join(nav), '\n'.join(docs), SCRIPT)
    with open(os.path.join(OUT, 'ErikwnkWFUI-docs.html'), 'w', encoding='utf-8', newline='\n') as stream:
        stream.write(page)
    print('docs/html/ErikwnkWFUI-docs.html (Overview, %s)' % ', '.join(os.path.basename(rel) for doc, rel, title, body, sections in built))


if __name__ == '__main__':
    sys.exit(main())
