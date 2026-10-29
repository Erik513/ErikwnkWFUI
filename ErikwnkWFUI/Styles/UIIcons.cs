using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace ErikwnkWFUI.Styles
{
    // Reusable step/action icons, shipped as embedded resources in
    // ErikwnkWFUI.dll so multiple projects can use them without keeping
    // their own copy of the image files.
    /// <summary>
    /// The icons that ship with the library (a web page, a folder, a document, a program), as images made from the embedded resources, and two helpers for
    /// your own: <see cref="Resize"/> and <see cref="LoadEmbedded"/>.
    /// </summary>
    /// <remarks>The four icons are one image each, shared by everyone and for the whole run of the program: do not dispose them and do not draw into them.</remarks>
    public static class UIIcons
    {
        private static readonly Lazy<Image> WebIcon = new Lazy<Image>(() => LoadIcon("IconWeb.png"));
        private static readonly Lazy<Image> FolderIcon = new Lazy<Image>(() => LoadIcon("IconFolder.png"));
        private static readonly Lazy<Image> DocumentIcon = new Lazy<Image>(() => LoadIcon("IconDocument.png"));
        private static readonly Lazy<Image> ApplicationIcon = new Lazy<Image>(() => LoadIcon("IconApplication.png"));

        /// <summary>An icon of a web page.</summary>
        public static Image Web => WebIcon.Value;
        /// <summary>An icon of a folder.</summary>
        public static Image Folder => FolderIcon.Value;
        /// <summary>An icon of a document.</summary>
        public static Image Document => DocumentIcon.Value;
        /// <summary>An icon of a program.</summary>
        public static Image Application => ApplicationIcon.Value;

        // (The scaled copy is square: <size> by <size>, whatever the shape of the source.)
        /// <summary>
        /// A scaled copy of an image, <paramref name="size"/> by <paramref name="size"/> pixels, for example for a button that draws its image at a fixed pixel size
        /// (<c>Button.Image</c> does not scale itself). The caller disposes the copy. Where there is nothing to scale (<paramref name="source"/> is null, or
        /// <paramref name="size"/> is 0 or less) it returns <paramref name="source"/> itself, which is then not a copy: do not dispose it for one of the shared icons.
        /// </summary>
        public static Image Resize(Image source, int size)
        {
            if (source == null || size <= 0)
                return source;

            Bitmap resized = new Bitmap(size, size);

            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, 0, 0, size, size);
            }

            return resized;
        }

        private static Image LoadIcon(string fileName)
        {
            return LoadEmbedded(Assembly.GetExecutingAssembly(), $"ErikwnkWFUI.Icons.{fileName}");
        }

        /// <summary>
        /// Loads an image from an embedded resource in the given assembly. Intended
        /// for consuming apps to load their own logo/icon the same way ErikwnkWFUI
        /// loads its bundled icons: baked into the assembly instead of a loose file
        /// next to the exe, so it can't go missing or get left behind by an update.
        /// The image is a copy that belongs to the caller (who disposes it).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null, or <paramref name="resourceName"/> is null, empty or only white space.</exception>
        /// <exception cref="InvalidOperationException">The assembly has no resource of that name (the message lists the ones it has).</exception>
        public static Image LoadEmbedded(Assembly assembly, string resourceName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentNullException(nameof(resourceName));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string available = string.Join(", ", assembly.GetManifestResourceNames());
                    throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' was not found in assembly " +
                        $"'{assembly.GetName().Name}'. Available resources: {available}");
                }

                using (Image loaded = Image.FromStream(stream))
                {
                    // Copy it so the stream can be closed safely afterward.
                    return new Bitmap(loaded);
                }
            }
        }
    }
}
