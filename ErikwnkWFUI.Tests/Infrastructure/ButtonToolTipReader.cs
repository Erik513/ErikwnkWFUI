using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The tooltip a button got from the factory, read back from the factory's own
/// table (a <c>ToolTip</c> is not reachable from the button itself).
/// </summary>
internal static class ButtonToolTipReader
{
    public static ToolTip? Find(Button button)
    {
        FieldInfo field = typeof(UIButtonFactory).GetField("ToolTipsByButton", BindingFlags.NonPublic | BindingFlags.Static)!;
        object table = field.GetValue(null)!;
        object?[] args = { button, null };
        bool found = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, args)!;

        return found ? (ToolTip)args[1]! : null;
    }

    public static string Text(Button button)
    {
        return Find(button)?.GetToolTip(button) ?? "";
    }
}
