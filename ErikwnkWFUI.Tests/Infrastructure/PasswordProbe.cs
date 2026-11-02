using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>Whether a text box hides what is typed (a password char of any kind).</summary>
internal static class PasswordProbe
{
    public static bool Hides(TextBox box)
    {
        return box.UseSystemPasswordChar || box.PasswordChar != '\0';
    }
}
