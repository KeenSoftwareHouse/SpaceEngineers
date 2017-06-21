using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX.Direct3D;
using SharpDX.Toolkit.Graphics;
using VRage.FileSystem;
using VRage.Render11.Common;
using VRage.Utils;
using VRageMath;
using VRageRender;
namespace VRage.Render11.Resources
{
    internal static class MyResourceUtils
    {
        public static Format MakeSrgb(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm:
                    return Format.R8G8B8A8_UNorm_SRgb;
                case Format.B8G8R8A8_UNorm:
                    return Format.B8G8R8A8_UNorm_SRgb;
                case Format.B8G8R8X8_UNorm:
                    return Format.B8G8R8X8_UNorm_SRgb;
                case Format.BC1_UNorm:
                    return Format.BC1_UNorm_SRgb;
                case Format.BC2_UNorm:
                    return Format.BC2_UNorm_SRgb;
                case Format.BC3_UNorm:
                    return Format.BC3_UNorm_SRgb;
                case Format.BC7_UNorm:
                    return Format.BC7_UNorm_SRgb;
            }
            return fmt;
        }

        public static Format MakeLinear(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    return Format.R8G8B8A8_UNorm;
                case Format.B8G8R8A8_UNorm_SRgb:
                    return Format.B8G8R8A8_UNorm;
                case Format.B8G8R8X8_UNorm_SRgb:
                    return Format.B8G8R8X8_UNorm;
                case Format.BC1_UNorm_SRgb:
                    return Format.BC1_UNorm;
                case Format.BC2_UNorm_SRgb:
                    return Format.BC2_UNorm;
                case Format.BC3_UNorm_SRgb:
                    return Format.BC3_UNorm;
                case Format.BC7_UNorm_SRgb:
                    return Format.BC7_UNorm;
            }
            return fmt;
        }

        public static bool IsSrgb(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    return true;
                case Format.B8G8R8A8_UNorm_SRgb:
                    return true;
                case Format.B8G8R8X8_UNorm_SRgb:
                    return true;
                case Format.BC1_UNorm_SRgb:
                    return true;
                case Format.BC2_UNorm_SRgb:
                    return true;
                case Format.BC3_UNorm_SRgb:
                    return true;
                case Format.BC7_UNorm_SRgb:
                    return true;
            }
            return false;
        }

        public static int GetTexelBitSize(Format fmt)
        {
            #region Format switch
            const int bitsInByte = 8;

            switch (fmt)
            {
                case Format.Unknown:
                    throw new ArgumentException(string.Format("The format {0} cannot be used.", fmt), "fmt");

                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return 16 * bitsInByte;

                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return 12 * bitsInByte;

                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                    return 8 * bitsInByte;

                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                case Format.R11G11B10_Float:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                    return 4 * bitsInByte;

                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                    return 2 * bitsInByte;

                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                    return 1 * bitsInByte;

                case Format.R1_UNorm:
                    throw new ArgumentException(string.Format("The one-bit format {0} cannot be used.", fmt), "fmt");

                case Format.R9G9B9E5_Sharedexp:
                case Format.R8G8_B8G8_UNorm:
                case Format.G8R8_G8B8_UNorm:
                    return 4 * bitsInByte;

                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRgb:
                    return bitsInByte / 2;

                case Format.BC2_Typeless:
                case Format.BC2_UNorm:
                case Format.BC2_UNorm_SRgb:
                case Format.BC3_Typeless:
                case Format.BC3_UNorm:
                case Format.BC3_UNorm_SRgb:
                    return 1 * bitsInByte;

                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return bitsInByte / 2;

                case Format.BC5_Typeless:
                case Format.BC5_UNorm:
                case Format.BC5_SNorm:
                    return 1 * bitsInByte;

                case Format.B5G6R5_UNorm:
                case Format.B5G5R5A1_UNorm:
                    return 2 * bitsInByte;

                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                case Format.B8G8R8X8_UNorm_SRgb:
                    return 4 * bitsInByte;

                case Format.BC6H_Typeless:
                case Format.BC6H_Uf16:
                case Format.BC6H_Sf16:
                case Format.BC7_Typeless:
                case Format.BC7_UNorm:
                case Format.BC7_UNorm_SRgb:
                    return 1 * bitsInByte;

                case Format.B4G4R4A4_UNorm:
                    return 2 * bitsInByte;

                case Format.AYUV:
                case Format.Y410:
                case Format.Y416:
                case Format.NV12:
                case Format.P010:
                case Format.P016:
                case Format.Opaque420:
                case Format.YUY2:
                case Format.Y210:
                case Format.Y216:
                case Format.NV11:
                case Format.AI44:
                case Format.IA44:
                case Format.P8:
                case Format.A8P8:
                    throw new ArgumentException(string.Format("The format {0} is a YUV video format. We don't support those.", fmt), "fmt");

                case Format.P208:
                case Format.V208:
                case Format.V408:
                    throw new ArgumentException(string.Format("The format {0} is unsupported.", fmt), "fmt");

                default:
                    throw new ArgumentOutOfRangeException("fmt", fmt, "Invalid format.");
            }
            #endregion
        }

        public static long GetTextureByteSize(this ITexture texture)
        {
            // CHECK-ME Is this correct?
            Vector3I size = texture.Size3;
            int mipmapCount = texture.MipmapCount;
            long textureSize = 0;
            for (int it = 0; it < mipmapCount; it++)
            {
                textureSize += (long)(size.X * size.Y * size.Z * GetTexelBitSize(texture.Format) / (double)8);
                size.X = size.X / 2 + size.X % 2;
                size.Y = size.Y / 2 + size.X % 2;
            }

            return textureSize;
        }

        public static int GetMipmapsCount(int size)
        {
            int extraOffset = 0;
            int bitOffset = 0;
            while (true)
            {
                bool isBitSet = (size & 1) == 1;
                if (size <= 1)
                    break;
                if (isBitSet)
                    extraOffset = 1;
                bitOffset++;
                size /= 2;
            }
            return 1 + bitOffset + extraOffset;
        }

        public static int GetMipmapSize(int mipmap0Size, int mipmap)
        {
            int mipmapSize = mipmap0Size;
            for (int i = 0; i < mipmap; i++)
            {
                mipmapSize = mipmapSize / 2 + mipmapSize % 2;
            }
            return mipmapSize;
        }

        public static int GetMipmapStride(int mipmap0Size, int mipmap)
        {
            int mipmapSize = GetMipmapSize(mipmap0Size, mipmap);
            int stride = ((mipmapSize / 4) + (mipmapSize % 4 == 0 ? 0 : 1)) * 4;
            return stride;
        }

        public static bool CheckTexturesConsistency(Texture2DDescription desc1, Texture2DDescription desc2)
        {
            return desc1.Format == desc2.Format && desc1.MipLevels == desc2.MipLevels && desc1.Width == desc2.Width && desc1.Height == desc2.Height;
        }

        /// <summary>Normalizes file names into lower case relative path, if possible</summary>
        /// <returns>True if it's a file texture, false if the texture is ram generated</returns>
        public static bool NormalizeFileTextureName(ref string name)
        {
            Uri uri;
            return NormalizeFileTextureName(ref name, out uri);
        }

        [ThreadStatic]
        static Dictionary<string, MyTuple<string, Uri>> m_normalizeFileTextureResults = new Dictionary<string, MyTuple<string, Uri>>();
        /// <summary>Normalizes file names into lower case relative path, if possible</summary>
        /// <returns>True if it's a file texture, false if the texture is ram generated</returns>
        public static bool NormalizeFileTextureName(ref string name, out Uri uri)
        {
            // Check for valid generated texture name first
            if (MyRenderProxy.IsValidGeneratedTextureName(name))
            {
                uri = null;
                return false;
            }

            if (m_normalizeFileTextureResults.ContainsKey(name))
            {
                string tmpName = m_normalizeFileTextureResults[name].Item1;
                uri = m_normalizeFileTextureResults[name].Item2;
                name = tmpName;
                return true;
            }

            if (Uri.TryCreate(name, UriKind.Absolute, out uri))
            {
                // Path is absolute
                string tmpName = MakeRelativePath(uri).ToLower();
                m_normalizeFileTextureResults.Add(name, new MyTuple<string, Uri>(tmpName, uri));
                name = tmpName;
                return true;
            }

            // This call causes memory leaks, therefore there is "caching" of the results
            uri = new Uri(Path.Combine(MyFileSystem.ContentPath, name));
            m_normalizeFileTextureResults.Add(name, new MyTuple<string, Uri>(name, uri));
            return true;
        }

        /// <returns>Returns resolved texture path (rooted and without . or ..)</returns>
        public static string GetTextureFullPath(string textureName, string contentPath = null)
        {
            if (string.IsNullOrEmpty(textureName))
                return string.Empty;

            // Check for valid generated texture name first
            if (MyRenderProxy.IsValidGeneratedTextureName(textureName))
                return textureName;

            string path = null;
            if (Path.IsPathRooted(textureName))
            {
                path = textureName;
            }
            else
            {

                if (!string.IsNullOrEmpty(contentPath))
                {
                    // Mod models may still refer to vanilla texture
                    string localpath = Path.Combine(contentPath, textureName);
                    if (MyFileSystem.FileExists(localpath))
                        path = localpath;
                }

                if (path == null)
                    path = Path.Combine(MyFileSystem.ContentPath, textureName);
            }

            path = path.ToLower().Replace('/', '\\');
            return Path.GetFullPath(path);
        }

        static string MakeRelativePath(Uri toUri)
        {
            String contentPath = TerminatePath(MyFileSystem.ContentPath);

            Uri fromUri = new Uri(contentPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                // path can't be made relative.
                return toUri.AbsoluteUri;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        static string TerminatePath(string path)
        {
            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] == Path.DirectorySeparatorChar)
                return path;

            return path + Path.DirectorySeparatorChar;
        }
    }
}
