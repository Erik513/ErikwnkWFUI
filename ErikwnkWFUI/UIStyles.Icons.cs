using System.Drawing;
using System.Reflection;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// ErikwnkWFUI's own bundled icons (Web/Folder/Document/Application),
        /// plus <see cref="LoadEmbedded"/> for loading your own app's icon
        /// from its embedded resources the same way - see
        /// ErikwnkWFUI/README.md for why that's preferable to a loose file.
        /// </summary>
        public static class Icons
        {
            /// <summary>An icon of a web page.</summary>
            public static Image Web { get { return UIIcons.Web; } }
            /// <summary>An icon of a folder.</summary>
            public static Image Folder { get { return UIIcons.Folder; } }
            /// <summary>An icon of a document.</summary>
            public static Image Document { get { return UIIcons.Document; } }
            /// <summary>An icon of a program.</summary>
            public static Image Application { get { return UIIcons.Application; } }

            /// <summary>An image from an embedded resource of an assembly, a copy that belongs to the caller. See <see cref="UIIcons.LoadEmbedded"/> for the exceptions.</summary>
            public static Image LoadEmbedded(Assembly assembly, string resourceName)
            {
                return UIIcons.LoadEmbedded(assembly, resourceName);
            }
        }
    }
}
