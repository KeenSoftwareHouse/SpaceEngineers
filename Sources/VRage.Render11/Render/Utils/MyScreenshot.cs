﻿using System;
using System.IO;
using SharpDX.Direct3D11;
using VRage.FileSystem;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    struct MyScreenshot
    {
        internal readonly ImageFileFormat Format;
        internal readonly string SavePath;
        internal readonly Vector2 SizeMult;
        internal readonly bool IgnoreSprites;

        static MyScreenshot()
        {
            Directory.CreateDirectory(Path.Combine(MyFileSystem.UserDataPath, screenshotsFolder));
        }
        
        internal MyScreenshot(string path, Vector2 sizeMult, bool ignoreSprites)
        {
            SavePath = path ?? GetDefaultScreenshotFilenameWithExtension();
            Format = GetFormat(Path.GetExtension(SavePath).ToLower());
            SizeMult = sizeMult;
            IgnoreSprites = ignoreSprites;
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

                case ".dds":
                    return ImageFileFormat.Dds;

                case ".bmp":
                    return ImageFileFormat.Bmp;

                default:
                    MyRender11.Log.WriteLine("GetFormat: Unhandled extension for image file format.");
                    return ImageFileFormat.Png;
            }
        }
    }
}
