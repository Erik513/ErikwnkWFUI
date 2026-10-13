using System.Reflection;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// How many handlers listen to <c>UIColors.ThemeChanged</c> just now: a control listens while it has a window. Other
/// tests may be showing controls at the same time, so compare the count while a control is shown with the count after
/// it is gone, not with an absolute number.
/// </summary>
public static class ThemeListeners
{
    public static int Count()
    {
        FieldInfo field = typeof(UIColors).GetField("ThemeChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Delegate? handlers = (Delegate?)field.GetValue(null);

        return handlers == null ? 0 : handlers.GetInvocationList().Length;
    }
}
