using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using ImageFileFormat = SharpDX.Direct3D9.ImageFileFormat;

namespace VRageRender
{
    struct MyScreenshot
    {
        internal readonly ImageFileFormat Format;
        internal readonly string SavePath;
        internal readonly Vector2 SizeMult;
        internal readonly bool IgnoreSprites;
		internal readonly bool ShowNotification;

        static MyScreenshot()
        {
            Directory.CreateDirectory(Path.Combine(MyFileSystem.UserDataPath, screenshotsFolder));
        }
        
        internal MyScreenshot(string path, Vector2 sizeMult, bool ignoreSprites, bool showNotification)
        {
            SavePath = path ?? GetDefaultScreenshotFilenameWithExtension();
            Format = GetFormat(Path.GetExtension(SavePath).ToLower());
            SizeMult = sizeMult;
            IgnoreSprites = ignoreSprites;
			ShowNotification = showNotification;
        }

        static readonly string screenshotsFolder = "Screenshots";

        static string GetDefaultScreenshotFilenameWithExtension()
        {
            return Path.Combine(
                MyFileSystem.UserDataPath,
                screenshotsFolder,
                MyValueFormatter.GetFormatedDateTimeForFilename(DateTime.Now) + ".png"
               );
        }

        static ImageFileFormat GetFormat(string lowerCaseExtension)
        {
            switch (lowerCaseExtension)
            {
                case ".png":
                    return ImageFileFormat.Png;

                case ".jpg":
                case ".jpeg":
                    return ImageFileFormat.Jpg;

                case ".bmp":
                    return ImageFileFormat.Bmp;

                default:
                    MyRender11.Log.WriteLine("GetFormat: Unhandled extension for image file format.");
                    return ImageFileFormat.Png;
            }
        }
    }
}
