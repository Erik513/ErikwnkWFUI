using System;
using System.Collections.Generic;
using System.Text;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// BuildHtmlTable/BuildCfHtmlTable - the "HTML Format" clipboard payload
/// CopySelection/CopySelectionAsTable puts alongside the plain-text one (see
/// ListViewCopyTests), invoked directly via reflection since both are plain
/// static string-building with no Clipboard/UI involved. BuildCfHtmlTable's
/// own byte-offset math is the part most worth locking down here - it's
/// computed in UTF-8 bytes (not .NET char counts) specifically to survive
/// non-ASCII content, which is exactly what a formula-only test wouldn't
/// catch if that byte/char distinction ever regressed.
/// </summary>
public class ListViewHtmlTableTests
{
    private static string BuildHtmlTable(IReadOnlyList<string>? headerCells, IReadOnlyList<List<string>> rows)
    {
        using WfuiListView listView = new WfuiListView();
        return listView.InvokePrivate<string>("BuildHtmlTable", headerCells, rows)!;
    }

    private static string BuildCfHtmlTable(IReadOnlyList<string>? headerCells, IReadOnlyList<List<string>> rows)
    {
        using WfuiListView listView = new WfuiListView();
        return listView.InvokePrivate<string>("BuildCfHtmlTable", headerCells, rows)!;
    }

    [Fact]
    public void BuildHtmlTable_NoHeader_ProducesOnlyDataRows()
    {
        var rows = new List<List<string>> { new List<string> { "A", "B" } };

        string table = BuildHtmlTable(null, rows);

        Assert.DoesNotContain("<th", table);
        Assert.Contains("<td style=\"border:1px solid #999;padding:4px 8px;\">A</td>", table);
        Assert.Contains("<td style=\"border:1px solid #999;padding:4px 8px;\">B</td>", table);
    }

    [Fact]
    public void BuildHtmlTable_WithHeader_IncludesAHeaderRow()
    {
        var headerCells = new List<string> { "Name", "Status" };
        var rows = new List<List<string>>();

        string table = BuildHtmlTable(headerCells, rows);

        Assert.Contains("<th style=\"border:1px solid #999;padding:4px 8px;background:#eee;text-align:left;\">Name</th>", table);
        Assert.Contains("<th style=\"border:1px solid #999;padding:4px 8px;background:#eee;text-align:left;\">Status</th>", table);
    }

    [Fact]
    public void BuildHtmlTable_HtmlEncodesCellContent()
    {
        var rows = new List<List<string>> { new List<string> { "<b>&Co</b>" } };

        string table = BuildHtmlTable(null, rows);

        Assert.DoesNotContain("<b>&Co</b>", table);
        Assert.Contains("&lt;b&gt;&amp;Co&lt;/b&gt;", table);
    }

    [Fact]
    public void BuildCfHtmlTable_FragmentOffsets_PointExactlyAtTheTableHtml()
    {
        var headerCells = new List<string> { "Name" };
        var rows = new List<List<string>> { new List<string> { "Alice" } };

        string cfHtml = BuildCfHtmlTable(headerCells, rows);
        string expectedTable = BuildHtmlTable(headerCells, rows);

        string fragment = ExtractByOffsets(cfHtml, "StartFragment", "EndFragment");

        Assert.Equal(expectedTable, fragment);
    }

    [Fact]
    public void BuildCfHtmlTable_HtmlOffsets_PointAtAWellFormedDocument()
    {
        var rows = new List<List<string>> { new List<string> { "A" } };

        string cfHtml = BuildCfHtmlTable(null, rows);

        string html = ExtractByOffsets(cfHtml, "StartHTML", "EndHTML");

        Assert.StartsWith("<html>", html, StringComparison.Ordinal);
        Assert.EndsWith("</html>", html, StringComparison.Ordinal);
    }

    // The whole reason the offsets are computed in UTF-8 bytes instead of
    // .NET char counts (see BuildCfHtmlTable's own comment) - a summary or
    // title with German umlauts is exactly the case a char-count-based
    // offset would get wrong, since each of these encodes to 2 UTF-8 bytes
    // but is still only 1 .NET char.
    [Fact]
    public void BuildCfHtmlTable_NonAsciiContent_OffsetsStillPointAtTheTableHtml()
    {
        var headerCells = new List<string> { "Größe" };
        var rows = new List<List<string>> { new List<string> { "Übersicht äöüß" } };

        string cfHtml = BuildCfHtmlTable(headerCells, rows);
        string expectedTable = BuildHtmlTable(headerCells, rows);

        string fragment = ExtractByOffsets(cfHtml, "StartFragment", "EndFragment");

        Assert.Equal(expectedTable, fragment);
    }

    // Parses "Label:0000000123" out of the CF_HTML header and slices the
    // UTF-8 byte form of the whole string at those two byte offsets -
    // mirrors exactly what a real clipboard consumer (Word, browsers, ...)
    // does with this envelope.
    private static string ExtractByOffsets(string cfHtml, string startLabel, string endLabel)
    {
        int start = ParseOffset(cfHtml, startLabel);
        int end = ParseOffset(cfHtml, endLabel);

        byte[] utf8Bytes = Encoding.UTF8.GetBytes(cfHtml);
        return Encoding.UTF8.GetString(utf8Bytes, start, end - start);
    }

    private static int ParseOffset(string cfHtml, string label)
    {
        string marker = label + ":";
        int index = cfHtml.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        return int.Parse(cfHtml.Substring(index, 10));
    }
}
