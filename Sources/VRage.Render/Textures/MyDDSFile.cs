#define COLOR_SAVE_TO_ARGB

using System;
using System.IO;

using SharpDX.Direct3D9;
using VRage.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Reflection;
using VRage.Library.Utils;
using VRage.FileSystem;

//  Read/Write dds files from/to files or from streams.
namespace VRageRender.Textures
{
    static class MyDDSFile
    {
        delegate ImageInformation GetImageInfoFromFileWDelegate(string srcFileRef);

        static readonly Type D3DXType = typeof(Texture).Assembly.GetType("SharpDX.Direct3D9.D3DX9");
        static readonly MethodInfo GetImageInfoFromFileWMethodInfo = D3DXType.GetMethod("GetImageInfoFromFileW");
        static readonly GetImageInfoFromFileWDelegate GetImageInfoFromFileW = GetImageInfoFromFileWMethodInfo.CreateDelegate<GetImageInfoFromFileWDelegate>();

        public static void DDSFromFile(string fileName, Device device, bool loadMipMap, int offsetMipMaps, out SharpDX.Direct3D9.Texture texture)
        {
            try
            {
                using (var stream = MyFileSystem.OpenRead(fileName))
                {
                    InternalDDSFromStream(stream, device, 0, loadMipMap, offsetMipMaps, out texture);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Failed loading texture " + fileName.ToString());
                MyLog.Default.WriteLine(e.ToString());
                throw;
            }
        }


        public static void DDSFromFile(string fileName, Device device, bool loadMipMap, int offsetMipMaps, out CubeTexture texture)
        {
            try
            {
                using (var stream = MyFileSystem.OpenRead(fileName))
                {
                    InternalDDSFromStream(stream, device, 0, loadMipMap, 0, out texture);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Failed loading texture " + fileName.ToString());
                MyLog.Default.WriteLine(e.ToString());
                throw;
            }
        }

        internal static uint D3DXSkipDDSMipLevels(uint levels)
        {
            var skip_mask = 0x1Fu;
            var levels_shift = 26;
            var box_filter = 5u;

            return ((levels & skip_mask) << levels_shift) | (box_filter);
        }



        private static void InternalDDSFromStream(Stream stream, Device device, int streamOffset, bool loadMipMap, int offsetMipMaps, out SharpDX.Direct3D9.Texture texture)
        {
            var fileStream = stream as FileStream;
            byte[] data = fileStream != null ? null : SharpDX.Utilities.ReadStream(stream);

            int maxTextureDimension = MyRender.GraphicsDevice.Capabilities.MaxTextureWidth;
            if (IntPtr.Size == 4 || MyRender.GraphicsDevice.AvailableTextureMemory < 1024 * 1024 * 600)
            {
                maxTextureDimension = 2048;
            }

            ImageInformation imageInfo;
            if (fileStream != null)
            {
                imageInfo = GetImageInfoFromFileW(fileStream.Name);
            }
            else
            {
                imageInfo = ImageInformation.FromMemory(data);
            }
            var textureWidth = imageInfo.Width;
            var textureHeight = imageInfo.Height;

            textureWidth = Math.Max(1, textureWidth >> offsetMipMaps);
            textureHeight = Math.Max(1, textureHeight >> offsetMipMaps);
            int loadMipmapOffset = offsetMipMaps;
            while (textureWidth > maxTextureDimension || textureHeight > maxTextureDimension)
            {
                loadMipmapOffset++;
                textureWidth = Math.Max(1, textureWidth >> 1);
                textureHeight = Math.Max(1, textureHeight >> 1);
            }

            if (offsetMipMaps == 0 && loadMipmapOffset == 0)
            {
                if (fileStream != null)
                {
                    texture = SharpDX.Direct3D9.Texture.FromFile(device, fileStream.Name, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, Filter.Linear, 0);
                }
                else
                {
                    texture = SharpDX.Direct3D9.Texture.FromMemory(device, data, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, Filter.Linear, 0);
                }
            }
            else if (loadMipmapOffset > 0)
            {
                var skipFilter = (Filter)D3DXSkipDDSMipLevels((uint)loadMipmapOffset);

                if (fileStream != null)
                {
                    texture = SharpDX.Direct3D9.Texture.FromFile(device, fileStream.Name, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, skipFilter, 0);
                }
                else
                {
                    texture = SharpDX.Direct3D9.Texture.FromMemory(device, data, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, skipFilter, 0);
                }
            }
            else
            {
                if (fileStream != null)
                {
                    texture = SharpDX.Direct3D9.Texture.FromFile(device, fileStream.Name, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, Filter.None, 0);
                }
                else
                {
                    texture = SharpDX.Direct3D9.Texture.FromMemory(device, data, 0, 0, 0, Usage.None, Format.Unknown, Pool.Default, Filter.None, Filter.None, 0);
                }

                int width = MipMapSize(offsetMipMaps, texture.GetLevelDescription(0).Width);
                int height = MipMapSize(offsetMipMaps, texture.GetLevelDescription(0).Height);
                int maxLevels = Math.Min(MaxMipMapLevels(width), MaxMipMapLevels(height));
                int actualLevels = Math.Min(maxLevels, texture.LevelCount - offsetMipMaps);

                Format format = texture.GetLevelDescription(0).Format;
                Texture offsetedTexture = new Texture(device, width, height, actualLevels, Usage.Dynamic, format, Pool.Default);
                for (int i = offsetMipMaps, j = 0; j < actualLevels; i++, j++)
                {
                    int levelWidth = MipMapSize(j, width);
                    int levelHeight = MipMapSize(j, height);

                    SharpDX.DataStream ds;
                    texture.LockRectangle(i, LockFlags.ReadOnly, out ds);
                    texture.UnlockRectangle(i);

                    SharpDX.DataStream ds2;
                    offsetedTexture.LockRectangle(j, LockFlags.None, out ds2);
                    ds2.Position = 0;
                    ds2.Write(ds.DataPointer, 0, (int)MipMapSizeInBytes(levelWidth, levelHeight, format));
                    offsetedTexture.UnlockRectangle(j);
                }

                texture.Dispose();
                texture = offsetedTexture;
            }
        }

        //Get the size in bytes for a mip-map level.
        private static int MipMapSizeInBytes(int width, int height, Format compressionFormat)
        {
            switch (compressionFormat)
            {
                case Format.R32F:
                    return width * height * 4;

                case Format.R16F:
                    return width * height * 2;

                case Format.A32B32G32R32F:
                    return width * height * 16;

                case Format.A16B16G16R16F:
                    return width * height * 8;

                case Format.V8U8:
                    return width * height * 2;

                case Format.Q8W8V8U8:
                    return width * height * 4;

                case Format.G16R16F:
                    return width * height * 4;

                case Format.G32R32F:
                    return width * height * 8;

                case Format.A16B16G16R16:
                    return width * height * 8;

                case Format.Dxt1:
                    {
                        int blockSize = 8;
                        return ((width + 3) / 4) * ((height + 3) / 4) * blockSize;
                    }

                case Format.Dxt2:
                case Format.Dxt3:
                case Format.Dxt4:
                case Format.Dxt5:
                    {
                        int blockSize = 16;
                        return ((width + 3) / 4) * ((height + 3) / 4) * blockSize;
                    }

                default:
                    return width * height * 4;
            }
        }

        //We need the the mip size, we shift until we get there but the smallest mip must be at least of 1 pixel.
        private static int MipMapSize(int map, int size)
        {
            for (int i = 0; i < map; i++)
                size >>= 1;
            if (size < 4)
                return 4;
            return size;
        }

        private static int MaxMipMapLevels(int size)
        {
            int i = 0;
            while (size >= 4)
            {
                i++;
                size >>= 1;
            }
            return i;
        }

        //loads the data from a stream in to a texture object.
        private static void InternalDDSFromStream(Stream stream, Device device, int streamOffset, bool loadMipMap, int offsetMipMaps, out CubeTexture texture)
        {
            stream.Position = 0;
            texture = SharpDX.Direct3D9.CubeTexture.FromStream(device, stream, 0, 0, offsetMipMaps, Usage.None, Format.Unknown, Pool.Default, Filter.None, Filter.None, 0);
        }


        /// <summary>
        /// Save a texture from memory to a file.
        /// (Supported formats : Dxt1,Dxt3,Dxt5,A8R8G8B8/Color,A4R4G4B4,A1R5G5B5,R5G6B5,A8,
        /// FP32/Single,FP16/HalfSingle,FP32x4/Vector4,FP16x4/HalfVector4,CxV8U8/NormalizedByte2/CxVU,Q8VW8V8U8/NormalizedByte4/8888QWVU
        /// ,HalfVector2/G16R16F/16.16fGR,Vector2/G32R32F,G16R16/RG32/1616GB,A8B8G8R8,A2B10G10R10/Rgba1010102,A16B16G16R16/Rgba64)
        /// </summary>
        /// <param name="fileName">The name of the file where you want to save the texture.</param>
        /// <param name="saveMipMaps">Save the complete mip-map chain ?</param>
        /// <param name="texture">The texture that you want to save.</param>
        /// <param name="throwExceptionIfFileExist">Throw an exception if the file exists ?</param>
        public static void DDSToFile(string fileName, bool saveMipMaps, BaseTexture texture, bool throwExceptionIfFileExist)
        {
            if (throwExceptionIfFileExist && File.Exists(fileName))
            {
                throw new Exception("The file allready exists and \"throwExceptionIfFileExist\" is true");
            }

            Stream fileStream = null;
            try
            {
                fileStream = File.Create(fileName);
                BaseTexture.ToStream(texture, ImageFileFormat.Dds);
                //DDSToStream(fileStream, 0, saveMipMaps, texture);
                // sometimes needed because of out of memory and this helps
                //GC.Collect(2);
            }
            catch (Exception x)
            {
                throw x;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
        }
    }
}
