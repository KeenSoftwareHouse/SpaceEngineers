using System;
using System.IO;
using System.Collections.Generic;
using VRage.Utils;

using SharpDX.Direct3D9;
using SharpDX;
using VRage.Library.Utils;
using VRage.FileSystem;

//  Screenshot object survives only one DRAW after created. We delete it immediatelly. So if 'm_screenshot'
//  is not null we know we have to take screenshot and set it to null.
//  All files are saved under same date/time names

namespace VRageRender
{
    class MyScreenshot
    {
        static readonly string screenshotsFolder = "Screenshots";

        readonly string m_datetimePrefix;
        readonly string m_fullPathToSave;
        VRageMath.Vector2 m_sizeMultiplier = VRageMath.Vector2.One;

        public Surface DefaultSurface;
        public Surface DefaultDepth;
        public readonly bool IgnoreSprites;

        internal bool ShowNotification { get; private set; }

        public MyScreenshot(VRageMath.Vector2 sizeMultiplier, string saveToPath, bool ignoreSprites, bool showNotification)
        {
            MyRender.Log.WriteLine("MyScreenshot.Constructor() - START");
            MyRender.Log.IncreaseIndent();

            System.Diagnostics.Debug.Assert(sizeMultiplier.X > 0 && sizeMultiplier.Y > 0);
            m_sizeMultiplier = sizeMultiplier;
            IgnoreSprites = ignoreSprites;
            ShowNotification = showNotification;

            m_fullPathToSave = saveToPath;
            m_datetimePrefix = MyValueFormatter.GetFormatedDateTimeForFilename(DateTime.Now);

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyScreenshot.Constructor() - END");
        }

        public string GetFilename(string name)
        {
            if (!String.IsNullOrEmpty(m_fullPathToSave))
            {
                return m_fullPathToSave;
            }
            else
            {
                return Path.Combine(screenshotsFolder, m_datetimePrefix + "_" + name);
            }
        }

        public static string SaveScreenshot(Texture tex, string file)
        {
#if !XB1
            MyRender.Log.WriteLine("MyScreenshot.SaveTexture2D() - START");
            MyRender.Log.IncreaseIndent();

            string filename = null;

            using (Texture systemTex = new Texture(MyRender.GraphicsDevice, tex.GetLevelDescription(0).Width, tex.GetLevelDescription(0).Height, 1, Usage.None, Format.A8R8G8B8, Pool.SystemMemory))
            {
                string extension = Path.GetExtension(file);

                using (Surface sourceSurface = tex.GetSurfaceLevel(0))
                using (Surface destSurface = systemTex.GetSurfaceLevel(0))
                {
                    MyRender.GraphicsDevice.GetRenderTargetData(sourceSurface, destSurface);
                }
                
                try
                {
                    MyRender.Log.WriteLine("File: " + file);

                    Stack<SharpDX.Rectangle> tiles = new Stack<SharpDX.Rectangle>();

                    int tileWidth = systemTex.GetLevelDescription(0).Width;
                    int tileHeight = systemTex.GetLevelDescription(0).Height;

                    while (tileWidth > 3200)
                    {
                        tileWidth /= 2;
                        tileHeight /= 2;
                    }

                    int widthOffset = 0;
                    int heightOffset = 0;

                    while (widthOffset < systemTex.GetLevelDescription(0).Width)
                    {
                        while (heightOffset < systemTex.GetLevelDescription(0).Height)
                        {
                            tiles.Push(new SharpDX.Rectangle(widthOffset, heightOffset, widthOffset + tileWidth, heightOffset + tileHeight));
                            heightOffset += tileHeight;
                        }

                        heightOffset = 0;
                        widthOffset += tileWidth;
                    }

                    bool multipleTiles = tiles.Count > 1;

                    int sc = 0;
                    byte[] data = new byte[tileWidth * tileHeight * 4];

                    int sysTexWidth = systemTex.GetLevelDescription(0).Width;
                    int sysTexHeight = systemTex.GetLevelDescription(0).Height;
                    while (tiles.Count > 0)
                    {
                        SharpDX.Rectangle rect = tiles.Pop();
                        //texture2D.GetData<byte>(0, rect2, data, 0, data.Length);
                        DataStream ds;
                        //DataRectangle dr = texture2D.LockRectangle(0, rect2, LockFlags.ReadOnly, out ds);
                        DataRectangle dr = systemTex.LockRectangle(0, LockFlags.ReadOnly, out ds);

                        //we have to go line by line..
                        ds.Seek(rect.Y * sysTexWidth * 4, SeekOrigin.Begin);
                        int targetOffset = 0;

                        int linesCount = tileHeight;

                        int pixelsBefore = rect.X;
                        int pixelsAfter = sysTexWidth - tileWidth - rect.X;

                        while (linesCount-- > 0)
                        {
                            if (pixelsBefore > 0)
                                ds.Seek(pixelsBefore * 4, SeekOrigin.Current);
                            
                            ds.Read(data, targetOffset, tileWidth * 4);
                            targetOffset += tileWidth * 4;
                            
                            if (pixelsAfter > 0 && linesCount > 0)
                                ds.Seek(pixelsAfter * 4, SeekOrigin.Current);
                        }

                        systemTex.UnlockRectangle(0);
                        filename = file;

                        if (multipleTiles)
                        {
                            filename = file.Replace(extension, "_" + sc.ToString("##00") + extension);
                        }

                        using (var stream = MyFileSystem.OpenWrite(MyFileSystem.UserDataPath, filename))
                        {
                            using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(tileWidth, tileHeight))
                            {
                                System.Drawing.Imaging.BitmapData imageData = image.LockBits(new System.Drawing.Rectangle(0, 0, tileWidth, tileHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                                System.Runtime.InteropServices.Marshal.Copy(data, 0, imageData.Scan0, data.Length);

                                if (extension == ".png")
                                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                else if (extension == ".jpg" || extension == ".jpeg")
                                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                else if (extension == ".bmp")
                                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                                else
                                    throw new InvalidOperationException("Invalid file extension: " + extension + ", please use png, jpg or bmp");

                                image.UnlockBits(imageData);
                            }

                            //texture2D.SaveAsPng(stream, texture2D.Width, texture2D.Height);
                            //BaseTexture.ToStream(texture2D, ImageFileFormat.Png);
                        }

                        sc++;
                        GC.Collect();
                    }
                }
                catch (Exception exc)
                {
                    //  Write exception to log, but continue as if nothing wrong happened
                    MyRender.Log.WriteLine(exc);
                    filename = null;
                }
            }
            //BaseTexture.ToFile(texture2D, "c:\\test.png", ImageFileFormat.Png);

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyScreenshot.SaveTexture2D() - END");

            return filename;
#else
            System.Diagnostics.Debug.Assert(false, "Not Screenshoot support on XB1 yet!");
            return null;
#endif
        }

        //  Failure while saving will not crash the game, only log an exception into log file
        public string SaveTexture2D(Texture texture2D, string name)
        {
            return SaveScreenshot(texture2D, GetFilename(name + ".png"));
        }

        public VRageMath.Vector2 SizeMultiplier
        {
            get { return m_sizeMultiplier; }
        }
    }
}
