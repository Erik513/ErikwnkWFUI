namespace ErikwnkWFUI.Tests;

/// <summary>Plain bindable row shape used by the DataGridView tests - two auto-generated columns, Name and Value.</summary>
public sealed class TestItem
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    public TestItem()
    {
    }

    public TestItem(string name, int value)
    {
        Name = name;
        Value = value;
    }
}
