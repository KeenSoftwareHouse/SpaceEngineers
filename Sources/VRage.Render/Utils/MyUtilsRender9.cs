using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
//using KeenSoftwareHouse.Library.Extensions;
using VRage.Utils;

using VRageRender;
//using VRageMath;
//using VRageMath.Graphics;

using SharpDX;
using SharpDX.Direct3D9;


namespace VRageRender.Utils
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using MathHelper = VRageMath.MathHelper;
    using VRageRender.Textures;
    using VRageMath;
    using VRage.Animations;
    using System.Xml;
    using VRage.Library.Utils;
    using VRage.FileSystem;

    static class MyUtilsRender9
    {
        public static void AssertTexture(MyTexture2D texture)
        {
            //AssertTextureDxtCompress(texture);
            //AssertTextureMipMapped(texture);
        }
        //  Calculate texture size, based on width * resolution, texture format (number of bytes per one pixel) and number of mip-map levels
        //  Result is in mega bytes (not bytes)
        //  IMPORTANT: I am not sure if I am doing this correctly for non-uniform mip maps (e.g. 512x128) because each dimension should
        //  have different mipmap level count, but there's only one. In forst case, result will be wrong just by few bytes.
        public static double CalculateTextureSizeInMb(Format inputFormat, int inputWidth, int inputHeight, int inputLevelCount)
        {
            int sizeInBytes = 0;

            int width = inputWidth;
            int height = inputHeight;

            for (int level = 0; level < inputLevelCount; level++)
            {
                int sizeOfOneLevel;

                MyDebug.AssertRelease(width >= 1);
                MyDebug.AssertRelease(height >= 1);

                switch (inputFormat)
                {
                    case Format.A8:
                    case Format.L8:
                        sizeOfOneLevel = width * height;
                        break;

                    case Format.A8R8G8B8:
                    case Format.A8B8G8R8:
                    case Format.D24S8:
                    case Format.A2R10G10B10:
                    case Format.Q8W8V8U8:
                        sizeOfOneLevel = width * height * 4;
                        break;

                    case Format.Dxt1:
                        sizeOfOneLevel = (width * height * 3) / 8;
                        break;

                    case Format.Dxt3:
                    case Format.Dxt5:
                        sizeOfOneLevel = (width * height * 4) / 4;
                        break;
                    case Format.R32F:
                        sizeOfOneLevel = (width * height * 4);
                        break;
                    case Format.A16B16G16R16:
                    case Format.A16B16G16R16F:
                        sizeOfOneLevel = (width * height * 4) * 2;
                        break;
                    default:
                        throw new Exception("You are trying to calculate 'texture size in Mb' on a texture whose format is not yet supported by this method. You should extend this method!");
                }

                sizeInBytes += sizeOfOneLevel;

                if (width > 1) width /= 2;
                if (height > 1) height /= 2;
            }

            return sizeInBytes / 1024.0 / 1024.0;
        }
        public static double GetTextureSizeInMb(MyTexture2D texture)
        {
            return CalculateTextureSizeInMb(texture.Format, texture.Width, texture.Height, texture.LevelCount);
        }
        public static double GetTextureSizeInMb(MyTextureCube texture)
        {
            return 6 * CalculateTextureSizeInMb(texture.Format, texture.Size, texture.Size, texture.LevelCount);
        }
        static void AssertTextureDxtCompress(MyTexture2D texture)
        {
            MyDebug.AssertRelease(IsTextureDxtCompressed(texture));
        }
        static void AssertTextureMipMapped(MyTexture2D texture)
        {
            //MyCommonDebugUtils.AssertRelease(IsTextureMipMapped(texture));
        }
        private static MyTextureAtlas LoadTextureAtlas(string textureDir, string atlasFile)
        {
            var fsPath = Path.Combine(MyFileSystem.ContentPath, atlasFile);
            if (!File.Exists(fsPath))
            {
                MyLog.Default.WriteLine("Warning: " + atlasFile + " not found.");
                return null;
            }

            try
            {
                var atlas = new MyTextureAtlas(64);
                
                using (var file = MyFileSystem.OpenRead(fsPath))
                using (StreamReader sr = new StreamReader(file))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (line.StartsWith("#"))
                            continue;
                        if (line.Trim(' ').Length == 0)
                            continue;

                        string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                        string name = parts[0];
                        string atlasName = parts[1];

                        Vector4 uv = new Vector4(
                            Convert.ToSingle(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[7], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[8], System.Globalization.CultureInfo.InvariantCulture));

                        MyTexture2D atlasTexture = MyTextureManager.GetTexture<MyTexture2D>(textureDir + atlasName);
                        MyTextureAtlasItem item = new MyTextureAtlasItem(atlasTexture, uv);
                        atlas.Add(name, item);
                    }
                }

                return atlas;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Warning: " + e.ToString());
            }

            return null;
        }
        public static BoundingFrustum UnprojectRectangle(VRageMath.Rectangle source, Viewport viewport, Matrix viewMatrix, Matrix projectionMatrix)
        {
            // Point in screen space of the center of the region selected
            Vector2 regionCenterScreen = new Vector2(source.Center.X, source.Center.Y);
            // Generate the projection matrix for the screen region
            Matrix regionProjMatrix = projectionMatrix;
            // Calculate the region dimensions in the projection matrix. M11 is inverse of width, M22 is inverse of height.
            regionProjMatrix.M11 /= ((float)source.Width / (float)viewport.Width);
            regionProjMatrix.M22 /= ((float)source.Height / (float)viewport.Height);
            // Calculate the region center in the projection matrix. M31 is horizonatal center.
            regionProjMatrix.M31 = (regionCenterScreen.X - (viewport.Width / 2f)) / ((float)source.Width / 2f);

            // M32 is vertical center. Notice that the screen has low Y on top, projection has low Y on bottom.
            regionProjMatrix.M32 = -(regionCenterScreen.Y - (viewport.Height / 2f)) / ((float)source.Height / 2f);

            return new BoundingFrustum(viewMatrix * regionProjMatrix);
        }
        public static void LoadTextureAtlas(string[] enumsToStrings, string textureDir, string atlasFile, out MyTexture2D texture, out MyAtlasTextureCoordinate[] textureCoords)
        {
            //MyTextureAtlas atlas = contentManager.Load<MyTextureAtlas>(atlasFile);

            MyTextureAtlas atlas = LoadTextureAtlas(textureDir, atlasFile);

            //  Here we define particle texture coordinates inside of texture atlas
            textureCoords = new MyAtlasTextureCoordinate[enumsToStrings.Length];

            texture = null;

            for (int i = 0; i < enumsToStrings.Length; i++)
            {
                MyTextureAtlasItem textureAtlasItem = atlas[enumsToStrings[i]];

                textureCoords[i] = new MyAtlasTextureCoordinate(new Vector2(textureAtlasItem.UVOffsets.X, textureAtlasItem.UVOffsets.Y), new Vector2(textureAtlasItem.UVOffsets.Z, textureAtlasItem.UVOffsets.W));

                //  Texture atlas content processor support having more DDS files for one atlas, but we don't want it (because we want to have all particles in one texture, so we can draw fast).
                //  So here we just take first and only texture.
                if (texture == null)
                {
                    texture = textureAtlasItem.AtlasTexture;
                }
            }
        }
        public static bool IsTextureDxtCompressed(MyTexture2D texture)
        {
            return texture.Format == Format.Dxt1 || texture.Format == Format.Dxt3 || texture.Format == Format.Dxt5;
        }
        public static bool IsTextureMipMapped(MyTexture2D texture)
        {
            return texture.LevelCount > 1;
        }
        //  Calculate halfpixel for texture coordinate fix when we copy texture to render target and want
        //  pixels and texels to match precisely.
        //  IMPORTANT: Sometimes half-pixel depends on screen resolution, but sometimes you need to read from
        //  low resolution render targets, and then you need to supply size of that render target (not screen size)
        public static Vector2 GetHalfPixel(int screenSizeX, int screenSizeY)
        {
            return new Vector2(0.5f / (float)screenSizeX, 0.5f / (float)screenSizeY);
        }
        //  Calculates coordinates for quad that lies on line defined by two points and is always facing the camera. It thickness is defined in metres.
        //  It is used for drawing bullet lines, debris flying from explosions, anything that isn't quad but is line.
        //  IMPORTANT: Parameter 'polyLine' is refed only for performance. Don't change it inside the method.
        public static void GetPolyLineQuad(out MyQuadD retQuad, ref MyPolyLineD polyLine)
        {
            Vector3D toCamera = MyRenderCamera.Position - polyLine.Point0;
            Vector3D cameraToPoint;
            if (!MyUtils.HasValidLength(toCamera))
            {
                // When camera at point, choose random direction
                cameraToPoint = Vector3D.Forward;
            }
            else
            {
                cameraToPoint = MyUtils.Normalize(toCamera);
            }
            Vector3D sideVector = MyUtils.GetVector3Scaled(Vector3D.Cross((Vector3D)polyLine.LineDirectionNormalized, cameraToPoint), polyLine.Thickness);

            retQuad.Point0 = polyLine.Point0 - sideVector;
            retQuad.Point1 = polyLine.Point1 - sideVector;
            retQuad.Point2 = polyLine.Point1 + sideVector;
            retQuad.Point3 = polyLine.Point0 + sideVector;
        }

    }

}