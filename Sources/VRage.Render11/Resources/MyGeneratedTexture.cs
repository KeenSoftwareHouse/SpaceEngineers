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

namespace VRage.Render11.Resources
{
    internal interface IGeneratedTexture : ISrvBindable
    {
        
    }

    namespace Internal
    {
        internal class MyGeneratedTexture : IGeneratedTexture
        {
            string m_name;
            Vector2I m_size;
            Resource m_resource;
            ShaderResourceView m_srv;
            private Texture2DDescription m_desc;
            private int m_bytes;

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

            public Vector2I Size
            {
                get { return m_size; }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_size.X, m_size.Y, 1); }
            }

            public void Init(string name, Texture2DDescription desc, DataBox[] dataBoxes, Vector2I size, int bytes,
                bool enableDxInitialisation)
            {
                Clear();

                m_name = name;
                m_size = size;
                m_desc = desc;
                m_bytes = bytes;

                if (enableDxInitialisation)
                {
                    m_resource = new Texture2D(MyRender11.Device, m_desc, dataBoxes);
                    m_resource.DebugName = name;
                    m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                    m_srv.DebugName = name;
                }
            }

            public void Clear()
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
        }

        internal class MySwitchableDebugTexture : IGeneratedTexture
        {
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

            public void Init(IGeneratedTexture releaseTex, IGeneratedTexture debugTex)
            {
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
        public static IGeneratedTexture MissingCubeTex { get { return m_missingCubeTex; }}

        public static IGeneratedTexture IntelFallbackCubeTex = new MyGeneratedTexture();
        public static IGeneratedTexture Dithering8x8Tex = new MyGeneratedTexture();
        public static IGeneratedTexture RandomTex = new MyGeneratedTexture();

        MyObjectsPool<MyGeneratedTexture> m_objectsPoolGenerated = new MyObjectsPool<MyGeneratedTexture>(16);
        MyObjectsPool<MySwitchableDebugTexture> m_objectsPoolSwitchable = new MyObjectsPool<MySwitchableDebugTexture>(8);

        DataBox[] m_tmpDataBoxArray1 = new DataBox[1];
        DataBox[] m_tmpDataBoxArray6 = new DataBox[6];

        Texture2DDescription m_descDefault = new Texture2DDescription
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

        void CreateTexture(MyGeneratedTexture tex, string name, Texture2DDescription desc, DataBox[] dataBoxes, Vector2I size, int bytes, bool enableDxInitialisation = true)
        {
            tex.Init(name, desc, dataBoxes, size, bytes, enableDxInitialisation);
        }

        unsafe void CreateR_1x1(MyGeneratedTexture tex, string name, byte data)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            void* ptr = &data;
            m_tmpDataBoxArray1[0].DataPointer = new IntPtr(ptr);
            m_tmpDataBoxArray1[0].RowPitch = 1;
            CreateTexture(tex, name, desc, m_tmpDataBoxArray1, new Vector2I(1, 1), 1);
        }

        unsafe void CreateRGBA_1x1(MyGeneratedTexture tex, string name, Color color)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            int data = color.ToRgba();
            void* ptr = &data;
            m_tmpDataBoxArray1[0].DataPointer = new IntPtr(ptr);
            m_tmpDataBoxArray1[0].RowPitch = 4;
            CreateTexture(tex, name, desc, m_tmpDataBoxArray1, new Vector2I(1, 1), 4);
        }

        unsafe void CreateCubeRGBA_1x1(MyGeneratedTexture tex, string name, Color color)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.Height = 1;
            desc.Width = 1;
            desc.ArraySize = 6;
            desc.OptionFlags = ResourceOptionFlags.TextureCube;
            int data = color.ToRgba();
            void* ptr = &data;
            for (int i = 0; i < 6; i++)
            {
                m_tmpDataBoxArray6[i].DataPointer = new IntPtr(ptr);
                m_tmpDataBoxArray6[i].RowPitch = 4;
            }

            CreateTexture(tex, name, desc, m_tmpDataBoxArray6, new Vector2I(1, 1), 4);
        }

        unsafe void CreateR(MyGeneratedTexture tex, string name, Vector2I resolution, byte[] data)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8_UNorm;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Height = height;
            desc.Width = width;
            fixed (byte* dptr = data)
            {
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(dptr);
                m_tmpDataBoxArray1[0].RowPitch = width;
                CreateTexture(tex, name, desc, m_tmpDataBoxArray1, new Vector2I(width, height), width * height);
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(null);
            }
        }

        unsafe void CreateRGBA(MyGeneratedTexture tex, string name, Vector2I resolution, Color[] colors)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R8G8B8A8_UNorm;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Height = width;
            desc.Width = height;
            byte[] rawData = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = x + y*width;
                    Color currentColor = colors[offset];
                    rawData[offset*4 + 0] = currentColor.R;
                    rawData[offset*4 + 1] = currentColor.G;
                    rawData[offset*4 + 2] = currentColor.B;
                    rawData[offset*4 + 3] = currentColor.A;
                }
            fixed (byte* dptr = rawData)
            {
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(dptr);
                m_tmpDataBoxArray1[0].RowPitch = width*4;
                CreateTexture(tex, name, desc, m_tmpDataBoxArray1, new Vector2I(width, height), width*height*4);
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(null);
            }
        }

        unsafe void CreateR32G32B32A32_Float(MyGeneratedTexture tex, string name, Vector2I resolution, Vector4[] colors)
        {
            Texture2DDescription desc = m_descDefault;
            desc.Format = Format.R32G32B32A32_Float;
            int width = resolution.X;
            int height = resolution.Y;
            desc.Height = width;
            desc.Width = height;
            float[] values = new float[width * height * 4];
            
            int inOffset = 0;
            int outOffset = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    values[outOffset++] = colors[inOffset].X;
                    values[outOffset++] = colors[inOffset].Y;
                    values[outOffset++] = colors[inOffset].Z;
                    values[outOffset++] = colors[inOffset].W;
                    inOffset++;
                }
            fixed (float* dptr = values)
            {
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(dptr);
                m_tmpDataBoxArray1[0].RowPitch = width * 16;
                CreateTexture(tex, name, desc, m_tmpDataBoxArray1, new Vector2I(width, height), width * height * 16);
                m_tmpDataBoxArray1[0].DataPointer = new IntPtr(null);
            }
        }

        unsafe void CreateCheckerR(MyGeneratedTexture tex, string name, Vector2I resolution, byte v1, byte v2)
        {
            int width = resolution.X;
            int height = resolution.Y;
            byte[] ditherData = new byte[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < height; x++)
                {
                    byte v = v1;
                    if (((y + x) & 1) == 0)
                        v = v2;
                    ditherData[y*width + x] = v;
                }
            CreateR(tex, name, resolution, ditherData);
        }

        unsafe void CreateCheckerRGBA(MyGeneratedTexture tex, string name, Vector2I resolution, Color v1, Color v2)
        {
            int width = resolution.X;
            int height = resolution.Y;
            Color[] rawData = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Color v = v1;
                    if (((y + x) & 1) == 0)
                        v = v2;
                    rawData[y * width + x] = v;
                }
            CreateRGBA(tex, name, resolution, rawData);
        }

        unsafe void CreateAllTextures()
        {
            CreateRGBA_1x1((MyGeneratedTexture)ZeroTex, "Zero", new Color(0, 0, 0, 0));
            CreateRGBA_1x1((MyGeneratedTexture)PinkTex, "Pink", new Color(255, 0, 255));
            CreateRGBA_1x1((MyGeneratedTexture)ReleaseMissingNormalGlossTex, "ReleaseMissingNormalGloss", new Color(127, 127, 255, 0));
            CreateR_1x1((MyGeneratedTexture)ReleaseMissingAlphamaskTex, "ReleaseMissingAlphamask", 255);
            CreateRGBA_1x1((MyGeneratedTexture)ReleaseMissingExtensionTex, "ReleaseMissingExtension", new Color(255, 0, 0, 0));
            CreateCubeRGBA_1x1((MyGeneratedTexture)ReleaseMissingCubeTex, "ReleaseMissingCube", new Color(0, 0, 0, 0));
            CreateCheckerRGBA((MyGeneratedTexture)DebugMissingNormalGlossTex, "DebugMissingNormalGloss", new Vector2I(8, 8), new Color(91, 0, 217, 0), new Color(217, 0, 217, 255));
            CreateCheckerR((MyGeneratedTexture)DebugMissingAlphamaskTex, "DebugMissingAlphamask", new Vector2I(8, 8), 255, 0);
            CreateCheckerRGBA((MyGeneratedTexture)DebugMissingExtensionTex, "DebugMissingExtension", new Vector2I(8, 8), new Color(255, 255, 0, 0), new Color(0, 0, 0, 0));
            CreateCubeRGBA_1x1((MyGeneratedTexture)DebugMissingCubeTex, "DebubMissingCube", new Color(255, 0, 255, 0));

            m_missingNormalGlossTex.Init(ReleaseMissingNormalGlossTex, DebugMissingNormalGlossTex);
            m_missingAlphamaskTex.Init(ReleaseMissingNormalGlossTex, DebugMissingNormalGlossTex);
            m_missingExtensionTex.Init(ReleaseMissingExtensionTex, DebugMissingExtensionTex);
            m_missingCubeTex.Init(ReleaseMissingCubeTex, DebugMissingCubeTex);

            CreateCubeRGBA_1x1((MyGeneratedTexture)IntelFallbackCubeTex, "IntelFallbackCubeTex", new Color(50, 0, 50, 0));

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
            CreateR((MyGeneratedTexture)Dithering8x8Tex, "Dither_8x8", new Vector2I(8, 8), ditherData);
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
            CreateR32G32B32A32_Float((MyGeneratedTexture)RandomTex, "RandomTex", new Vector2I(randowTexRes, randowTexRes), randomValues);
        }

        public int GetTexturesCount()
        {
            return m_objectsPoolGenerated.ActiveCount;
        }

        public void OnDeviceInit()
        {
            CreateAllTextures();
        }

        public void OnDeviceReset()
        {
        }

        public void OnDeviceEnd()
        {
            foreach (var tex in m_objectsPoolGenerated.Active)
                tex.Clear();  
        }
    }
}