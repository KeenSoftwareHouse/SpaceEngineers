using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Resource = SharpDX.Direct3D11.Resource;
using DataBox = SharpDX.DataBox;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Color = SharpDX.Color;
using VRage.Library.Utils;
using System.Diagnostics;
using VRageRender.Messages;

namespace VRage.Render11.Resources
{
    internal interface IGeneratedTexture : ITexture
    {
    }

    internal interface IUserGeneratedTexture : IGeneratedTexture
    {
        void Reset(byte[] data = null);
        MyGeneratedTextureType Type { get; }
    }

    namespace Internal
    {
        internal class MyGeneratedTexture : IGeneratedTexture
        {
            string m_name;
            Vector2I m_size;
            Resource m_resource;
            ShaderResourceView m_srv;
            Texture2DDescription m_desc;
            int m_bytes;

            internal void Init(string name, Texture2DDescription desc, Vector2I size, int bytes)
            {
                m_name = name;
                m_size = size;
                m_desc = desc;
                m_bytes = bytes;
            }

            protected internal void Reset(DataBox[] dataBoxes)
            {
                Dispose();

                if (dataBoxes == null)
                    m_resource = new Texture2D(MyRender11.Device, m_desc);
                else
                    m_resource = new Texture2D(MyRender11.Device, m_desc, dataBoxes);
                m_resource.DebugName = m_name;
                m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                m_srv.DebugName = m_name;
            }

            protected internal void Dispose()
            {
                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }

                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
            }

            public MyGeneratedTextureType Type
            {
                get { return m_desc.Format.ToGeneratedTextureType(); }
            }

            public int MipmapCount
            {
                get { return m_desc.MipLevels; }
            }

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public string Name
            {
                get { return m_name; }
            }

            public Format Format
            {
                get { return m_desc.Format; }
            }

