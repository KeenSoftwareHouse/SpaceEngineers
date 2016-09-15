using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using SharpDX.DXGI;
using Resource = SharpDX.Direct3D11.Resource;
using DataBox = SharpDX.DataBox;
using SharpDXException = SharpDX.SharpDXException;
using VRage.FileSystem;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    [Flags]
    internal enum MyFileTextureEnum
    {
        UNSPECIFIED = 0,
        COLOR_METAL = 1,
        NORMALMAP_GLOSS = 2,
        EXTENSIONS = 4,
        ALPHAMASK = 8,
        GUI = 16,
        CUBEMAP = 32,
        SYSTEM = 64,
        CUSTOM = 128,
        GPUPARTICLES = 256
    }

    internal struct MyFileTextureUsageReport
    {
        public int TexturesTotal;
        public int TexturesLoaded;
        public long TotalTextureMemory;
    }

    internal interface IFileTexture : ISrvBindable
    { }

    namespace Internal
    {
        enum FileTextureState
        {
            Unloaded,
            Requested,
            Loaded,
        }

        internal class MyFileTexture : IFileTexture
        {
            // immutable data   
            string m_name;
            Vector2I m_size;
            MyFileTextureEnum m_type;
            bool m_ownsData;
            bool m_skipQualityReduction;
            Format m_imageFormatInFile = Format.Unknown;

            // data status
            internal FileTextureState TextureState;
            bool m_fileExists;
            int m_byteSize;

            ShaderResourceView m_srv;
            Resource m_resource;

            public ShaderResourceView Srv
            {
                get
                {
                    DoLoading();
                    return m_srv;
                }
            }

            public Resource Resource
            {
                get
                {
                    DoLoading();
                    return m_resource;
                }
            }

            public string Name
            {
                get { return m_name; }
            }

            public Vector3I Size3
            {
                get
                {
                    DoLoading();
                    return new Vector3I(m_size.X, m_size.Y, 1);
                }
            }

            public Vector2I Size
            {
                get
                {
                    DoLoading();
                    return m_size;
                }
            }

            public MyFileTextureEnum TextureType
            {
                get { return m_type; }
            }

            public long ByteSize
            {
                get
                {
                    DoLoading();
                    return m_byteSize;
                }
            }

            public Format ImageFormatInFile
            {
                get
                {
                    DoLoading();
                    return m_imageFormatInFile;
                }
            }

            public void Init(string name, MyFileTextureEnum type, bool waitTillLoaded, bool skipQualityReduction)
            {
                m_name = name;
                m_type = type;
                TextureState = waitTillLoaded ? FileTextureState.Unloaded : FileTextureState.Requested;
                m_skipQualityReduction = skipQualityReduction;
            }

            public void Load()
            {
                if (TextureState == FileTextureState.Loaded)
                    return;

                string path = Path.Combine(MyFileSystem.ContentPath, Name);

                Debug.Assert(m_resource == null);
                Debug.Assert(m_srv == null, "Texture " + Name + " in invalid state");

                Image img = null;

                if (MyFileSystem.FileExists(path))
                {
                    try
                    {
                        using (var s = MyFileSystem.OpenRead(path))
                        {
                            img = Image.Load(s);
                            m_imageFormatInFile = img.Description.Format;
                        }
                    }
                    catch (Exception e)
                    {
                        MyRender11.Log.WriteLine("Error while loading texture: " + path + ", exception: " + e);
                    }
                }

                bool loaded = false;
                if (img != null)
                {
                    int skipMipmaps = (m_type != MyFileTextureEnum.GUI && m_type != MyFileTextureEnum.GPUPARTICLES && img.Description.MipLevels > 1)
                        ? MyRender11.RenderSettings.TextureQuality.MipmapsToSkip(img.Description.Width,
                            img.Description.Height)
                        : 0;

                    if (m_skipQualityReduction)
                        skipMipmaps = 0;

                    int totalSize = 0;

                    int targetMipmaps = img.Description.MipLevels - skipMipmaps;
                    var mipmapsData = new DataBox[(img.Description.MipLevels - skipMipmaps) * img.Description.ArraySize];

                    long delta = 0;
                    int lastSize = 0;

                    for (int z = 0; z < img.Description.ArraySize; z++)
                    {
                        for (int i = 0; i < targetMipmaps; i++)
                        {
                            var pixels = img.GetPixelBuffer(z, i + skipMipmaps);
                            mipmapsData[Resource.CalculateSubResourceIndex(i, z, targetMipmaps)] =
                                new DataBox { DataPointer = pixels.DataPointer, RowPitch = pixels.RowStride };
                            delta = pixels.DataPointer.ToInt64() - img.DataPointer.ToInt64();

                            lastSize = pixels.BufferStride;
                            totalSize += lastSize;
                        }
                    }

                    var targetWidth = img.Description.Width >> skipMipmaps;
                    var targetHeight = img.Description.Height >> skipMipmaps;

                    bool overwriteFormatToSrgb = (m_type != MyFileTextureEnum.NORMALMAP_GLOSS) &&
                                                 !FormatHelper.IsSRgb(img.Description.Format);

                    var desc = new Texture2DDescription
                    {
                        MipLevels = targetMipmaps,
                        Format = overwriteFormatToSrgb ? MyResourceUtils.MakeSrgb(img.Description.Format) : img.Description.Format,
                        Height = targetHeight,
                        Width = targetWidth,
                        ArraySize = img.Description.ArraySize,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        Usage = ResourceUsage.Immutable,
                        SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                        OptionFlags =
                            img.Description.Dimension == TextureDimension.TextureCube
                                ? ResourceOptionFlags.TextureCube
                                : ResourceOptionFlags.None
                    };

                    try
                    {
                        m_resource = new Texture2D(MyRender11.Device, desc, mipmapsData);
                        m_size = new Vector2I(targetWidth, targetHeight);
                        //m_skippedMipmaps = skipMipmaps;
                        m_fileExists = true;
                        m_byteSize = totalSize;
                        m_ownsData = true;

                        m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                        m_resource.DebugName = m_name;
                        m_srv.DebugName = m_name;

                        img.Dispose();

                        loaded = true;
                    }
                    catch (SharpDXException)
                    {
                        img.Dispose();
                    }
                }
                if (!loaded)
                {
                    ISrvBindable replacingTexture = MyGeneratedTextureManager.ZeroTex;
                    switch (m_type)
                    {
                        case MyFileTextureEnum.NORMALMAP_GLOSS:
                            replacingTexture = MyGeneratedTextureManager.MissingNormalGlossTex;
                            break;
                        case MyFileTextureEnum.EXTENSIONS:
                            replacingTexture = MyGeneratedTextureManager.MissingExtensionTex;
                            break;
                        case MyFileTextureEnum.ALPHAMASK:
                            replacingTexture = MyGeneratedTextureManager.MissingAlphamaskTex;
                            break;
                        case MyFileTextureEnum.CUBEMAP:
                            replacingTexture = MyGeneratedTextureManager.MissingCubeTex;
                            break;
                    }

                    MyRender11.Log.WriteLine("Could not load texture: " + path);

                    m_srv = replacingTexture.Srv;
                    m_resource = replacingTexture.Resource;
                    m_size = replacingTexture.Size;
                    m_ownsData = false;
                    m_fileExists = false;
                    m_byteSize = 0;
                    MyRender11.Log.WriteLine("Missing or invalid texture: " + Name);
                }

                TextureState = FileTextureState.Loaded;
            }

            public void Destroy()
            {
                TextureState = FileTextureState.Unloaded;
                if (m_ownsData)
                {
                    m_srv.Dispose();
                    m_resource.Dispose();
                }
                m_srv = null;
                m_resource = null;
            }

            void DoLoading()
            {
                switch (TextureState)
                {
                    case FileTextureState.Unloaded:
                        MyManagers.FileTextures.Load(this);
                        break;
                    case FileTextureState.Requested: // We do nothing and let the resources be loaded in batch later
                        break;
                    case FileTextureState.Loaded:
                        break;
                }
            }
        }
    }

    internal class MyFileTextureManager : IManager, IManagerDevice, IManagerCallback
    {
        internal static class MyFileTextureHelper
        {
            internal static bool IsAssetTextureFilter(IFileTexture texture)
            {
                MyFileTexture textureInternal = (MyFileTexture)texture;
                return textureInternal.TextureType != MyFileTextureEnum.SYSTEM;
            }

            internal static bool IsQualityDependantFilter(IFileTexture texture)
            {
                MyFileTexture textureInternal = (MyFileTexture)texture;
                MyFileTextureEnum type = textureInternal.TextureType;

                if (type == MyFileTextureEnum.SYSTEM)
                    return false;
                if (type == MyFileTextureEnum.GUI)
                    return false;
                if (type == MyFileTextureEnum.GPUPARTICLES)
                    return false;
                return true;
            }
        }


        bool m_isDeviceInit;
        Stopwatch m_sw = new Stopwatch();

        readonly Dictionary<string, ISrvBindable> m_unmannagedTextures = new Dictionary<string, ISrvBindable>();

        readonly Dictionary<string, MyFileTexture> m_textures = new Dictionary<string, MyFileTexture>();
        readonly HashSet<string> m_loadedTextures = new HashSet<string>();
        readonly HashSet<string> m_requestedTextures = new HashSet<string>();

        readonly MyObjectsPool<MyFileTexture> m_texturesPool = new MyObjectsPool<MyFileTexture>(1024);


        public MyFileTextureManager()
        {
            RegisterDefaultTextures();
        }

        void RegisterDefaultTextures()
        {
            m_unmannagedTextures.Add("EMPTY", MyGeneratedTextureManager.ZeroTex);
            m_unmannagedTextures.Add("MISSING_NORMAL_GLOSS", MyGeneratedTextureManager.MissingNormalGlossTex);
            m_unmannagedTextures.Add("MISSING_EXTENSIONS", MyGeneratedTextureManager.MissingExtensionTex);
            m_unmannagedTextures.Add("Pink", MyGeneratedTextureManager.PinkTex);
            m_unmannagedTextures.Add("MISSING_CUBEMAP", MyGeneratedTextureManager.MissingCubeTex);
            m_unmannagedTextures.Add("INTEL_FALLBACK_CUBEMAP", MyGeneratedTextureManager.IntelFallbackCubeTex);
            m_unmannagedTextures.Add("DITHER_8x8", MyGeneratedTextureManager.Dithering8x8Tex);
            m_unmannagedTextures.Add("MISSING_ALPHAMASK", MyGeneratedTextureManager.MissingAlphamaskTex);
            m_unmannagedTextures.Add("Random", MyGeneratedTextureManager.RandomTex);
        }

        #region Loading

        static string TerminatePath(string path)
        {
            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] == Path.DirectorySeparatorChar)
                return path;

            return path + Path.DirectorySeparatorChar;
        }

        static void MakeRelativePath(ref String absPathFile)
        {
            String contentPath = TerminatePath(MyFileSystem.ContentPath);

            Uri fromUri = new Uri(contentPath);
            Uri toUri = new Uri(absPathFile);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return;
            } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            absPathFile = relativePath;
        }

        public ISrvBindable GetTexture(string name, MyFileTextureEnum type, bool waitTillLoaded = false, bool skipQualityReduction = false)
        {
            if (name == null || name.Length <= 0)
            {
                switch (type)
                {
                    case MyFileTextureEnum.NORMALMAP_GLOSS:
                        return MyGeneratedTextureManager.MissingNormalGlossTex;
                    case MyFileTextureEnum.EXTENSIONS:
                        return MyGeneratedTextureManager.MissingExtensionTex;
                    case MyFileTextureEnum.ALPHAMASK:
                        return MyGeneratedTextureManager.MissingAlphamaskTex;
                    case MyFileTextureEnum.CUBEMAP:
                        return MyGeneratedTextureManager.MissingCubeTex;
                }
                return MyRender11.DebugMode ? MyGeneratedTextureManager.PinkTex : MyGeneratedTextureManager.ZeroTex;
            }

            // Check whether name is absolute path to file
            if (System.IO.Path.IsPathRooted(name))
                MakeRelativePath(ref name);

            if (m_unmannagedTextures.ContainsKey(name))
                return m_unmannagedTextures[name];


            MyFileTexture texOut;

            if (!m_textures.TryGetValue(name, out texOut))
            {
                m_texturesPool.AllocateOrCreate(out texOut);
                texOut.Init(name, type, waitTillLoaded, skipQualityReduction);
                m_textures.Add(name, texOut);
            }

            switch (texOut.TextureState)
            {
                case FileTextureState.Unloaded:
                case FileTextureState.Requested:
                    if (waitTillLoaded)
                        LoadInternal(name);
                    else
                    {
                        texOut.TextureState = FileTextureState.Requested;
                        m_requestedTextures.Add(name);
                    }
                    break;
                case FileTextureState.Loaded:
                    break;
            }

            return texOut;
        }

        public void LoadAllRequested()
        {
            if (m_requestedTextures.Count == 0)
                return;

            m_sw.Restart();

            foreach (var it in m_requestedTextures)
            {
                var tex = m_textures[it];
                LoadInternal(tex.Name, false);
            }

            m_sw.Stop();
            Debug.WriteLine(String.Format("Loaded {0} textures in {1} s", m_requestedTextures.Count, m_sw.Elapsed.TotalSeconds));
            m_requestedTextures.Clear();
        }

        public void Load(MyFileTexture tex)
        {
            Debug.Assert(m_textures.ContainsKey(tex.Name));
            LoadInternal(tex.Name);
        }

        private void LoadInternal(string name, bool alterRequested = true)
        {
            if (m_loadedTextures.Contains(name))
                return;

            MyFileTexture tex = m_textures[name];
            tex.Load();

            if (alterRequested)
                m_requestedTextures.Remove(name);
            m_loadedTextures.Add(name);
        }

        #endregion

        #region Disposal

        public void DisposeAll()
        {
            foreach (var it in m_loadedTextures)
            {
                DisposeTexInternal(it, false);
            }

            m_loadedTextures.Clear();
        }

        public void DisposeTex(Func<IFileTexture, bool> filter)
        {
            foreach (var tex in m_loadedTextures.Where(t => filter(m_textures[t])))
            {
                DisposeTexInternal(tex, false);
            }

            m_loadedTextures.RemoveWhere(t => filter(m_textures[t]));
        }

        public void DisposeTex(ISrvBindable texture)
        {
            MyFileTexture texInternal = texture as MyFileTexture;
            Debug.Assert(texInternal != null);
            DisposeTex(texInternal);
        }

        public void DisposeTex(MyFileTexture texture)
        {
            DisposeTex(texture.Name);
        }

        public void DisposeTex(string name)
        {
            DisposeTexInternal(name);
        }

        void DisposeTexInternal(string name, bool alterLoaded = true)
        {
            MyRenderProxy.Assert(m_textures.ContainsKey(name), "The texture has not been created by this manager");

            if (!m_loadedTextures.Contains(name))
                return;

            if (alterLoaded)
                m_loadedTextures.Remove(name); // Will not throw if not found
            m_requestedTextures.Remove(name); // Will not throw if not found

            // We keep the texture object but destroy the dx resources
            m_textures[name].Destroy();
        }

        #endregion

        #region Reporting

        public MyFileTextureUsageReport GetReport()
        {
            MyFileTextureUsageReport report;
            report.TexturesTotal = m_textures.Count;
            report.TexturesLoaded = m_textures.Count - m_loadedTextures.Count;
            report.TotalTextureMemory = GetTotalByteSizeOfResources();

            return report;
        }

        public StringBuilder GetFileTexturesDesc()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Loaded file textures:");
            builder.AppendLine("[Filename    Width x Height    Format    Internat texture type    Byte size ]");
            foreach (var tex in m_texturesPool.Active)
            {
                builder.AppendFormat("{0}    {1} x {2}    {3}    {4}    {5} bytes", tex.Name.Replace("/", @"\"), tex.Size.X, tex.Size.Y, tex.ImageFormatInFile, tex.TextureType, tex.ByteSize);
                builder.AppendLine();
            }
            return builder;
        }

        public long GetTotalByteSizeOfResources()
        {
            return m_textures.Values.Sum(t => t.ByteSize);
        }

        public int GetUnmannagedTexturesCount()
        {
            return m_unmannagedTextures.Count;
        }

        public int GetMannagedTexturesCount()
        {
            return m_textures.Count;
        }

        #endregion

        #region IManagerDevice overrides

        public void OnDeviceInit()
        {
            MyRenderProxy.Assert(m_textures.Count == 0);
            LoadAllRequested();
            m_isDeviceInit = true;
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;
            DisposeAll();
        }

        public void OnUnloadData()
        {
            DisposeTex(MyFileTextureHelper.IsQualityDependantFilter);
        }

        public void OnFrameEnd()
        {
        }

        #endregion
    }
}
