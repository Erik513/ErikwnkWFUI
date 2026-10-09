using System.Drawing;
using System.Runtime.CompilerServices;
using ErikwnkWFUI.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The tests put the mouse far away from any toast, so a test machine's real mouse cannot hold
/// a toast open (a toast stays while the mouse is on it).
/// </summary>
internal static class ToastTestDefaults
{
    [ModuleInitializer]
    internal static void Init()
    {
        ToastForm.CursorPosition = () => new Point(-30000, -30000);
    }
}