            public Vector2I Size
            {
                get { return m_size; }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_size.X, m_size.Y, 1); }
            }
        }

        class MyGeneratedTextureFromPattern : IGeneratedTexture
        {
            string m_name;
            Vector2I m_size;
            Resource m_resource;
            ShaderResourceView m_srv;
            Texture2DDescription m_desc;

            byte[] CreateTextureDataByPattern(Vector2I strides, byte[] pattern4x4)
            {
                MyRenderProxy.Assert(strides.X % 4 == 0);
                MyRenderProxy.Assert(strides.Y % 4 == 0);
                int blocksCount = 0;
                for (int i = 0; i < MyResourceUtils.GetMipmapsCount(Math.Max(strides.X, strides.Y)); i++)
                {
                    Vector2I blocksInMipmap;
                    blocksInMipmap.X = MyResourceUtils.GetMipmapStride(strides.X, i);
                    blocksInMipmap.Y = MyResourceUtils.GetMipmapStride(strides.Y, i);
                    blocksCount += blocksInMipmap.X * blocksInMipmap.Y / 16;
                }

                byte[] data = new byte[blocksCount * pattern4x4.Length];
                for (int i = 0; i < blocksCount; i++)
                {
                    Array.Copy(pattern4x4, 0, data, pattern4x4.Length*i, pattern4x4.Length);
                }
                return data;
            }

            unsafe void InitDxObjects(string name, Vector2I size, Format format, byte[] bytePattern)
            {
                Texture2DDescription desc = new Texture2DDescription();
                desc.Format = format;
                desc.ArraySize = 1;
                desc.Height = size.Y;
                desc.Width = size.X;
                desc.MipLevels = MyResourceUtils.GetMipmapsCount(Math.Max(size.X, size.Y));
                desc.BindFlags = BindFlags.ShaderResource;
                desc.CpuAccessFlags = CpuAccessFlags.None;
                desc.OptionFlags = ResourceOptionFlags.None;
                desc.Usage = ResourceUsage.Immutable;
                desc.SampleDescription = new SampleDescription(1, 0);

                Vector2I strides0;
                strides0.X = MyResourceUtils.GetMipmapStride(desc.Width, 0);
                strides0.Y = MyResourceUtils.GetMipmapStride(desc.Height, 0);
                byte[] dataBoxData = CreateTextureDataByPattern(strides0, bytePattern);
                DataBox[] dataBoxes = new DataBox[desc.MipLevels];
                int blocksOffset = 0;
                fixed (void* ptr = dataBoxData)
                {
                    for (int i = 0; i < dataBoxes.Length; i++)
                    {
                        dataBoxes[i].SlicePitch = 0;
                        dataBoxes[i].RowPitch = MyResourceUtils.GetMipmapStride(size.X, i) * bytePattern.Length / 16;
                        dataBoxes[i].DataPointer = new IntPtr(((byte*)ptr) + blocksOffset * bytePattern.Length);

                        Vector2I blocksInMipmap;
                        blocksInMipmap.X = MyResourceUtils.GetMipmapStride(size.X, i) / 4;
                        blocksInMipmap.Y = MyResourceUtils.GetMipmapStride(size.Y, i) / 4;
                        blocksOffset += blocksInMipmap.X * blocksInMipmap.Y;
                    }
                    m_resource = new Texture2D(MyRender11.Device, desc, dataBoxes);
                    m_resource.DebugName = name;
                }

                m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                m_srv.DebugName = name;
            }

            internal void Init(string name, Vector2I size, Format format, byte[] bytePattern)
            {
                m_name = name;
                m_size = size;

                InitDxObjects(name, size, format, bytePattern);
            }

            protected internal void Reset(DataBox[] dataBoxes)
            {
                Dispose();

                m_resource = new Texture2D(MyRender11.Device, m_desc, dataBoxes);
                m_resource.DebugName = m_name;
                m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                m_srv.DebugName = m_name;
            }

            protected internal void Dispose()
            {
                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }

                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
            }

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public string Name
            {
                get { return m_name; }
            }

            public Format Format
            {
                get { return m_desc.Format; }
            }

            public Vector2I Size
            {
                get { return m_size; }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_size.X, m_size.Y, 1); }
            }

            public int MipmapCount
            {
                get { return m_desc.MipLevels; }
            }
        }

        internal class MyUserGeneratedTexture : MyGeneratedTexture, IUserGeneratedTexture
        {
            public void Reset(byte[] data)
            {
                MyGeneratedTextureManager.ResetUserTexture(this, data);
            }
        }

        internal class MySwitchableDebugTexture : IGeneratedTexture
        {
            string m_name;
            IGeneratedTexture m_releaseTexture;
            IGeneratedTexture m_debugTexture;

            IGeneratedTexture GetTexture()
            {
                if (MyRender11.Settings.UseDebugMissingFileTextures)
                    return m_debugTexture;
                else
                    return m_releaseTexture;
            }

            public ShaderResourceView Srv
            {
                get { return GetTexture().Srv; }
            }

            public Resource Resource
            {
                get { return GetTexture().Resource; }
            }

            public string Name
            {
                get { return GetTexture().Name; }
            }

            public Vector2I Size
            {
                get { return GetTexture().Size; }
            }

            public Vector3I Size3
            {
                get { return GetTexture().Size3; }
            }

            public int MipmapCount
            {
                get { return GetTexture().MipmapCount; }
            }

            public Format Format { get { return GetTexture().Format; } }

            public void Init(IGeneratedTexture releaseTex, IGeneratedTexture debugTex, string name)
            {
                m_name = name;
                m_releaseTexture = releaseTex;
                m_debugTexture = debugTex;
            }
        }
    }

    class MyGeneratedTextureManager : IManager, IManagerDevice
    {
        public static IGeneratedTexture ZeroTex = new MyGeneratedTexture();
        public static IGeneratedTexture ReleaseMissingNormalGlossTex = new MyGeneratedTexture();
        public static IGeneratedTexture ReleaseMissingAlphamaskTex = new MyGeneratedTexture();
        public static IGeneratedTexture ReleaseMissingExtensionTex = new MyGeneratedTexture();
        public static IGeneratedTexture ReleaseMissingCubeTex = new MyGeneratedTexture();
        public static IGeneratedTexture PinkTex = new MyGeneratedTexture();
        public static IGeneratedTexture DebugMissingNormalGlossTex = new MyGeneratedTexture();
        public static IGeneratedTexture DebugMissingAlphamaskTex = new MyGeneratedTexture();
        public static IGeneratedTexture DebugMissingExtensionTex = new MyGeneratedTexture();
        public static IGeneratedTexture DebugMissingCubeTex = new MyGeneratedTexture();

        static MySwitchableDebugTexture m_missingNormalGlossTex = new MySwitchableDebugTexture();
        static MySwitchableDebugTexture m_missingAlphamaskTex = new MySwitchableDebugTexture();
        static MySwitchableDebugTexture m_missingExtensionTex = new MySwitchableDebugTexture();
        static MySwitchableDebugTexture m_missingCubeTex = new MySwitchableDebugTexture();

        public static IGeneratedTexture MissingNormalGlossTex { get { return m_missingNormalGlossTex; } }
        public static IGeneratedTexture MissingAlphamaskTex { get { return m_missingAlphamaskTex; } }
        public static IGeneratedTexture MissingExtensionTex { get { return m_missingExtensionTex; } }
        public static IGeneratedTexture MissingCubeTex { get { return m_missingCubeTex; } }

        public static IGeneratedTexture IntelFallbackCubeTex = new MyGeneratedTexture();
        public static IGeneratedTexture Dithering8x8Tex = new MyGeneratedTexture();
        public static IGeneratedTexture RandomTex = new MyGeneratedTexture();

        static DataBox[] m_tmpDataBoxArray1 = new DataBox[1];
        static DataBox[] m_tmpDataBoxArray6 = new DataBox[6];

        static readonly Texture2DDescription m_descDefault = new Texture2DDescription
        {
            ArraySize = 1,
            BindFlags = BindFlags.ShaderResource,
            Format = Format.R8G8B8A8_UNorm,
            Height = 0, // This value needs to be modified!
            Width = 0, // This value needs to be modified!
            Usage = ResourceUsage.Immutable,
            MipLevels = 1,
            SampleDescription = new SampleDescription
            {
                Count = 1,
                Quality = 0,
            }
        };

        MyTextureStatistics m_statistics = new MyTextureStatistics();
        MyObjectsPool<MyUserGeneratedTexture> m_objectsPoolGenerated = new MyObjectsPool<MyUserGeneratedTexture>(16);
        MyObjectsPool<MyGeneratedTextureFromPattern> m_objectsPoolGeneratedFromPattern = new MyObjectsPool<MyGeneratedTextureFromPattern>(16);

        public IUserGeneratedTexture NewUserTexture(string name, int width, int height, MyGeneratedTextureType type, int numMipLevels)
        {
            MyUserGeneratedTexture texture;
            m_objectsPoolGenerated.AllocateOrCreate(out texture);
            switch (type)
            {
                case MyGeneratedTextureType.RGBA:
                    CreateRGBA(texture, name, new Vector2I(width, height), true, (byte[])null, true, numMipLevels);
                    break;
                case MyGeneratedTextureType.RGBA_Linear:
                    CreateRGBA(texture, name, new Vector2I(width, height), false, (byte[])null, true, numMipLevels);
                    break;
                case MyGeneratedTextureType.Alphamask:
                    CreateR(texture, name, new Vector2I(width, height), null, true, numMipLevels);
                    break;
                default:
                    throw new Exception();
            }
            m_statistics.Add(texture);
            return texture;
        }

        public IGeneratedTexture CreateFromBytePattern(string name, int width, int height, Format format, byte[] pattern)
        {
            MyGeneratedTextureFromPattern generated;
            m_objectsPoolGeneratedFromPattern.AllocateOrCreate(out generated);
            generated.Init(name, new Vector2I(width, height), format, pattern);
            m_statistics.Add(generated);
            return generated;
        }

        internal static void ResetUserTexture(MyUserGeneratedTexture texture, byte[] data)
        {
            switch (texture.Format)
            {
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                    Reset(texture, data, 4);
                    break;
                case Format.R8_UNorm:
                    Reset(texture, data, 1);
                    break;
                default:
                    throw new Exception();
            }
        }

        void CreateR_1x1(MyGeneratedTexture tex, string name, byte data)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            tex.Init(name, desc, new Vector2I(1, 1), 1);
            Reset(tex, data);
        }

        static unsafe void Reset(MyGeneratedTexture tex, byte data)
        {
            void* ptr = &data;
            m_tmpDataBoxArray1[0].DataPointer = new IntPtr(ptr);
            m_tmpDataBoxArray1[0].RowPitch = 1;
            tex.Reset(m_tmpDataBoxArray1);
        }

        void CreateRGBA_1x1(MyGeneratedTexture tex, string name, Color color)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            tex.Init(name, desc, new Vector2I(1, 1), 4);
            int data = color.ToRgba();
            Reset(tex, data);
        }

        static unsafe void Reset(MyGeneratedTexture tex, int data)
        {
            void* ptr = &data;
            m_tmpDataBoxArray1[0].DataPointer = new IntPtr(ptr);
            m_tmpDataBoxArray1[0].RowPitch = 4;
            tex.Reset(m_tmpDataBoxArray1);
        }

        void CreateCubeRGBA_1x1(MyGeneratedTexture tex, string name, Color color)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            desc.ArraySize = 6;
            desc.OptionFlags = ResourceOptionFlags.TextureCube;
            tex.Init(name, desc, new Vector2I(1, 1), 4);
            int data = color.ToRgba();
            ResetCube(tex, data);
        }

        static unsafe void ResetCube(MyGeneratedTexture tex, int data)
        {
            void* ptr = &data;
            for (int i = 0; i < 6; i++)
            {
                m_tmpDataBoxArray6[i].DataPointer = new IntPtr(ptr);
                m_tmpDataBoxArray6[i].RowPitch = 4;
            }
            tex.Reset(m_tmpDataBoxArray6);
        }

        void CreateR(MyGeneratedTexture tex, string name, Vector2I resolution, byte[] data, bool userTexture = false, int numMipLebels = 1)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Usage = userTexture ? ResourceUsage.Default : ResourceUsage.Immutable;
            desc.Format = Format.R8_UNorm;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Height = height;
            desc.Width = width;
            desc.MipLevels = numMipLebels;
            tex.Init(name, desc, new Vector2I(width, height), width * height);
            if (data != null)
                Reset(tex, data, 1);
        }

        static unsafe void Reset(MyGeneratedTexture tex, byte[] data, int nchannels)
        {
            if (data == null)
            {
                tex.Reset(null);
            }
            else
            {
                fixed (byte* dptr = data)
                {
                    int numMiplevels = tex.MipmapCount;
                    DataBox[] dataBox = new DataBox[numMiplevels];

                    int width = tex.Size.X;
                    int height = tex.Size.Y;

                    int offset = 0;
                    for (int i = 0; i < numMiplevels; ++i)
                    {
                        dataBox[i].DataPointer = new IntPtr(dptr + offset);
                        dataBox[i].RowPitch = width * nchannels;
                        offset += width * height * nchannels;

                        width >>= 1;
                        height >>= 1;
                    }
                    tex.Reset(dataBox);
                }
            }
        }

        unsafe void CreateRGBA(MyGeneratedTexture tex, string name, Vector2I resolution, bool srgb, Color[] colors, int numMipLevels = 1)
        {
            int width = resolution.X;
            int height = resolution.Y;
            byte[] rawData = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = x + y * width;
                    Color currentColor = colors[offset];
                    rawData[offset * 4 + 0] = currentColor.R;
                    rawData[offset * 4 + 1] = currentColor.G;
                    rawData[offset * 4 + 2] = currentColor.B;
                    rawData[offset * 4 + 3] = currentColor.A;
                }
            }
            CreateRGBA(tex, name, resolution, srgb, rawData);
        }

        void CreateRGBA(MyGeneratedTexture tex, string name, Vector2I resolution, bool srgb, byte[] data, bool userTexture = false, int numMipLevels = 1)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Usage = userTexture ? ResourceUsage.Default : ResourceUsage.Immutable;
            desc.Format = srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Width = width;
            desc.Height = height;
            desc.MipLevels = numMipLevels;
            tex.Init(name, desc, new Vector2I(width, height), width * height * 4);
            if (data != null)
            {
                Debug.Assert(width * height * 4 == data.Length, "Wrong data array size for RGBA texture");
                Reset(tex, data, 4);
            }
        }

        void CreateR32G32B32A32_Float(MyGeneratedTexture tex, string name, Vector2I resolution, Vector4[] colors)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R32G32B32A32_Float;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Height = width;
            desc.Width = height;
            tex.Init(name, desc, new Vector2I(width, height), width * height * 16);

            if (colors != null)
            {
                float[] values = new float[width * height * 4];

                int inOffset = 0;
                int outOffset = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        values[outOffset++] = colors[inOffset].X;
                        values[outOffset++] = colors[inOffset].Y;
                        values[outOffset++] = colors[inOffset].Z;
                        values[outOffset++] = colors[inOffset].W;
                        inOffset++;
                    }
                }

                Reset(tex, values, width, 4);
            }
        }

        static unsafe void Reset(MyGeneratedTexture tex, float[] data, int rowlength, int nchannels)
        {
            fixed (float* dptr = data)
            {
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(dptr);
                m_tmpDataBoxArray1[0].RowPitch = rowlength * nchannels * 4;
                tex.Reset(m_tmpDataBoxArray1);
            }
        }

        void CreateCheckerR(MyGeneratedTexture tex, string name, Vector2I resolution, byte v1, byte v2)
        {
            int width = resolution.X;
            int height = resolution.Y;
            byte[] ditherData = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < height; x++)
                {
                    byte v = v1;
                    if (((y + x) & 1) == 0)
                        v = v2;
                    ditherData[y * width + x] = v;
                }
            }
            CreateR(tex, name, resolution, ditherData);
        }

        void CreateCheckerRGBA(MyGeneratedTexture tex, string name, Vector2I resolution, Color v1, Color v2)
        {
            int width = resolution.X;
            int height = resolution.Y;
            Color[] rawData = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color v = v1;
                    if (((y + x) & 1) == 0)
                        v = v2;
                    rawData[y * width + x] = v;
                }
        }
            CreateRGBA(tex, name, resolution, false, rawData);
        }

        void CreateAllTextures()
        {
            CreateRGBA_1x1((MyGeneratedTexture)ZeroTex, "EMPTY", new Color(0, 0, 0, 0));
            CreateRGBA_1x1((MyGeneratedTexture)PinkTex, "PINK", new Color(255, 0, 255));
            CreateRGBA_1x1((MyGeneratedTexture)ReleaseMissingNormalGlossTex, "ReleaseMissingNormalGloss", new Color(127, 127, 255, 0));
            CreateR_1x1((MyGeneratedTexture)ReleaseMissingAlphamaskTex, "ReleaseMissingAlphamask", 255);
            CreateRGBA_1x1((MyGeneratedTexture)ReleaseMissingExtensionTex, "ReleaseMissingExtension", new Color(255, 0, 0, 0));
            CreateCubeRGBA_1x1((MyGeneratedTexture)ReleaseMissingCubeTex, "ReleaseMissingCube", new Color(255, 0, 255, 0));
            CreateCheckerRGBA((MyGeneratedTexture)DebugMissingNormalGlossTex, "DebugMissingNormalGloss", new Vector2I(8, 8), new Color(91, 0, 217, 0), new Color(217, 0, 217, 255));
            CreateCheckerR((MyGeneratedTexture)DebugMissingAlphamaskTex, "DebugMissingAlphamask", new Vector2I(8, 8), 255, 0);
            CreateCheckerRGBA((MyGeneratedTexture)DebugMissingExtensionTex, "DebugMissingExtension", new Vector2I(8, 8), new Color(255, 255, 0, 0), new Color(0, 0, 0, 0));
            CreateCubeRGBA_1x1((MyGeneratedTexture)DebugMissingCubeTex, "DebubMissingCube", new Color(255, 0, 255, 0));

            m_missingNormalGlossTex.Init(ReleaseMissingNormalGlossTex, DebugMissingNormalGlossTex, "MISSING_NORMAL_GLOSS");
            m_missingAlphamaskTex.Init(ReleaseMissingAlphamaskTex, DebugMissingAlphamaskTex, "MISSING_ALPHAMASK");
            m_missingExtensionTex.Init(ReleaseMissingExtensionTex, DebugMissingExtensionTex, "MISSING_EXTENSIONS");
            m_missingCubeTex.Init(ReleaseMissingCubeTex, DebugMissingCubeTex, "MISSING_CUBEMAP");

            CreateCubeRGBA_1x1((MyGeneratedTexture)IntelFallbackCubeTex, "INTEL_FALLBACK_CUBEMAP", new Color(10, 10, 10, 0));

            InitializeRandomTexture();

            byte[] ditherData = new byte[] 
                {
                    0,  32,  8, 40,  2, 34, 10, 42,
                    48, 16, 56, 24, 50, 18, 58, 26,
                    12, 44,  4, 36, 14, 46,  6, 38, 
                    60, 28, 52, 20, 62, 30, 54, 22,
                     3, 35, 11, 43,  1, 33,  9, 41,
                    51, 19, 59, 27, 49, 17, 57, 25,
                    15, 47,  7, 39, 13, 45, 5,  37,
                    63, 31, 55, 23, 61, 29, 53, 21 };
            for (int i = 0; i < 64; i++)
                ditherData[i] = (byte)(ditherData[i] * 4);
            CreateR((MyGeneratedTexture)Dithering8x8Tex, "DITHER_8x8", new Vector2I(8, 8), ditherData);
        }

        public void InitializeRandomTexture(int? seed = null)
        {
            MyRandom random;
            if (seed.HasValue)
                random = new MyRandom(seed.Value);
            else
                random = new MyRandom();

            int randowTexRes = 1024;
            Vector4[] randomValues = new Vector4[randowTexRes * randowTexRes * 4];
            for (uint i = 0, ctr = 0; i < randowTexRes * randowTexRes; i++)
            {
                randomValues[ctr++] = new Vector4(random.NextFloat() * 2.0f - 1.0f,
                    random.NextFloat() * 2.0f - 1.0f,
                    random.NextFloat() * 2.0f - 1.0f,
                    random.NextFloat() * 2.0f - 1.0f);
            }
            CreateR32G32B32A32_Float((MyGeneratedTexture)RandomTex, "RANDOM", new Vector2I(randowTexRes, randowTexRes), randomValues);
        }

        public void DisposeTex(IGeneratedTexture tex)
        {
            if (tex == null)
                return;

            if (tex is MyUserGeneratedTexture)
            {
                MyUserGeneratedTexture texture = (MyUserGeneratedTexture) tex;
                texture.Dispose();
                m_objectsPoolGenerated.Deallocate(texture);
                m_statistics.Remove(texture);
                tex = null;
            }
            else if (tex is MyGeneratedTextureFromPattern)
            {
                MyGeneratedTextureFromPattern texture = (MyGeneratedTextureFromPattern)tex;
                texture.Dispose();
                m_objectsPoolGeneratedFromPattern.Deallocate(texture);
                m_statistics.Remove(texture);
                tex = null;
            }
            else
            {
                MyRenderProxy.Assert(false, "It is disposed texture that does not belong to this manager");
            }
        }

        public int ActiveTexturesCount
        {
            get { return m_objectsPoolGenerated.ActiveCount + m_objectsPoolGeneratedFromPattern.ActiveCount; }
        }

        public void OnDeviceInit()
        {
            CreateAllTextures();
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }

        public void OnDeviceEnd()
        {
            foreach (var tex in m_objectsPoolGenerated.Active)
                tex.Dispose();
        }

        public MyTextureStatistics Statistics
        {
            get { return m_statistics; }
        }
    }
}