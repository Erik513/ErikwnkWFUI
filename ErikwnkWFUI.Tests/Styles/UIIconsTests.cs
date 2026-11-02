using System.Drawing;
using System.Reflection;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>The icons that ship with the library, scaled copies of an image, and loading an image from the resources of an assembly.</summary>
public class UIIconsTests
{
    [Fact]
    public void TheFourIcons_ExistAndAreTheSameImageOnEveryAccess()
    {
        foreach (Func<Image> icon in new Func<Image>[] { () => UIIcons.Web, () => UIIcons.Folder, () => UIIcons.Document, () => UIIcons.Application })
        {
            Image first = icon();

            Assert.NotNull(first);
            Assert.True(first.Width > 0 && first.Height > 0);
            Assert.Same(first, icon());
        }
    }

    [Fact]
    public void Resize_MakesASquareCopyOfTheSize_AndLeavesTheSourceAlone()
    {
        Size before = UIIcons.Web.Size;

        using Image small = UIIcons.Resize(UIIcons.Web, 16);

        Assert.NotSame(UIIcons.Web, small);
        Assert.Equal(new Size(16, 16), small.Size);
        Assert.Equal(before, UIIcons.Web.Size);
    }

    [Fact]
    public void Resize_WithNothingToScale_ReturnsTheSourceItself()
    {
        using Bitmap source = new Bitmap(8, 8);

        Assert.Null(UIIcons.Resize(null!, 16));
        Assert.Same(source, UIIcons.Resize(source, 0));
        Assert.Same(source, UIIcons.Resize(source, -5));
    }

    [Fact]
    public void LoadEmbedded_GivesACopyThatBelongsToTheCaller_SoTheSharedIconIsNotAffectedByDisposingIt()
    {
        Assembly library = typeof(UIIcons).Assembly;

        Image loaded = UIIcons.LoadEmbedded(library, "ErikwnkWFUI.Icons.IconWeb.png");

        Assert.NotSame(UIIcons.Web, loaded);
        Assert.Equal(UIIcons.Web.Size, loaded.Size);

        loaded.Dispose();

        Assert.True(UIIcons.Web.Width > 0);
    }

    [Fact]
    public void LoadEmbedded_WithAResourceThatIsNotThere_SaysWhichOnesAre()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => UIIcons.LoadEmbedded(typeof(UIIcons).Assembly, "Nothing.png"));

        Assert.Contains("Nothing.png", error.Message);
        Assert.Contains("ErikwnkWFUI.Icons.IconWeb.png", error.Message);
    }

    [Fact]
    public void LoadEmbedded_WithoutAnAssemblyOrAName_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => UIIcons.LoadEmbedded(null!, "x.png"));
        Assert.Throws<ArgumentNullException>(() => UIIcons.LoadEmbedded(typeof(UIIcons).Assembly, null!));
        Assert.Throws<ArgumentNullException>(() => UIIcons.LoadEmbedded(typeof(UIIcons).Assembly, "   "));
    }
}
