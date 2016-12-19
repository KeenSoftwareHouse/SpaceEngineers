using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.WIC;
using System.IO;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using ImageFileFormat = SharpDX.Direct3D9.ImageFileFormat;

namespace VRageRender
{
    class MyTextureData: MyImmediateRC
    {
        internal static bool ToFile(IResource res, string path, ImageFileFormat fmt)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)); // if the directory is not created, the "new FileStream" will fail
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    Save(res, stream, fmt);
                }

                return true;
            }
            catch (Exception e)
            {
                MyRender11.Log.WriteLine("SaveResourceToFile()");
                MyRender11.Log.IncreaseIndent();
                MyRender11.Log.WriteLine(string.Format("Failed to save screenshot {0}: {1}", path, e));
                MyRender11.Log.DecreaseIndent();

                return false;
            }
        }

        internal static byte[] ToData(IResource res, byte[] screenData, ImageFileFormat fmt)
        {
            try
            {
                using (var stream = screenData == null ? new MemoryStream() : new MemoryStream(screenData, true))
                {
                    Save(res, stream, fmt);
                    return stream.GetBuffer();
                }
            }
            catch (Exception e)
            {
                MyRender11.Log.WriteLine("MyTextureData.ToData()");
                MyRender11.Log.IncreaseIndent();
                MyRender11.Log.WriteLine(string.Format("Failed to extract data: {0}", e));
                MyRender11.Log.DecreaseIndent();

                return null;
            }
        }

        private static void Save(IResource res, Stream stream, ImageFileFormat fmt)
        {
            var texture = res.Resource as Texture2D;
            var textureCopy = new Texture2D(MyRender11.Device, new Texture2DDescription
            {
                Width = (int)texture.Description.Width,
                Height = (int)texture.Description.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = texture.Description.Format,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });
            RC.CopyResource(res, textureCopy);

            DataStream dataStream;
            var dataBox = RC.MapSubresource(
                textureCopy,
                0,
                0,
                MapMode.Read,
                MapFlags.None,
                out dataStream);

            var dataRectangle = new DataRectangle
            {
                DataPointer = dataStream.DataPointer,
                Pitch = dataBox.RowPitch
            };

            var bitmap = new Bitmap(
                MyRender11.WIC,
                textureCopy.Description.Width,
                textureCopy.Description.Height,
                PixelFormatFromFormat(textureCopy.Description.Format), // TODO: should use some conversion from textureCopy.Description.Format
                dataRectangle);

            using (var wicStream = new WICStream(MyRender11.WIC, stream))
            {
                BitmapEncoder bitmapEncoder;
                switch (fmt)
                {
                    case ImageFileFormat.Png:
                        bitmapEncoder = new PngBitmapEncoder(MyRender11.WIC, wicStream);
                        break;
                    case ImageFileFormat.Jpg:
                        bitmapEncoder = new JpegBitmapEncoder(MyRender11.WIC, wicStream);
                        break;
                    case ImageFileFormat.Bmp:
                        bitmapEncoder = new BmpBitmapEncoder(MyRender11.WIC, wicStream);
                        break;
                    default:
                        MyRenderProxy.Assert(false, "Unsupported file format.");
                        bitmapEncoder = null;
                        break;
                }
                if (bitmapEncoder != null)
                {
                    using (var bitmapFrameEncode = new BitmapFrameEncode(bitmapEncoder))
                    {
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
                        var pixelFormat = PixelFormat.FormatDontCare;
                        bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
                        bitmapFrameEncode.WriteSource(bitmap);
                        bitmapFrameEncode.Commit();
                        bitmapEncoder.Commit();
                    }
                    bitmapEncoder.Dispose();
                }
            }

            RC.UnmapSubresource(textureCopy, 0);
            textureCopy.Dispose();
            bitmap.Dispose();
        }

        public static Guid PixelFormatFromFormat(SharpDX.DXGI.Format format)
        {
            switch (format)
            {
                case SharpDX.DXGI.Format.R32G32B32A32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFloat;
                case SharpDX.DXGI.Format.R32G32B32A32_UInt:
                case SharpDX.DXGI.Format.R32G32B32A32_SInt:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFixedPoint;
                case SharpDX.DXGI.Format.R32G32B32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32_Float:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFloat;
                case SharpDX.DXGI.Format.R32G32B32_UInt:
                case SharpDX.DXGI.Format.R32G32B32_SInt:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFixedPoint;
                case SharpDX.DXGI.Format.R16G16B16A16_Typeless:
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_UInt:
                case SharpDX.DXGI.Format.R16G16B16A16_SNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_SInt:
                    return SharpDX.WIC.PixelFormat.Format64bppRGBA;
                case SharpDX.DXGI.Format.R32G32_Typeless:
                case SharpDX.DXGI.Format.R32G32_Float:
                case SharpDX.DXGI.Format.R32G32_UInt:
                case SharpDX.DXGI.Format.R32G32_SInt:
                case SharpDX.DXGI.Format.R32G8X24_Typeless:
                case SharpDX.DXGI.Format.D32_Float_S8X24_UInt:
                case SharpDX.DXGI.Format.R32_Float_X8X24_Typeless:
                case SharpDX.DXGI.Format.X32_Typeless_G8X24_UInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R10G10B10A2_Typeless:
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                case SharpDX.DXGI.Format.R10G10B10A2_UInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA1010102;
                case SharpDX.DXGI.Format.R11G11B10_Float:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R8G8B8A8_Typeless:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                case SharpDX.DXGI.Format.R8G8B8A8_SNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_SInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA;
                case SharpDX.DXGI.Format.R16G16_Typeless:
                case SharpDX.DXGI.Format.R16G16_Float:
                case SharpDX.DXGI.Format.R16G16_UNorm:
                case SharpDX.DXGI.Format.R16G16_UInt:
                case SharpDX.DXGI.Format.R16G16_SNorm:
                case SharpDX.DXGI.Format.R16G16_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R32_Typeless:
                case SharpDX.DXGI.Format.D32_Float:
                case SharpDX.DXGI.Format.R32_Float:
                case SharpDX.DXGI.Format.R32_UInt:
                case SharpDX.DXGI.Format.R32_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R24G8_Typeless:
                case SharpDX.DXGI.Format.D24_UNorm_S8_UInt:
                case SharpDX.DXGI.Format.R24_UNorm_X8_Typeless:
                    return SharpDX.WIC.PixelFormat.Format32bppGrayFloat;
                case SharpDX.DXGI.Format.X24_Typeless_G8_UInt:
                case SharpDX.DXGI.Format.R9G9B9E5_Sharedexp:
                case SharpDX.DXGI.Format.R8G8_B8G8_UNorm:
                case SharpDX.DXGI.Format.G8R8_G8B8_UNorm:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case SharpDX.DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGR101010;
                case SharpDX.DXGI.Format.B8G8R8A8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case SharpDX.DXGI.Format.R8G8_Typeless:
                case SharpDX.DXGI.Format.R8G8_UNorm:
                case SharpDX.DXGI.Format.R8G8_UInt:
                case SharpDX.DXGI.Format.R8G8_SNorm:
                case SharpDX.DXGI.Format.R8G8_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R16_Typeless:
                case SharpDX.DXGI.Format.R16_Float:
                case SharpDX.DXGI.Format.D16_UNorm:
                case SharpDX.DXGI.Format.R16_UNorm:
                case SharpDX.DXGI.Format.R16_SNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayHalf;
                case SharpDX.DXGI.Format.R16_UInt:
                case SharpDX.DXGI.Format.R16_SInt:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayFixedPoint;
                case SharpDX.DXGI.Format.B5G6R5_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGR565;
                case SharpDX.DXGI.Format.B5G5R5A1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGRA5551;
                case SharpDX.DXGI.Format.B4G4R4A4_UNorm:
                    return Guid.Empty;

                case SharpDX.DXGI.Format.R8_Typeless:
                case SharpDX.DXGI.Format.R8_UNorm:
                case SharpDX.DXGI.Format.R8_UInt:
                case SharpDX.DXGI.Format.R8_SNorm:
                case SharpDX.DXGI.Format.R8_SInt:
                    return SharpDX.WIC.PixelFormat.Format8bppGray;
                case SharpDX.DXGI.Format.A8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format8bppAlpha;
                case SharpDX.DXGI.Format.R1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format1bppIndexed;

                default:
                    return Guid.Empty;
            }
        }

        static int BitsPerPixel(SharpDX.DXGI.Format fmt)
        {
            switch (fmt)
            {
                case SharpDX.DXGI.Format.R32G32B32A32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                case SharpDX.DXGI.Format.R32G32B32A32_UInt:
                case SharpDX.DXGI.Format.R32G32B32A32_SInt:
                    return 128;

                case SharpDX.DXGI.Format.R32G32B32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32_Float:
                case SharpDX.DXGI.Format.R32G32B32_UInt:
                case SharpDX.DXGI.Format.R32G32B32_SInt:
                    return 96;

                case SharpDX.DXGI.Format.R16G16B16A16_Typeless:
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_UInt:
                case SharpDX.DXGI.Format.R16G16B16A16_SNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_SInt:
                case SharpDX.DXGI.Format.R32G32_Typeless:
                case SharpDX.DXGI.Format.R32G32_Float:
                case SharpDX.DXGI.Format.R32G32_UInt:
                case SharpDX.DXGI.Format.R32G32_SInt:
                case SharpDX.DXGI.Format.R32G8X24_Typeless:
                case SharpDX.DXGI.Format.D32_Float_S8X24_UInt:
                case SharpDX.DXGI.Format.R32_Float_X8X24_Typeless:
                case SharpDX.DXGI.Format.X32_Typeless_G8X24_UInt:
                    return 64;

                case SharpDX.DXGI.Format.R10G10B10A2_Typeless:
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                case SharpDX.DXGI.Format.R10G10B10A2_UInt:
                case SharpDX.DXGI.Format.R11G11B10_Float:
                case SharpDX.DXGI.Format.R8G8B8A8_Typeless:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                case SharpDX.DXGI.Format.R8G8B8A8_SNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_SInt:
                case SharpDX.DXGI.Format.R16G16_Typeless:
                case SharpDX.DXGI.Format.R16G16_Float:
                case SharpDX.DXGI.Format.R16G16_UNorm:
                case SharpDX.DXGI.Format.R16G16_UInt:
                case SharpDX.DXGI.Format.R16G16_SNorm:
                case SharpDX.DXGI.Format.R16G16_SInt:
                case SharpDX.DXGI.Format.R32_Typeless:
                case SharpDX.DXGI.Format.D32_Float:
                case SharpDX.DXGI.Format.R32_Float:
                case SharpDX.DXGI.Format.R32_UInt:
                case SharpDX.DXGI.Format.R32_SInt:
                case SharpDX.DXGI.Format.R24G8_Typeless:
                case SharpDX.DXGI.Format.D24_UNorm_S8_UInt:
                case SharpDX.DXGI.Format.R24_UNorm_X8_Typeless:
                case SharpDX.DXGI.Format.X24_Typeless_G8_UInt:
                case SharpDX.DXGI.Format.R9G9B9E5_Sharedexp:
                case SharpDX.DXGI.Format.R8G8_B8G8_UNorm:
                case SharpDX.DXGI.Format.G8R8_G8B8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                case SharpDX.DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm:
                case SharpDX.DXGI.Format.B8G8R8A8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                    return 32;

                case SharpDX.DXGI.Format.R8G8_Typeless:
                case SharpDX.DXGI.Format.R8G8_UNorm:
                case SharpDX.DXGI.Format.R8G8_UInt:
                case SharpDX.DXGI.Format.R8G8_SNorm:
                case SharpDX.DXGI.Format.R8G8_SInt:
                case SharpDX.DXGI.Format.R16_Typeless:
                case SharpDX.DXGI.Format.R16_Float:
                case SharpDX.DXGI.Format.D16_UNorm:
                case SharpDX.DXGI.Format.R16_UNorm:
                case SharpDX.DXGI.Format.R16_UInt:
                case SharpDX.DXGI.Format.R16_SNorm:
                case SharpDX.DXGI.Format.R16_SInt:
                case SharpDX.DXGI.Format.B5G6R5_UNorm:
                case SharpDX.DXGI.Format.B5G5R5A1_UNorm:
                case SharpDX.DXGI.Format.B4G4R4A4_UNorm:
                    return 16;

                case SharpDX.DXGI.Format.R8_Typeless:
                case SharpDX.DXGI.Format.R8_UNorm:
                case SharpDX.DXGI.Format.R8_UInt:
                case SharpDX.DXGI.Format.R8_SNorm:
                case SharpDX.DXGI.Format.R8_SInt:
                case SharpDX.DXGI.Format.A8_UNorm:
                    return 8;

                case SharpDX.DXGI.Format.R1_UNorm:
                    return 1;

                case SharpDX.DXGI.Format.BC1_Typeless:
                case SharpDX.DXGI.Format.BC1_UNorm:
                case SharpDX.DXGI.Format.BC1_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC4_Typeless:
                case SharpDX.DXGI.Format.BC4_UNorm:
                case SharpDX.DXGI.Format.BC4_SNorm:
                    return 4;

                case SharpDX.DXGI.Format.BC2_Typeless:
                case SharpDX.DXGI.Format.BC2_UNorm:
                case SharpDX.DXGI.Format.BC2_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC3_Typeless:
                case SharpDX.DXGI.Format.BC3_UNorm:
                case SharpDX.DXGI.Format.BC3_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC5_Typeless:
                case SharpDX.DXGI.Format.BC5_UNorm:
                case SharpDX.DXGI.Format.BC5_SNorm:
                case SharpDX.DXGI.Format.BC6H_Typeless:
                case SharpDX.DXGI.Format.BC6H_Uf16:
                case SharpDX.DXGI.Format.BC6H_Sf16:
                case SharpDX.DXGI.Format.BC7_Typeless:
                case SharpDX.DXGI.Format.BC7_UNorm:
                case SharpDX.DXGI.Format.BC7_UNorm_SRgb:
                    return 8;

                default:
                    return 0;
            }
        }
    }
}
