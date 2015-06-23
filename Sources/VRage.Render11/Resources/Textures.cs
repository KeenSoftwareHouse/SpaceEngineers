using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using VRage.Utils;
using System.Reflection;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using Texture2D = SharpDX.Direct3D11.Texture2D;
using Texture1D = SharpDX.Direct3D11.Texture1D;
using Vector2 = VRageMath.Vector2;
using Resource = SharpDX.Direct3D11.Resource;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRageMath;

namespace VRageRender.Resources
{
    struct TexId
    {
        internal int Index;

        public static bool operator ==(TexId x, TexId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(TexId x, TexId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly TexId NULL = new TexId { Index = -1 };
    }

    enum MyTextureEnum
    {
        GUI,
        COLOR_METAL,
        NORMALMAP_GLOSS,
        EXTENSIONS,
        ALPHAMASK,
        CUBEMAP,
        SYSTEM,
        CUSTOM
    }

    enum MyTextureState
    {
        WAITING,
        LOADED
    }

    struct MyTextureInfo
    {
        // immutable data
        internal string Name;
        internal string ContentPath;
        internal MyTextureEnum Type;
        internal bool OwnsData;

        // data status
        internal bool FileExists;
        internal int SkippedMipmaps;
        internal Resource Resource;
        internal Vector2 Size;
    }

    static class MyTextures
    {
        static Dictionary<MyStringId, TexId> NameIndex = new Dictionary<MyStringId, TexId>(MyStringId.Comparer);
        internal static MyFreelist<MyTextureInfo> Textures = new MyFreelist<MyTextureInfo>(512);
        internal static ShaderResourceView[] Views = new ShaderResourceView[512];

        static HashSet<TexId>[] State;

        internal static TexId ZeroTexId;
        internal static TexId MissingNormalGlossTexId;
        internal static TexId MissingAlphamaskTexId;
        internal static TexId MissingExtensionTexId;
        internal static TexId Dithering8x8TexId;
        internal static TexId DebugPinkTexId;
        internal static TexId MissingCubeTexId;
        internal static TexId IntelFallbackCubeTexId;

        internal static void Init()
        {
            //MyCallbacks.RegisterDeviceEndListener(new OnDeviceEndDelegate(OnDeviceEnd));
            //MyCallbacks.RegisterDeviceResetListener(new OnDeviceResetDelegate(OnDeviceReset));

            int statesNum = Enum.GetNames(typeof(MyTextureState)).Length;
            State = new HashSet<TexId>[statesNum];
            for(int i=0; i<statesNum; i++)
            {
                State[i] = new HashSet<TexId>();
            }

            CreateCommonTextures();
        }

        internal static void InitState(TexId texId, MyTextureState state)
        {
            Debug.Assert(!CheckState(texId, MyTextureState.LOADED));
            Debug.Assert(!CheckState(texId, MyTextureState.WAITING));

            State[(int)state].Add(texId);
        }

        internal static void MoveState(TexId texId, MyTextureState from, MyTextureState to)
        {
            State[(int)from].Remove(texId);
            State[(int)to].Add(texId);
        }

        internal static bool CheckState(TexId texId, MyTextureState state)
        {
            return State[(int)state].Contains(texId);
        }

        internal static void ClearState(TexId texId)
        {
            for(int i=0; i<State.Length; i++)
            {
                State[i].Remove(texId);
            }
        }

        static Format MakeSrgb(Format fmt)
        {
            switch(fmt)
            {
                case Format.R8G8B8A8_UNorm:
                    return Format.R8G8B8A8_UNorm_SRgb;
                case Format.B8G8R8A8_UNorm:
                    return Format.B8G8R8A8_UNorm_SRgb;
                case Format.B8G8R8X8_UNorm:
                    return Format.B8G8R8X8_UNorm_SRgb;
            }
            return fmt;
        }

        static void LoadTexture(TexId texId)
        {
            var contentPath = Textures.Data[texId.Index].ContentPath;
            string path;
            
            if (string.IsNullOrEmpty(contentPath))
            {
                path = Path.Combine(MyFileSystem.ContentPath, Textures.Data[texId.Index].Name);
            }
            else
            { 
                path = Path.Combine(contentPath, Textures.Data[texId.Index].Name);
            }

            Debug.Assert(Textures.Data[texId.Index].Resource == null);
            Debug.Assert(GetView(texId) == null, "Texture " + Textures.Data[texId.Index].Name + " in invalid state");

            Image img = null;

            if (MyFileSystem.FileExists(path))
            {
                try
                {
                    using (var s = MyFileSystem.OpenRead(path))
                    {
                        img = Image.Load(s);
                    }
                }
                catch(Exception e)
                {
                    MyRender11.Log.WriteLine("Could not load texture: " + path + ", exception: " + e);
                }
            }

            bool loaded = false;
            if (img != null)
            {         
                int skipMipmaps = (Textures.Data[texId.Index].Type != MyTextureEnum.GUI && img.Description.MipLevels > 1) ? MyRender11.RenderSettings.TextureQuality.MipmapsToSkip(img.Description.Width, img.Description.Height) : 0;

                int targetMipmaps = img.Description.MipLevels - skipMipmaps;
                var mipmapsData = new DataBox[(img.Description.MipLevels - skipMipmaps) * img.Description.ArraySize];
                for(int z = 0; z<img.Description.ArraySize; z++)
                {
                    for (int i = 0; i < targetMipmaps; i++)
                    {
                        var pixels = img.GetPixelBuffer(z, i + skipMipmaps);
                        mipmapsData[Resource.CalculateSubResourceIndex(i, z, targetMipmaps)] = 
                            new DataBox { DataPointer = pixels.DataPointer, RowPitch = pixels.RowStride };
                    }
                }

                var targetWidth = img.Description.Width >> skipMipmaps;
                var targetHeight = img.Description.Height >> skipMipmaps;

                bool overwriteFormatToSrgb = Textures.Data[texId.Index].Type == MyTextureEnum.COLOR_METAL &&
                    !SharpDX.DXGI.FormatHelper.IsCompressed(img.Description.Format) &&
                    !SharpDX.DXGI.FormatHelper.IsSRgb(img.Description.Format);

                var desc = new Texture2DDescription
                {
                    MipLevels = targetMipmaps,
                    Format = overwriteFormatToSrgb ? MakeSrgb(img.Description.Format) : img.Description.Format,
                    Height = targetHeight,
                    Width = targetWidth,
                    ArraySize = img.Description.ArraySize,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Usage = ResourceUsage.Immutable,
                    SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                    OptionFlags = img.Description.Dimension == TextureDimension.TextureCube ? ResourceOptionFlags.TextureCube : ResourceOptionFlags.None
                };

                try
                {
                    var resource = new Texture2D(MyRender11.Device, desc, mipmapsData);

                    Textures.Data[texId.Index].Resource = resource;
                    Textures.Data[texId.Index].Size = new Vector2(targetWidth, targetHeight);
                    Textures.Data[texId.Index].SkippedMipmaps = skipMipmaps;
                    Textures.Data[texId.Index].FileExists = true;
                    Views[texId.Index] = new ShaderResourceView(MyRender11.Device, resource);
                    resource.DebugName = path;
                    Views[texId.Index].DebugName = path;

                    img.Dispose();

                    loaded = true;
                }
                catch (SharpDXException)
                {
                    img.Dispose();
                }
            }
            if(!loaded)
            {
                // set data to some crap
                TexId replacingId = ZeroTexId;

                switch(Textures.Data[texId.Index].Type)
                {
                    case MyTextureEnum.NORMALMAP_GLOSS:
                        replacingId = MissingNormalGlossTexId;
                        break;
                    case MyTextureEnum.EXTENSIONS:
                        replacingId = MissingExtensionTexId;
                        break;
                    case MyTextureEnum.ALPHAMASK:
                        replacingId = MissingAlphamaskTexId;
                        break;
                    case MyTextureEnum.CUBEMAP:
                        replacingId = MissingCubeTexId;
                        break;
                    case MyTextureEnum.COLOR_METAL:
                        replacingId = MyRender11.DebugMode ? DebugPinkTexId : ZeroTexId;
                        break;
                }

                Views[texId.Index] = Views[replacingId.Index];
                Textures.Data[texId.Index].Resource = Textures.Data[replacingId.Index].Resource;
                Textures.Data[texId.Index].Size = Textures.Data[replacingId.Index].Size;
                Textures.Data[texId.Index].OwnsData = false;
            }
        }

        internal static Vector2 GetSize(TexId texId)
        {
            return Textures.Data[texId.Index].Size;
        }

        internal static ShaderResourceView GetView(TexId tex)
        {
            return tex != TexId.NULL ? Views[tex.Index] : null;
        }

        internal static TexId GetTexture(string path, MyTextureEnum type, bool waitTillLoaded = false)
        {
            var nameKey = X.TEXT(path);
            return GetTexture(nameKey, null, type, waitTillLoaded);
        }

        internal static TexId GetTexture(MyStringId nameId, string contentPath, MyTextureEnum type, bool waitTillLoaded = false)
        {
            if(nameId == MyStringId.NullOrEmpty)
            {
                switch (type)
                {
                    case MyTextureEnum.NORMALMAP_GLOSS:
                        return MissingNormalGlossTexId;
                    case MyTextureEnum.EXTENSIONS:
                        return MissingExtensionTexId;
                    case MyTextureEnum.ALPHAMASK:
                        return MissingAlphamaskTexId;
                    case MyTextureEnum.CUBEMAP:
                        return MissingCubeTexId;
                    case MyTextureEnum.COLOR_METAL:
                        return MyRender11.DebugMode ? DebugPinkTexId : ZeroTexId;
                }
                return ZeroTexId;
            }

            var nameKey = nameId;

            if(!string.IsNullOrEmpty(contentPath))
            {
                var fullPath = Path.Combine(contentPath, nameKey.ToString());
                if (MyFileSystem.FileExists(fullPath))
                {
                    nameKey = X.TEXT(fullPath);
                }
                else // take file from main content
                {
                    contentPath = null;
                }
            }

            if (!NameIndex.ContainsKey(nameKey))
            {
                //Debug.Assert(type != MyTextureEnum.SYSTEM);

                var texId = NameIndex[nameKey] = new TexId{ Index = Textures.Allocate() };
                InitState(texId, MyTextureState.WAITING);

                Textures.Data[texId.Index] = new MyTextureInfo
                {
                    Name = nameId.ToString(),
                    ContentPath = contentPath,
                    Type = type,
                    OwnsData = true
                };
                MyArrayHelpers.Reserve(ref Views, texId.Index + 1);
                Views[texId.Index] = null;

                if (waitTillLoaded)
                {
                    LoadTexture(texId);
                    MoveState(texId, MyTextureState.WAITING, MyTextureState.LOADED);
                }
            }
            return NameIndex[nameKey];
        }

        static void UnloadResources(TexId texId)
        {
            //Debug.Assert(CheckState(texId, MyTextureState.LOADED));

            if (Textures.Data[texId.Index].OwnsData)
            {
                if (Textures.Data[texId.Index].Resource != null)
                {
                    Textures.Data[texId.Index].Resource.Dispose();
                    Textures.Data[texId.Index].Resource = null;
                }
                if (Views[texId.Index] != null)
                {
                    Views[texId.Index].Dispose();
                    Views[texId.Index] = null;
                }
            }
            else
            {
                Textures.Data[texId.Index].Resource = null;
                Views[texId.Index] = null;
            }

            Textures.Data[texId.Index].FileExists = false;
            Textures.Data[texId.Index].Size = Vector2.Zero;
            Textures.Data[texId.Index].SkippedMipmaps = 0;

            MoveState(texId, MyTextureState.LOADED, MyTextureState.WAITING);
        }

        internal static void UnloadTexture(string path)
        {
            var nameKey = X.TEXT(path);
            var texId = TexId.NULL;
            if (NameIndex.TryGetValue(nameKey, out texId))
                UnloadResources(texId);
        }

        internal static void RemoveTextures(Func<TexId, bool> filter)
        {
            var removeList = new List<MyStringId>();

            foreach (var kv in NameIndex)
            {
                var texId = kv.Value;
                if(filter(texId))
                {
                    UnloadResources(texId);

                    removeList.Add(kv.Key);
                    Textures.Free(texId.Index);
                    ClearState(texId);
                }
            }

            for(int i=0; i<removeList.Count; i++)
            {
                NameIndex.Remove(removeList[i]);
            }
        }

        static TexId RegisterTexture(string name, string contentPath, MyTextureEnum type, Resource resource, Vector2 size)
        {
            var nameKey = X.TEXT(name);
            if (!NameIndex.ContainsKey(nameKey))
            {
                var texId = NameIndex[nameKey] = new TexId { Index = Textures.Allocate() };

                Textures.Data[texId.Index] = new MyTextureInfo
                {
                    Name = name,
                    ContentPath = contentPath,
                    Type = type,
                    Resource = resource,
                    Size = size
                };

                resource.DebugName = name;

                Views[texId.Index] = new ShaderResourceView(MyRender11.Device, resource);
                Views[texId.Index].DebugName = name;
            }
            else // reregistered after device reset
            {
                var id = NameIndex[nameKey];
                
                if(Textures.Data[id.Index].Resource == null)
                {
                    Textures.Data[id.Index].Resource = resource;
                    resource.DebugName = name;
                    Views[id.Index] = new ShaderResourceView(MyRender11.Device, resource);
                    Views[id.Index].DebugName = name;
                }
            }

            return NameIndex[nameKey];
        }

        static unsafe void CreateCommonTextures()
        {
            { 
                var desc = new Texture2DDescription();
                desc.ArraySize = 1;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                desc.Height = 1;
                desc.Width = 1;
                desc.Usage = ResourceUsage.Immutable;
                desc.MipLevels = 1;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                DataBox[] databox = new DataBox[1];
                uint data = 0;
                void* ptr = &data;

                databox[0].DataPointer = new IntPtr(ptr);
                databox[0].RowPitch = 4;

                ZeroTexId = RegisterTexture("EMPTY", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));

                data = (255 << 16) | (127 << 8) | 127;
                MissingNormalGlossTexId = RegisterTexture("MISSING_NORMAL_GLOSS", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));

                data = 255;
                MissingExtensionTexId = RegisterTexture("MISSING_EXTENSIONS", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));

                data = (127 << 16) | (0 << 8) | 255;
                DebugPinkTexId = RegisterTexture("Pink", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));
            }
            {
                var desc = new Texture2DDescription();
                desc.ArraySize = 6;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                desc.Height = 1;
                desc.Width = 1;
                desc.Usage = ResourceUsage.Immutable;
                desc.MipLevels = 1;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;
                desc.OptionFlags = ResourceOptionFlags.TextureCube;

                DataBox[] databox = new DataBox[6];
                uint data = 0;
                void* ptr = &data;

                for (int i = 0; i < 6; i++)
                {
                    databox[i].DataPointer = new IntPtr(ptr);
                    databox[i].RowPitch = 4;
                }

                MissingCubeTexId = RegisterTexture("MISSING_CUBEMAP", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));
            }
            {
                var desc = new Texture2DDescription();
                desc.ArraySize = 6;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                desc.Height = 1;
                desc.Width = 1;
                desc.Usage = ResourceUsage.Immutable;
                desc.MipLevels = 1;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;
                desc.OptionFlags = ResourceOptionFlags.TextureCube;

                DataBox[] databox = new DataBox[6];
                uint byteval = (uint)(0.2f * 255);
                uint data = byteval | (byteval << 8) | (byteval << 16);
                void* ptr = &data;

                for (int i = 0; i < 6; i++)
                {
                    databox[i].DataPointer = new IntPtr(ptr);
                    databox[i].RowPitch = 4;
                }

                IntelFallbackCubeTexId = RegisterTexture("INTEL_FALLBACK_CUBEMAP", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));
            }
            
            {
                byte[] ditherData = new byte[] {
                    0, 32, 8, 40, 2, 34, 10, 42,
                    48, 16, 56, 24, 50, 18, 58, 26,
                    12, 44, 4, 36, 14, 46, 6, 38, 
                    60, 28, 52, 20, 62, 30, 54, 22,
                    3, 35, 11, 43, 1, 33, 9, 41,
                    51, 19, 59, 27, 49, 17, 57, 25,
                    15, 47, 7, 39, 13, 45, 5, 37,
                    63, 31, 55, 23, 61, 29, 53, 21 };
                for (int i = 0; i < 64; i++)
                {
                    ditherData[i] *= 4;
                }
                var desc = new Texture2DDescription();
                desc.ArraySize = 1;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.Format = SharpDX.DXGI.Format.R8_UNorm;
                desc.Height = 8;
                desc.Width = 8;
                desc.Usage = ResourceUsage.Immutable;
                desc.MipLevels = 1;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                DataBox[] databox = new DataBox[1];
                fixed (byte* dptr = ditherData)
                {
                    databox[0].DataPointer = new IntPtr(dptr);
                    databox[0].RowPitch = 8;
                    Dithering8x8TexId = RegisterTexture("DITHER_8x8", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(8, 8));
                }

                byte bdata = 255;
                void *ptr = &bdata;
                databox[0].DataPointer = new IntPtr(ptr);
                databox[0].RowPitch = 1;
                desc.Height = 1;
                desc.Width = 1;

                MissingAlphamaskTexId = RegisterTexture("MISSING_ALPHAMASK", null, MyTextureEnum.SYSTEM, new Texture2D(MyRender11.Device, desc, databox), new Vector2(1, 1));
            }
        }

        internal static void ReloadAssetTextures()
        {
            foreach(var kv in NameIndex)
            {
                if(Textures.Data[kv.Value.Index].Type != MyTextureEnum.SYSTEM)
                {
                    UnloadResources(kv.Value);
                }
            }
        }

        internal static void ReloadQualityDependantTextures()
        {
            foreach (var kv in NameIndex)
            {
                if (Textures.Data[kv.Value.Index].Type != MyTextureEnum.SYSTEM && Textures.Data[kv.Value.Index].Type != MyTextureEnum.GUI)
                {
                    UnloadResources(kv.Value);
                }
            }
        }

        internal static void OnSessionEnd()
        {
            RemoveTextures(x => Textures.Data[x.Index].Type != MyTextureEnum.GUI && Textures.Data[x.Index].Type != MyTextureEnum.SYSTEM);
        }

        internal static void OnDeviceEnd()
        {
            // drop all 
            foreach (var texId in NameIndex.Values)
            {
                if (Textures.Data[texId.Index].OwnsData)
                {
                    if (Textures.Data[texId.Index].Resource != null)
                    {
                        Textures.Data[texId.Index].Resource.Dispose();
                        Textures.Data[texId.Index].Resource = null;
                    }
                    if (Views[texId.Index] != null)
                    {
                        Views[texId.Index].Dispose();
                        Views[texId.Index] = null;
                    }
                }
                else
                {
                    Textures.Data[texId.Index].Resource = null;
                    Views[texId.Index] = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            ReloadAssetTextures();
            CreateCommonTextures();
        }

        // i don't use it because I need to check if full paths are what is requested later from game
        //internal static void PreloadTextures(string inDirectory, bool recursive)
        //{
        //    SearchOption search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        //    var files = Directory.GetFiles(Path.Combine(MyFileSystem.ContentPath, inDirectory), "*.dds", search);

        //    foreach (var file in files)
        //    {
        //        GetTexture(file, MyTextureEnum.GUI, true);
        //    }
        //}

        internal static void Load()
        {
            int texturesToLoad = State[(int)MyTextureState.WAITING].Count;

            if(texturesToLoad > 0)
            {
                var x = new Stopwatch();
                x.Start();

                foreach (var texId in State[(int)MyTextureState.WAITING])
                {
                    LoadTexture(texId);
                }

                foreach (var texId in State[(int)MyTextureState.WAITING].ToList())
                {
                    MoveState(texId, MyTextureState.WAITING, MyTextureState.LOADED);
                }

                x.Stop();
                Debug.WriteLine(String.Format("Loaded {0} textures in {1} s", texturesToLoad, x.Elapsed.TotalSeconds));
            }
        }
    }

    struct MyRwTextureInfo
    {
        // immutable data
        internal Texture1DDescription ? Description1D;
        internal Texture2DDescription ? Description2D;
        internal Resource Resource;
    }

    struct RwTexId
    {
        internal int Index;

        public static bool operator ==(RwTexId x, RwTexId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(RwTexId x, RwTexId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly RwTexId NULL = new RwTexId { Index = -1 };


        internal Resource Resource { get { return MyRwTextures.GetResource(this); } }
        internal ShaderResourceView ShaderView { get { return MyRwTextures.GetSrv(this); } }
        internal DepthStencilView SubresourceDsv(int subres) { return MyRwTextures.GetDsv(this, subres); }
        internal RenderTargetView SubresourceRtv(int subres) { return MyRwTextures.GetRtv(this, subres); }
        internal ShaderResourceView SubresourceSrv(int subres) { return MyRwTextures.GetSrv(this, subres); }
        internal ShaderResourceView SubresourceSrv(int array, int mipmap) { return MyRwTextures.GetSrv(this, mipmap + array * Description2d.MipLevels); }
        internal UnorderedAccessView Uav { get { return MyRwTextures.GetUav(this); } }
        internal UnorderedAccessView SubresourceUav(int array, int mipmap) { return MyRwTextures.GetUav(this, mipmap + array * Description2d.MipLevels); }
        internal DepthStencilView Dsv { get { return MyRwTextures.GetDsv(this); } }
        internal Texture2DDescription Description2d { get { return MyRwTextures.Textures.Data[Index].Description2D.Value; } }
        internal RenderTargetView Rtv { get { return MyRwTextures.GetRtv(this); } }
    }

    struct MySubresourceId
    {
        internal RwTexId Id;
        internal int Subresource;
    }

    struct MyDsvInfo
    {
        internal DepthStencilViewDescription ? Description;
        internal DepthStencilView View;
    }

    struct MySrvInfo
    {
        internal ShaderResourceViewDescription ? Description;
        internal ShaderResourceView View;
    }

    struct MyRtvInfo
    {
        internal RenderTargetViewDescription? Description;
        internal RenderTargetView View;
    }

    struct MyUavInfo
    {
        internal UnorderedAccessViewDescription? Description;
        internal UnorderedAccessView View;
    }

    static class MyRwTextures
    {
    
        static HashSet<RwTexId> Index = new HashSet<RwTexId>();
        internal static MyFreelist<MyRwTextureInfo> Textures = new MyFreelist<MyRwTextureInfo>(128);
        static Dictionary<RwTexId, MySrvInfo> Srvs = new Dictionary<RwTexId,MySrvInfo>();
        static Dictionary<RwTexId, MyUavInfo> Uavs = new Dictionary<RwTexId, MyUavInfo>();
        static Dictionary<RwTexId, MyDsvInfo> Dsvs = new Dictionary<RwTexId, MyDsvInfo>();
        static Dictionary<RwTexId, MyRtvInfo> Rtvs = new Dictionary<RwTexId, MyRtvInfo>();
        static Dictionary<MySubresourceId, MyDsvInfo> SubresourceDsvs = new Dictionary<MySubresourceId,MyDsvInfo>();
        static Dictionary<MySubresourceId, MyRtvInfo> SubresourceRtvs = new Dictionary<MySubresourceId, MyRtvInfo>();
        static Dictionary<MySubresourceId, MySrvInfo> SubresourceSrvs = new Dictionary<MySubresourceId, MySrvInfo>();
        static Dictionary<MySubresourceId, MyUavInfo> SubresourceUavs = new Dictionary<MySubresourceId, MyUavInfo>();

        internal static ShaderResourceView GetSrv(RwTexId id)
        {
            return Srvs[id].View;
        }

        internal static UnorderedAccessView GetUav(RwTexId id)
        {
            return Uavs[id].View;
        }

        internal static DepthStencilView GetDsv(RwTexId id)
        {
            return Dsvs[id].View;
        }
        internal static RenderTargetView GetRtv(RwTexId id)
        {
            return Rtvs[id].View;
        }

        internal static DepthStencilView GetDsv(RwTexId id, int subres)
        {
            return SubresourceDsvs[new MySubresourceId { Id = id, Subresource = subres }].View;
        }

        internal static RenderTargetView GetRtv(RwTexId id, int subres)
        {
            return SubresourceRtvs[new MySubresourceId { Id = id, Subresource = subres }].View;
        }

        internal static ShaderResourceView GetSrv(RwTexId id, int subres)
        {
            return SubresourceSrvs[new MySubresourceId { Id = id, Subresource = subres }].View;
        }

        internal static UnorderedAccessView GetUav(RwTexId id, int subres)
        {
            return SubresourceUavs[new MySubresourceId { Id = id, Subresource = subres }].View;
        }

        internal static Resource GetResource(RwTexId id)
        {
            return Textures.Data[id.Index].Resource;
        }

        internal static RwTexId CreateRenderTarget(int width, int height, Format fmt, bool mipmapAutogen = false)
        {
            var desc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = fmt,
                Height = height,
                Width = width,
                MipLevels = mipmapAutogen ? 0 : 1,
                OptionFlags = mipmapAutogen ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc };
            Textures.Data[handle.Index].Resource = new Texture2D(MyRender11.Device, desc);

            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Rtvs[handle] = new MyRtvInfo { Description = null, View = new RenderTargetView(MyRender11.Device, Textures.Data[handle.Index].Resource) };

            Index.Add(handle);

            return handle;
        }

        internal static RwTexId CreateDynamicTexture(int width, int height, Format fmt)
        {
            var desc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                Format = fmt,
                Height = height,
                Width = width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc };
            Textures.Data[handle.Index].Resource = new Texture2D(MyRender11.Device, desc);

            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Index.Add(handle);

            return handle;
        }

        internal static RwTexId CreateShadowmap(int width, int height)
        {
            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = Format.R24G8_Typeless;
            desc.ArraySize = 1;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.OptionFlags = 0;

            var handle = new RwTexId { Index = Textures.Allocate() };
            var res = new Texture2D(MyRender11.Device, desc);
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc, Resource = res };
            Index.Add(handle);

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription();
            srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
            srvDesc.Format = Format.R24_UNorm_X8_Typeless;
            srvDesc.Texture2D.MipLevels = -1;
            srvDesc.Texture2D.MostDetailedMip = 0;
            Srvs[handle] = new MySrvInfo { Description = srvDesc, View = new ShaderResourceView(MyRender11.Device, res, srvDesc) };

            DepthStencilViewDescription dsvDesc = new DepthStencilViewDescription();
            dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
            dsvDesc.Format = Format.D24_UNorm_S8_UInt;
            dsvDesc.Flags = DepthStencilViewFlags.None;
            dsvDesc.Texture2D.MipSlice = 0;
            Dsvs[handle] = new MyDsvInfo { Description = dsvDesc, View = new DepthStencilView(MyRender11.Device, res, dsvDesc) };

            return handle;
        }

        internal static RwTexId CreateShadowmapArray(int width, int height, int arraySize, 
            Format resourceFormat = Format.R24G8_Typeless, 
            Format depthFormat = Format.D24_UNorm_S8_UInt, 
            Format? viewFormat = Format.R24_UNorm_X8_Typeless, 
            string debugName = null)
        {
            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = resourceFormat;
            desc.ArraySize = arraySize;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.DepthStencil;
            if (viewFormat.HasValue)
            {
                desc.BindFlags |= BindFlags.ShaderResource;
            }
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.OptionFlags = 0;

            var handle = new RwTexId { Index = Textures.Allocate() };
            var res = new Texture2D(MyRender11.Device, desc);
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc, Resource = res };
            Index.Add(handle);

            var srvDesc = new ShaderResourceViewDescription();
            if(viewFormat.HasValue)
            {
                srvDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
                srvDesc.Format = viewFormat.Value;
                srvDesc.Texture2DArray.MipLevels = -1;
                srvDesc.Texture2DArray.MostDetailedMip = 0;
                srvDesc.Texture2DArray.ArraySize = arraySize;
                srvDesc.Texture2DArray.FirstArraySlice = 0;
                Srvs[handle] = new MySrvInfo { Description = srvDesc, View = new ShaderResourceView(MyRender11.Device, res, srvDesc) };
            }

            var dsvDesc = new DepthStencilViewDescription();
            dsvDesc.Dimension = DepthStencilViewDimension.Texture2DArray;
            dsvDesc.Format = depthFormat;
            dsvDesc.Flags = DepthStencilViewFlags.None;
            dsvDesc.Texture2DArray.MipSlice = 0;
            dsvDesc.Texture2DArray.ArraySize = 1;

            srvDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
            srvDesc.Format = viewFormat.Value;
            srvDesc.Texture2DArray.MipLevels = -1;
            srvDesc.Texture2DArray.MostDetailedMip = 0;
            srvDesc.Texture2DArray.ArraySize = 1;
            for (int i = 0; i < arraySize; i++)
            {
                dsvDesc.Texture2DArray.FirstArraySlice = i;

                SubresourceDsvs[new MySubresourceId { Id = handle, Subresource = i }] = new MyDsvInfo
                {
                    Description = dsvDesc,
                    View = new DepthStencilView(MyRender11.Device, res, dsvDesc)
                };

                srvDesc.Texture2DArray.FirstArraySlice = i;

                SubresourceSrvs[new MySubresourceId { Id = handle, Subresource = i }] = new MySrvInfo
                {
                    Description = srvDesc,
                    View = new ShaderResourceView(MyRender11.Device, res, srvDesc)
                };
            }

            return handle;
        }

        internal static RwTexId CreateUav1D(int width, Format resourceFormat, string debugName = null) {
            var desc = new Texture1DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                MipLevels = 1,
                Usage = ResourceUsage.Default,
                Width = width
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description1D = desc };
            Textures.Data[handle.Index].Resource = new Texture1D(MyRender11.Device, desc);

            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Uavs[handle] = new MyUavInfo { Description = null, View = new UnorderedAccessView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Index.Add(handle);

            return handle;
        }

        internal static RwTexId CreateUav2D(int width, int height, Format resourceFormat, string debugName = null)
        {
            var desc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                MipLevels = 1,
                Usage = ResourceUsage.Default,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0)
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc };
            Textures.Data[handle.Index].Resource = new Texture2D(MyRender11.Device, desc);

            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Uavs[handle] = new MyUavInfo { Description = null, View = new UnorderedAccessView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Index.Add(handle);

            return handle;
        }

        internal static RwTexId CreateScratch2D(int width, int height, Format resourceFormat, int samplesCount, int samplesQuality, string debugName = null)
        {
            var desc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                MipLevels = 1,
                Usage = ResourceUsage.Default,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(samplesCount, samplesQuality)
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc };
            Textures.Data[handle.Index].Resource = new Texture2D(MyRender11.Device, desc);

            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Index.Add(handle);

            return handle;
        }

        // has 6 rtv subresources and mipLevels * 6 srv/uav subresources
        internal static RwTexId CreateCubemap(int resolution, Format resourceFormat, string debugName = null)
        {
            int mipLevels = 1;
            while ((resolution >> mipLevels) > 0)
            {
                ++mipLevels;
            }

            var desc = new Texture2DDescription
            {
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                MipLevels = mipLevels,
                Usage = ResourceUsage.Default,
                Width = resolution,
                Height = resolution,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription = new SampleDescription(1, 0)
            };

            var handle = new RwTexId { Index = Textures.Allocate() };
            Textures.Data[handle.Index] = new MyRwTextureInfo { Description2D = desc };
            Textures.Data[handle.Index].Resource = new Texture2D(MyRender11.Device, desc);

            var res = Textures.Data[handle.Index].Resource;
            Srvs[handle] = new MySrvInfo { Description = null, View = new ShaderResourceView(MyRender11.Device, Textures.Data[handle.Index].Resource) };
            Index.Add(handle);

            var srvDesc = new ShaderResourceViewDescription();
            srvDesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
            srvDesc.Format = resourceFormat;
            srvDesc.Texture2DArray.MipLevels = 1;
            srvDesc.Texture2DArray.ArraySize = 1;

            var uavDesc = new UnorderedAccessViewDescription();
            uavDesc.Dimension = UnorderedAccessViewDimension.Texture2DArray;
            uavDesc.Format = resourceFormat;
            uavDesc.Texture2DArray.ArraySize = 1;

            for (int m = 0; m < mipLevels; ++m)
            {
                for (int i = 0; i < 6; i++)
                {
                    var subresource = i * mipLevels + m;

                    srvDesc.Texture2DArray.FirstArraySlice = i;
                    srvDesc.Texture2DArray.MostDetailedMip = m;

                    SubresourceSrvs[new MySubresourceId { Id = handle, Subresource = subresource }] = new MySrvInfo
                    {
                        Description = srvDesc,
                        View = new ShaderResourceView(MyRender11.Device, res, srvDesc)
                    };

                    uavDesc.Texture2DArray.FirstArraySlice = i;
                    uavDesc.Texture2DArray.MipSlice = m;

                    SubresourceUavs[new MySubresourceId { Id = handle, Subresource = subresource }] = new MyUavInfo
                    {
                        Description = uavDesc,
                        View = new UnorderedAccessView(MyRender11.Device, res, uavDesc)
                    };
                }
            }

            var rtvDesc = new RenderTargetViewDescription();
            rtvDesc.Dimension = RenderTargetViewDimension.Texture2DArray;
            rtvDesc.Format = resourceFormat;
            for (int i = 0; i < 6; i++)
            {
                rtvDesc.Texture2DArray.MipSlice = 0;

                rtvDesc.Texture2DArray.FirstArraySlice = i;
                rtvDesc.Texture2DArray.ArraySize = 1;

                SubresourceRtvs[new MySubresourceId { Id = handle, Subresource = i }] = new MyRtvInfo
                {
                    Description = rtvDesc,
                    View = new RenderTargetView(MyRender11.Device, res, rtvDesc)
                };
            }
                

            return handle;
        }

        internal static void Destroy(ref RwTexId id)
        {
            Destroy(id);
            id = RwTexId.NULL;
        }

        internal static void Destroy(RwTexId id)
        {
            if(Srvs.ContainsKey(id))
            {
                if (Srvs[id].View != null)
                {
                    Srvs[id].View.Dispose();
                }
                Srvs.Remove(id);
            }

            if (Uavs.ContainsKey(id))
            {
                if (Uavs[id].View != null)
                {
                    Uavs[id].View.Dispose();
                }
                Uavs.Remove(id);
            }

            if (Dsvs.ContainsKey(id))
            {
                if (Dsvs[id].View != null)
                {
                    Dsvs[id].View.Dispose();
                }
                Dsvs.Remove(id);
            }

            if (Rtvs.ContainsKey(id))
            {
                if (Rtvs[id].View != null)
                {
                    Rtvs[id].View.Dispose();
                }
                Rtvs.Remove(id);
            }

            // not very fast, but this function should be called rarely, and number of rw resources is rather limited
            var srvToRemove = new List<MySubresourceId>();
            var dsvToRemove = new List<MySubresourceId>();
            var rtvToRemove = new List<MySubresourceId>();
            var uavToRemove = new List<MySubresourceId>();
            foreach (var kv in SubresourceSrvs)
            {
                if (kv.Key.Id == id)
                {
                    kv.Value.View.Dispose();
                    srvToRemove.Add(kv.Key);
                }
            }
            foreach(var kv in SubresourceDsvs)
            {
                if(kv.Key.Id == id)
                {
                    kv.Value.View.Dispose();
                    dsvToRemove.Add(kv.Key);
                }
            }
            foreach (var kv in SubresourceRtvs)
            {
                if (kv.Key.Id == id)
                {
                    kv.Value.View.Dispose();
                    rtvToRemove.Add(kv.Key);
                }
            }
            foreach (var kv in SubresourceUavs)
            {
                if (kv.Key.Id == id)
                {
                    kv.Value.View.Dispose();
                    uavToRemove.Add(kv.Key);
                }
            }
            foreach (var k in srvToRemove)
            {
                SubresourceSrvs.Remove(k);
            }
            foreach(var k in dsvToRemove)
            {
                SubresourceDsvs.Remove(k);
            }
            foreach (var k in rtvToRemove)
            {
                SubresourceRtvs.Remove(k);
            }
            foreach (var k in uavToRemove)
            {
                SubresourceUavs.Remove(k);
            }

            if (Textures.Data[id.Index].Resource != null)
            {
                Textures.Data[id.Index].Resource.Dispose();
                Textures.Data[id.Index].Resource = null;
            }

            Textures.Free(id.Index);
        }

        internal static void Init()
        {

        }

        internal static void OnDeviceEnd()
        {
            foreach(var id in Index)
            {
                if(Textures.Data[id.Index].Resource != null)
                {
                    Textures.Data[id.Index].Resource.Dispose();
                    Textures.Data[id.Index].Resource = null;
                }
            }

            foreach(var key in Srvs.Keys.ToArray())
            {
                var view = Srvs[key];
                if(view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                Srvs[key] = view;
            }

            foreach (var key in Uavs.Keys.ToArray())
            {
                var view = Uavs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                Uavs[key] = view;
            }

            foreach (var key in Dsvs.Keys.ToArray())
            {
                var view = Dsvs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                Dsvs[key] = view;
            }

            foreach (var key in Rtvs.Keys.ToArray())
            {
                var view = Rtvs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                Rtvs[key] = view;
            }

            foreach (var key in SubresourceSrvs.Keys.ToArray())
            {
                var view = SubresourceSrvs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                SubresourceSrvs[key] = view;
            }

            foreach (var key in SubresourceDsvs.Keys.ToArray())
            {
                var view = SubresourceDsvs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                SubresourceDsvs[key] = view;
            }

            foreach (var key in SubresourceRtvs.Keys.ToArray())
            {
                var view = SubresourceRtvs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                SubresourceRtvs[key] = view;
            }

            foreach (var key in SubresourceUavs.Keys.ToArray())
            {
                var view = SubresourceUavs[key];
                if (view.View != null)
                {
                    view.View.Dispose();
                    view.View = null;
                }
                SubresourceUavs[key] = view;
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();


            foreach (var id in Index)
            {
                if (Textures.Data[id.Index].Description2D.HasValue)
                {
                    Textures.Data[id.Index].Resource = new Texture2D(MyRender11.Device, Textures.Data[id.Index].Description2D.Value);
                }
                if (Textures.Data[id.Index].Description1D.HasValue)
                {
                    Textures.Data[id.Index].Resource = new Texture1D(MyRender11.Device, Textures.Data[id.Index].Description1D.Value);
                }
            }

            foreach (var kv in Srvs.ToArray())
            {
                var view = Srvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new ShaderResourceView(MyRender11.Device, kv.Key.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new ShaderResourceView(MyRender11.Device, kv.Key.Resource);
                }
                Srvs[kv.Key] = view;
            }

            foreach (var kv in Uavs.ToArray())
            {
                var view = Uavs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new UnorderedAccessView(MyRender11.Device, kv.Key.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new UnorderedAccessView(MyRender11.Device, kv.Key.Resource);
                }
                Uavs[kv.Key] = view;
            }

            foreach (var kv in Dsvs.ToArray())
            {
                var view = Dsvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new DepthStencilView(MyRender11.Device, kv.Key.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new DepthStencilView(MyRender11.Device, kv.Key.Resource);
                }
                Dsvs[kv.Key] = view;
            }

            foreach (var kv in Rtvs.ToArray())
            {
                var view = Rtvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new RenderTargetView(MyRender11.Device, kv.Key.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new RenderTargetView(MyRender11.Device, kv.Key.Resource);
                }
                Rtvs[kv.Key] = view;
            }

            foreach (var kv in SubresourceSrvs.ToArray())
            {
                var view = SubresourceSrvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new ShaderResourceView(MyRender11.Device, kv.Key.Id.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new ShaderResourceView(MyRender11.Device, kv.Key.Id.Resource);
                }
                SubresourceSrvs[kv.Key] = view;
            }

            foreach (var kv in SubresourceDsvs.ToArray())
            {
                var view = SubresourceDsvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new DepthStencilView(MyRender11.Device, kv.Key.Id.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new DepthStencilView(MyRender11.Device, kv.Key.Id.Resource);
                }
                SubresourceDsvs[kv.Key] = view;
            }

            foreach (var kv in SubresourceRtvs.ToArray())
            {
                var view = SubresourceRtvs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new RenderTargetView(MyRender11.Device, kv.Key.Id.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new RenderTargetView(MyRender11.Device, kv.Key.Id.Resource);
                }
                SubresourceRtvs[kv.Key] = view;
            }

            foreach (var kv in SubresourceUavs.ToArray())
            {
                var view = SubresourceUavs[kv.Key];
                if (view.Description.HasValue)
                {
                    view.View = new UnorderedAccessView(MyRender11.Device, kv.Key.Id.Resource, view.Description.Value);
                }
                else
                {
                    view.View = new UnorderedAccessView(MyRender11.Device, kv.Key.Id.Resource);
                }
                SubresourceUavs[kv.Key] = view;
            }
        }
    }

    
}       
        