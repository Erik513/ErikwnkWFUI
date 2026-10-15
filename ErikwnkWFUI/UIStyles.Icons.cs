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
            public static Image Web { get { return UIIcons.Web; } }
            public static Image Folder { get { return UIIcons.Folder; } }
            public static Image Document { get { return UIIcons.Document; } }
            public static Image Application { get { return UIIcons.Application; } }

            public static Image LoadEmbedded(Assembly assembly, string resourceName)
            {
                return UIIcons.LoadEmbedded(assembly, resourceName);
            }
        }
    }
}
