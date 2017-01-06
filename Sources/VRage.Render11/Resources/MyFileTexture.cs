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
using VRageRender.Messages;

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
        public int TexturesTotalPeak;
        public int TexturesLoadedPeak;
        public long TotalTextureMemoryPeak;
    }

    internal interface IFileTexture : ITexture
    {
        string Path { get; }
    }

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
            string m_path;
            Vector2I m_size;
            MyFileTextureEnum m_type;
            bool m_ownsData;
            bool m_skipQualityReduction;
            
            Format m_format = SharpDX.DXGI.Format.Unknown;
            internal Format ImageFormatInFile = SharpDX.DXGI.Format.Unknown;

            // data status
            internal FileTextureState TextureState;
            int m_byteSize;

            ShaderResourceView m_srv;
            Resource m_resource;

            public bool Temporary { get; set; }

            public int MipmapCount { get; set; }

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

            public string Path
            {
                get { return m_path; }
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

            public Format Format
            {
                get
                {
                    DoLoading();
                    return m_format;
                }
            }

            public void Init(string name, string localPath, MyFileTextureEnum type, bool waitTillLoaded, bool skipQualityReduction, bool temporary)
            {
                Debug.Assert(System.IO.Path.IsPathRooted(localPath), "Path must be rooted");
                m_name = name;
                m_path = System.IO.Path.GetFullPath(localPath);
                m_type = type;
                TextureState = waitTillLoaded ? FileTextureState.Unloaded : FileTextureState.Requested;
                m_skipQualityReduction = skipQualityReduction;
                Temporary = temporary;
            }

            public unsafe void Load()
            {
                if (TextureState == FileTextureState.Loaded)
                    return;

                Debug.Assert(m_resource == null);
                Debug.Assert(m_srv == null, "Texture " + Name + " in invalid state");

                Image img = null;

                if (MyFileSystem.FileExists(m_path))
                {
                    try
                    {
                        using (var s = MyFileSystem.OpenRead(m_path))
                        {
                            img = Image.Load(s);
                            ImageFormatInFile = img.Description.Format;
                        }
                    }
                    catch (Exception e)
                    {
                        MyRender11.Log.WriteLine("Error while loading texture: " + m_path + ", exception: " + e);
                    }
                }

                bool loaded = false;
                if (img != null)
                {
                    int skipMipmaps = 0;
                    if (m_type != MyFileTextureEnum.GUI && m_type != MyFileTextureEnum.GPUPARTICLES && img.Description.MipLevels > 1)
                        skipMipmaps = MyRender11.Settings.User.TextureQuality.MipmapsToSkip(img.Description.Width, img.Description.Height);

                    if (m_skipQualityReduction)
                        skipMipmaps = 0;

                    int totalSize = 0;

                    int targetMipmaps = img.Description.MipLevels - skipMipmaps;
                    var mipmapsData = new DataBox[(img.Description.MipLevels - skipMipmaps) * img.Description.ArraySize];
                    if (img.Description.MipLevels <= 1)
                        Debug.WriteLine(Name);

                    int lastSize = 0;

                    for (int z = 0; z < img.Description.ArraySize; z++)
                    {
                        for (int i = 0; i < targetMipmaps; i++)
                        {
                            var pixels = img.GetPixelBuffer(z, i + skipMipmaps);
                            mipmapsData[Resource.CalculateSubResourceIndex(i, z, targetMipmaps)] =
                                new DataBox { DataPointer = pixels.DataPointer, RowPitch = pixels.RowStride };

                            void* data = pixels.DataPointer.ToPointer();

                            lastSize = pixels.BufferStride;
                            totalSize += lastSize;
                        }
                    }

                    var targetWidth = img.Description.Width >> skipMipmaps;
                    var targetHeight = img.Description.Height >> skipMipmaps;


                    bool overwriteFormatToSrgb = false;
                    if (MyCompilationSymbols.ReinterpretFormatsStoredInFiles)
                        overwriteFormatToSrgb = (m_type != MyFileTextureEnum.NORMALMAP_GLOSS) && !FormatHelper.IsSRgb(img.Description.Format);

                    m_format = overwriteFormatToSrgb ? MyResourceUtils.MakeSrgb(img.Description.Format) : img.Description.Format;

                    var desc = new Texture2DDescription
                    {
                        MipLevels = targetMipmaps,
                        Format = m_format,
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
                        m_byteSize = totalSize;
                        m_ownsData = true;

                        m_srv = new ShaderResourceView(MyRender11.Device, m_resource);
                        m_resource.DebugName = m_name;
                        m_srv.DebugName = m_name;

                        img.Dispose();

                        MipmapCount = targetMipmaps;

                        loaded = true;
                    }
                    catch (SharpDXException)
                    {
                        img.Dispose();
                    }
                }
                if (!loaded)
                {
                    ISrvBindable replacingTexture = MyGeneratedTextureManager.PinkTex;
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

                    MyRender11.Log.WriteLine("Could not load texture: " + m_path);

                    m_srv = replacingTexture.Srv;
                    m_resource = replacingTexture.Resource;
                    m_size = replacingTexture.Size;
                    m_ownsData = false;
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

    internal class MyFileTextureManager : IManager, IManagerDevice, IManagerUnloadData
    {
        const string FILE_SCHEME = "file";

        static MyFileTextureUsageReport m_report;

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


        Stopwatch m_sw = new Stopwatch();

        Dictionary<string, IGeneratedTexture> m_generatedTextures = new Dictionary<string, IGeneratedTexture>();

        bool m_temporaryTextureRequested;
        readonly Dictionary<string, MyFileTexture> m_textures = new Dictionary<string, MyFileTexture>();
        readonly HashSet<string> m_loadedTextures = new HashSet<string>();
        readonly HashSet<string> m_requestedTextures = new HashSet<string>();

        readonly MyObjectsPool<MyFileTexture> m_texturesPool = new MyObjectsPool<MyFileTexture>(1024);

        void RegisterDefaultTextures()
        {
            m_generatedTextures = new Dictionary<string, IGeneratedTexture>();

            m_generatedTextures[MyGeneratedTextureManager.ZeroTex.Name] = MyGeneratedTextureManager.ZeroTex;
            m_generatedTextures[MyGeneratedTextureManager.MissingNormalGlossTex.Name] = MyGeneratedTextureManager.MissingNormalGlossTex;
            m_generatedTextures[MyGeneratedTextureManager.MissingExtensionTex.Name] = MyGeneratedTextureManager.MissingExtensionTex;
            m_generatedTextures[MyGeneratedTextureManager.PinkTex.Name] = MyGeneratedTextureManager.PinkTex;
            m_generatedTextures[MyGeneratedTextureManager.MissingCubeTex.Name] = MyGeneratedTextureManager.MissingCubeTex;
            m_generatedTextures[MyGeneratedTextureManager.IntelFallbackCubeTex.Name] = MyGeneratedTextureManager.IntelFallbackCubeTex;
            m_generatedTextures[MyGeneratedTextureManager.Dithering8x8Tex.Name] = MyGeneratedTextureManager.Dithering8x8Tex;
            m_generatedTextures[MyGeneratedTextureManager.MissingAlphamaskTex.Name] = MyGeneratedTextureManager.MissingAlphamaskTex;
            m_generatedTextures[MyGeneratedTextureManager.RandomTex.Name] = MyGeneratedTextureManager.RandomTex;
        }

        #region Loading

        /// <remarks>On big loops, or whenever recommendable, cache the returned reference</remarks>
        public ITexture GetTexture(string name, MyFileTextureEnum type, bool waitTillLoaded = false, bool skipQualityReduction = false,
            bool temporary = false)
        {
            if (name == null || name.Length <= 0)
                return ReturnDefaultTexture(type);

            Uri uri;
            if (!MyResourceUtils.NormalizeFileTextureName(ref name, out uri))
            {
                IGeneratedTexture texture;
                if (m_generatedTextures.TryGetValue(name, out texture))
                    return texture;
                else
                {
                    MyRenderProxy.Assert(false, "Can't find generated texture with name \"" + name + "\"");
                    return ReturnDefaultTexture(type);
                }
            }

            MyFileTexture texOut;
            if (!m_textures.TryGetValue(name, out texOut))
            {
                if (uri.Scheme != FILE_SCHEME)
                {
                    Debug.Assert(false, "Cannot initialize a non file texture");
                    return ReturnDefaultTexture(type);
                }

                m_texturesPool.AllocateOrCreate(out texOut);
                texOut.Init(name, uri.LocalPath, type, waitTillLoaded, skipQualityReduction, temporary);
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

            texOut.Temporary &= temporary;
            m_temporaryTextureRequested |= temporary;

            return texOut;
        }

        public bool TryGetTexture(string name, out ITexture texture)
        {
            Uri uri;
            bool found;
            if (!MyResourceUtils.NormalizeFileTextureName(ref name, out uri))
            {
                IGeneratedTexture generatedTexture;
                found = m_generatedTextures.TryGetValue(name, out generatedTexture);
                texture = generatedTexture;
                return found;
            }

            MyFileTexture fileTexture;
            found = m_textures.TryGetValue(name, out fileTexture);
            texture = fileTexture;
            return found;
        }

        public bool TryGetTexture(string name, out IUserGeneratedTexture texture)
        {
            Uri uri;
            if (MyResourceUtils.NormalizeFileTextureName(ref name, out uri))
            {
                texture = null;
                return false;
            }

            IGeneratedTexture generatedTexture;
            m_generatedTextures.TryGetValue(name, out generatedTexture);
            texture = generatedTexture as IUserGeneratedTexture;
            return texture != null;
        }

        public IUserGeneratedTexture CreateGeneratedTexture(string name, int width, int height, MyTextureType type, int numMipLevels)
        {
            return CreateGeneratedTexture(name, width, height, type.ToGeneratedTextureType(), numMipLevels);
        }

        public IUserGeneratedTexture CreateGeneratedTexture(string name, int width, int height, MyGeneratedTextureType type, int numMipLevels)
        {
            IGeneratedTexture texture;
            if (m_generatedTextures.TryGetValue(name, out texture))
            {
                IUserGeneratedTexture userTexture =  texture as IUserGeneratedTexture;
                if (userTexture == null)
                {
                    MyRenderProxy.Fail("Trying to replace system texture");
                    return null;
                }

                if (userTexture.Size.X != width || userTexture.Size.Y != height ||
                        userTexture.Type != type || userTexture.MipmapCount != numMipLevels)
                {
                    MyRenderProxy.Fail("Trying to replace existing texture");
                }

                return userTexture;
            }

            var manager = MyManagers.GeneratedTextures;
            IUserGeneratedTexture ret = manager.NewUserTexture(name, width, height, type, numMipLevels);
            m_generatedTextures[name] = ret;
            return ret;
        }

        public void ResetGeneratedTexture(string name, byte[] data)
        {
            IGeneratedTexture texture;
            IUserGeneratedTexture userTexture;
            if (!m_generatedTextures.TryGetValue(name, out texture) || (userTexture = texture as IUserGeneratedTexture) == null )
            {
                Debug.Assert(false, "Failed to find generated texture \"" + name + "\"");
                return;
            }

            userTexture.Reset(data);
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

        private ITexture ReturnDefaultTexture(MyFileTextureEnum type)
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
            return MyGeneratedTextureManager.PinkTex;
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

        public void DisposeTex(string name, bool ignoreFailure = false)
        {
            DisposeTexInternal(name, ignoreFailure: ignoreFailure);
        }

        void DisposeTexInternal(string name, bool alterLoaded = true, bool ignoreFailure = false)
        {
            if (!MyResourceUtils.NormalizeFileTextureName(ref name))
            {
                IGeneratedTexture texture;
                if (m_generatedTextures.TryGetValue(name, out texture))
                {
                    IUserGeneratedTexture userTexture = texture as IUserGeneratedTexture;
                    if (userTexture == null)
                        MyRenderProxy.Assert(false, "Can't dispose system texture");
                    else
                        MyManagers.GeneratedTextures.DisposeTex(userTexture);

                    return;
                }
                else
                {
                    MyRenderProxy.Assert(false, "Can't find generated texture with name \"" + name + "\"");
                    return;
                }
            }

            MyRenderProxy.Assert(m_textures.ContainsKey(name) || ignoreFailure, "The texture has not been created by this manager");

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
            m_report.TexturesTotal = m_textures.Count;
            m_report.TexturesLoaded = m_loadedTextures.Count;
            m_report.TotalTextureMemory = GetTotalByteSizeOfResources();

            m_report.TexturesTotalPeak = Math.Max(m_report.TexturesTotalPeak, m_report.TexturesTotal);
            m_report.TexturesLoadedPeak = Math.Max(m_report.TexturesLoadedPeak, m_report.TexturesLoaded);
            m_report.TotalTextureMemoryPeak = Math.Max(m_report.TotalTextureMemoryPeak, m_report.TotalTextureMemory);

            return m_report;
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

        public int GeneratedTexturesCount
        {
            get { return m_generatedTextures.Count; }
        }

        public int FileTexturesCount
        {
            get { return m_textures.Count; }
        }

        #endregion

        #region IManagerDevice overrides

        public void OnDeviceInit()
        {
            MyRenderProxy.Assert(m_textures.Count == 0);
            RegisterDefaultTextures();
            LoadAllRequested();
        }

        public void OnDeviceReset()
        {
            OnDeviceEnd();
            OnDeviceInit();
        }

        public void OnDeviceEnd()
        {
            DisposeAll();
            RemoveUserGeneratedTextures();
        }

        void IManagerUnloadData.OnUnloadData()
        {
            DisposeTex(MyFileTextureHelper.IsQualityDependantFilter);
            RemoveUserGeneratedTextures();
        }

        // Free and remove all user generated texures
        private void RemoveUserGeneratedTextures()
        {
            var texturesToRemove = new List<IUserGeneratedTexture>();
            foreach (var texture in m_generatedTextures.Values)
            {
                var userTexture = texture as IUserGeneratedTexture;
                if (userTexture != null)
                    texturesToRemove.Add(userTexture);
            }

            foreach (var texture in texturesToRemove)
            {
                MyManagers.GeneratedTextures.DisposeTex(texture);
                m_generatedTextures.Remove(texture.Name);
            }
        }

        public void OnFrameEnd()
        {
            if (m_temporaryTextureRequested)
            {
                var texturesToRemove = new List<MyFileTexture>();
                foreach (var texture in m_textures.Values)
                {
                    if (texture.Temporary)
                        texturesToRemove.Add(texture);
                }

                foreach (var texture in texturesToRemove)
                {
                    DisposeTex(texture);
                    m_textures.Remove(texture.Name);
                }
            }
            m_temporaryTextureRequested = false;
        }

        #endregion
    }

    static class TextureExtensions
    {
        public static MyGeneratedTextureType ToGeneratedTextureType(this Format type)
        {
            switch (type)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    return MyGeneratedTextureType.RGBA;
                case Format.R8G8B8A8_UNorm:
                    return MyGeneratedTextureType.RGBA_Linear;
                case Format.R8_UNorm:
                    return MyGeneratedTextureType.Alphamask;
                default:
                    throw new Exception();
            }
        }

        public static MyGeneratedTextureType ToGeneratedTextureType(this MyTextureType type)
        {
            switch (type)
            {
                case MyTextureType.ColorMetal:
                    return MyGeneratedTextureType.RGBA;
                case MyTextureType.NormalGloss:
                    return MyGeneratedTextureType.RGBA_Linear;
                case MyTextureType.Extensions:
                    return MyGeneratedTextureType.RGBA;
                case MyTextureType.Alphamask:
                    return MyGeneratedTextureType.Alphamask;
                default:
                    throw new Exception();
            }
        }
    }
}
