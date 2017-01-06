using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using VRage;
using VRage.FileSystem;
using VRage.Utils;
using VRage.Render11.Resources;
using VRage.Import;
using VRage.Profiler;
using VRageRender.Import;


namespace VRageRender
{
    [Flags]
    enum MyShaderUnifiedFlags
    {
        NONE = 0,
        DEPTH_ONLY = 1 << 0,

        // only one!
        ALPHA_MASKED = 1 << 1,
        TRANSPARENT = 1 << 2,
        DITHERED = 1 << 3,
        USE_SHADOW_CASCADES = 1 << 4,
        ALPHA_MASK_ARRAY = 1 << 5,
        DITHERED_LOD = 1 << 6,
        STATIC_DECAL = 1 << 7,
        STATIC_DECAL_CUTOUT = 1 << 8,

        USE_SKINNING = 1 << 9,
        USE_VOXEL_DATA = 1 << 10,
        USE_VOXEL_MORPHING = 1 << 11,

        // only one!
        USE_CUBE_INSTANCING = 1 << 12,
        USE_DEFORMED_CUBE_INSTANCING = 1 << 13,
        USE_GENERIC_INSTANCING = 1 << 14,
        USE_MERGE_INSTANCING = 1 << 15,

        // only on USE_MERGE_INSTANCING
        USE_SINGLE_INSTANCE = 1 << 16,

        // no restriction
        USE_TEXTURE_INDICES = 1 << 17,
    }

    struct MyMaterialShadersBundleId
    {
        internal int Index;

        public static bool operator ==(MyMaterialShadersBundleId x, MyMaterialShadersBundleId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MyMaterialShadersBundleId x, MyMaterialShadersBundleId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MyMaterialShadersBundleId NULL = new MyMaterialShadersBundleId { Index = -1 };

        internal InputLayout IL { get { return MyMaterialShaders.Bundles[Index].IL; } }
        internal VertexShader VS { get { return MyMaterialShaders.Bundles[Index].VS; } }
        internal PixelShader PS { get { return MyMaterialShaders.Bundles[Index].PS; } }
    }

    struct MyMaterialShadersInfo
    {
        internal MyStringId Material;
        internal MyStringId Pass;
        internal VertexLayoutId Layout;
        internal MyShaderUnifiedFlags Flags;
        internal MyFileTextureEnum TextureTypes;
        internal string Name { get { return String.Format("[{0}][{1}]_{2}", Pass.ToString(), Material.ToString(), Flags); } }
    }

    struct MyMaterialShadersBundle
    {
        internal InputLayout IL;
        internal VertexShader VS;
        internal PixelShader PS;
    }

    struct MyMaterialShaderInfo
    {
        internal string VertexShaderFilename;
        internal string VertexShaderFilepath;
        internal string PixelShaderFilename;
        internal string PixelShaderFilepath;
    }

    static class MyMaterialShaders
    {
        public const string GEOMETRY_FOLDER = "Geometry";

        public const string GBUFFER_PASS = "GBuffer";
        public const string DEPTH_PASS = "Depth";
        public const string FORWARD_PASS = "Forward";
        public const string HIGHLIGHT_PASS = "Highlight";
        public const string FOLIAGE_STREAMING_PASS = "FoliageStreaming";
        public const string STATIC_GLASS_PASS = "StaticGlass";

        public static MyStringId GBUFFER_PASS_ID = X.TEXT_(GBUFFER_PASS);
        public static MyStringId DEPTH_PASS_ID = X.TEXT_(DEPTH_PASS);
        public static MyStringId FORWARD_PASS_ID = X.TEXT_(FORWARD_PASS);
        public static MyStringId HIGHLIGHT_PASS_ID = X.TEXT_(HIGHLIGHT_PASS);
        public static MyStringId FOLIAGE_STREAMING_PASS_ID = X.TEXT_(FOLIAGE_STREAMING_PASS);
        public static MyStringId STATIC_GLASS_PASS_ID = X.TEXT_(STATIC_GLASS_PASS);

        public static MyStringId DEFAULT_MATERIAL_TAG = X.TEXT_("Standard");
        public static MyStringId ALPHA_MASKED_MATERIAL_TAG = X.TEXT_("AlphaMasked");
        public static MyStringId TRIPLANAR_SINGLE_MATERIAL_TAG = X.TEXT_("TriplanarSingle");
        public static MyStringId TRIPLANAR_MULTI_MATERIAL_TAG = X.TEXT_("TriplanarMulti");
        public static MyStringId TRIPLANAR_DEBRIS_MATERIAL_TAG = X.TEXT_("TriplanarDebris");

        internal static void AddMaterialShaderFlagMacrosTo(List<ShaderMacro> list, MyShaderUnifiedFlags flags, MyFileTextureEnum textureTypes = MyFileTextureEnum.UNSPECIFIED)
        {
            if ((flags & MyShaderUnifiedFlags.DEPTH_ONLY) > 0)
                list.Add(new ShaderMacro("DEPTH_ONLY", null));
            if ((flags & MyShaderUnifiedFlags.ALPHA_MASKED) > 0)
                list.Add(new ShaderMacro("ALPHA_MASKED", null));
            if ((flags & MyShaderUnifiedFlags.ALPHA_MASK_ARRAY) > 0)
                list.Add(new ShaderMacro("ALPHA_MASK_ARRAY", null));
            if ((flags & MyShaderUnifiedFlags.TRANSPARENT) > 0)
                list.Add(new ShaderMacro("TRANSPARENT", null));
            if ((flags & MyShaderUnifiedFlags.DITHERED) > 0)
                list.Add(new ShaderMacro("DITHERED", null));
            if ((flags & MyShaderUnifiedFlags.DITHERED_LOD) > 0)
                list.Add(new ShaderMacro("DITHERED_LOD", null));
            if ((flags & MyShaderUnifiedFlags.USE_SKINNING) > 0)
                list.Add(new ShaderMacro("USE_SKINNING", null));
            if ((flags & MyShaderUnifiedFlags.STATIC_DECAL) > 0)
            {
                Debug.Assert(textureTypes != MyFileTextureEnum.UNSPECIFIED);
                list.Add(new ShaderMacro("STATIC_DECAL", null));
                list.AddRange(MyMeshMaterials1.GetMaterialTextureMacros(textureTypes));
            }
            if ((flags & MyShaderUnifiedFlags.STATIC_DECAL_CUTOUT) > 0)
                list.Add(new ShaderMacro("STATIC_DECAL_CUTOUT", null));
            if ((flags & MyShaderUnifiedFlags.USE_CUBE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_CUBE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_DEFORMED_CUBE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_DEFORMED_CUBE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_GENERIC_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_GENERIC_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_MERGE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_MERGE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_SINGLE_INSTANCE) > 0)
            {
                Debug.Assert((flags & MyShaderUnifiedFlags.USE_MERGE_INSTANCING) > 0);
                list.Add(new ShaderMacro("USE_SINGLE_INSTANCE", null));
            }
            if ((flags & MyShaderUnifiedFlags.USE_VOXEL_MORPHING) > 0)
                list.Add(new ShaderMacro("USE_VOXEL_MORPHING", null));
            if ((flags & MyShaderUnifiedFlags.USE_VOXEL_DATA) > 0)
                list.Add(new ShaderMacro("USE_VOXEL_DATA", null));
            if ((flags & MyShaderUnifiedFlags.USE_TEXTURE_INDICES) > 0)
                list.Add(new ShaderMacro("USE_TEXTURE_INDICES", null));
        }

        static Dictionary<MyStringId, MyMaterialShaderInfo> MaterialSources = new Dictionary<MyStringId, MyMaterialShaderInfo>(MyStringId.Comparer);

        static Dictionary<int, MyMaterialShadersBundleId> HashIndex = new Dictionary<int, MyMaterialShadersBundleId>();
        static MyFreelist<MyMaterialShadersInfo> BundleInfo = new MyFreelist<MyMaterialShadersInfo>(64);
        internal static MyMaterialShadersBundle[] Bundles = new MyMaterialShadersBundle[64];

        internal static MyMaterialShadersBundleId Get(MyStringId material, MyStringId materialPass,
            VertexLayoutId vertexLayout, MyShaderUnifiedFlags flags, MyFileTextureEnum textureTypes)
        {
            int hash = 0;
            MyHashHelper.Combine(ref hash, material.GetHashCode());
            MyHashHelper.Combine(ref hash, materialPass.GetHashCode());
            MyHashHelper.Combine(ref hash, vertexLayout.GetHashCode());
            MyHashHelper.Combine(ref hash, unchecked((int)flags));

            if (HashIndex.ContainsKey(hash))
            {
                return HashIndex[hash];
            }

            var id = new MyMaterialShadersBundleId { Index = BundleInfo.Allocate() };
            MyArrayHelpers.Reserve(ref Bundles, id.Index + 1);

            HashIndex[hash] = id;
            BundleInfo.Data[id.Index] = new MyMaterialShadersInfo
            {
                Material = material,
                Pass = materialPass,
                Layout = vertexLayout,
                Flags = flags,
                TextureTypes = textureTypes,
            };
            Bundles[id.Index] = new MyMaterialShadersBundle { };

            InitBundle(id);

            return id;
        }

        private static void ClearSources()
        {
            MaterialSources.Clear();
        }

        internal static void Recompile()
        {
            ClearSources();

            foreach (var id in HashIndex.Values)
            {
                InitBundle(id);
            }
        }

        internal static void GetMaterialSources(MyStringId id, out MyMaterialShaderInfo info)
        {
            if (!MaterialSources.TryGetValue(id, out info))
            {
                info = new MyMaterialShaderInfo();

                info.VertexShaderFilename = Path.Combine(MaterialsFolder, id.ToString(), "Vertex.hlsl"); ;
                info.VertexShaderFilepath = Path.Combine(MyShaders.ShadersPath, info.VertexShaderFilename);
                info.PixelShaderFilename = Path.Combine(MaterialsFolder, id.ToString(), "Pixel.hlsl");
                info.PixelShaderFilepath = Path.Combine(MyShaders.ShadersPath, info.PixelShaderFilename);

                MaterialSources[id] = info;
            }
        }

        static void InitBundle(MyMaterialShadersBundleId id, bool invalidateCache = false)
        {
            var info = BundleInfo.Data[id.Index];

            var macroList = new List<ShaderMacro>();
            macroList.Add(GetRenderingPassMacro(info.Pass.String));
            AddMaterialShaderFlagMacrosTo(macroList, info.Flags, info.TextureTypes);
            macroList.AddRange(info.Layout.Info.Macros);

            ProfilerShort.Begin("MyShaders.MaterialCompile");

            MyMaterialShaderInfo sources;
            GetMaterialSources(info.Material, out sources);

            ShaderMacro[] macros = macroList.ToArray();

            string vsDescriptor = GetShaderDescriptor(sources.VertexShaderFilename, info.Material.String, info.Pass.String, info.Layout);
            byte[] vsBytecode = MyShaders.Compile(sources.VertexShaderFilepath, macros, MyShaderProfile.vs_5_0, vsDescriptor, invalidateCache);

            string psDescriptor = GetShaderDescriptor(sources.PixelShaderFilename, info.Material.String, info.Pass.String, info.Layout);
            byte[] psBytecode = MyShaders.Compile(sources.PixelShaderFilepath, macros, MyShaderProfile.ps_5_0, psDescriptor, invalidateCache);

            ProfilerShort.End();

            // input layous
            bool canChangeBundle = vsBytecode != null && psBytecode != null;
            if (canChangeBundle)
            {
                if (Bundles[id.Index].IL != null)
                {
                    Bundles[id.Index].IL.Dispose();
                    Bundles[id.Index].IL = null;
                }
                if (Bundles[id.Index].VS != null)
                {
                    Bundles[id.Index].VS.Dispose();
                    Bundles[id.Index].VS = null;
                }
                if (Bundles[id.Index].PS != null)
                {
                    Bundles[id.Index].PS.Dispose();
                    Bundles[id.Index].PS = null;
                }

                try
                {
                    Bundles[id.Index].VS = new VertexShader(MyRender11.Device, vsBytecode);
                    Bundles[id.Index].VS.DebugName = vsDescriptor;
                    Bundles[id.Index].PS = new PixelShader(MyRender11.Device, psBytecode);
                    Bundles[id.Index].PS.DebugName = psDescriptor;
                    Bundles[id.Index].IL = info.Layout.Elements.Length > 0 ? new InputLayout(MyRender11.Device, vsBytecode, info.Layout.Elements) : null;
                }
                catch (SharpDXException e)
                {
                    if (!invalidateCache)
                    {
                        InitBundle(id, true);
                        return;
                    }
                    string message = "Failed to initialize material shader" + info.Name + " for vertex " + info.Layout.Info.Components.GetString();
                    MyRender11.Log.WriteLine(message);
                    throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
                }
            }
            else //if (Bundles[id.Index].VS == null && Bundles[id.Index].PS == null)
            {
                string message = "Failed to compile material shader" + info.Name + " for vertex " + info.Layout.Info.Components.GetString();
                MyRender11.Log.WriteLine(message);
                
                if (vsBytecode == null && psBytecode != null)
                    message = "vsByteCode is null, descriptor: " + vsDescriptor;
                else if (vsBytecode != null && psBytecode == null)
                    message = "psByteCode is null, descriptor: " + psDescriptor;
                else
                    message = "vsByteCode and psByteCode are null, vsDescriptor: " + vsDescriptor + "; psDescriptor: " + psDescriptor;
                MyRender11.Log.WriteLine(message);

#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                    ClearSources();
                    InitBundle(id, invalidateCache);
                }
#else
                if (Bundles[id.Index].VS == null && Bundles[id.Index].PS == null)
                    throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
#endif
            }
        }

        public static string GetShaderDescriptor(string shaderFilename, string material, string pass, VertexLayoutId layout)
        {
            return String.Format("{0}, {1}_{2}_{3}", shaderFilename, material, pass, layout.Info.Components.GetString());
        }

        public static string MaterialsFolder
        {
            get { return Path.Combine(GEOMETRY_FOLDER, "Materials"); }
        }

        public static string PassesFolder
        {
            get { return Path.Combine(GEOMETRY_FOLDER, "Passes"); }
        }

        internal static ShaderMacro GetRenderingPassMacro(string pass)
        {
            const string RENDERING_PASS = "RENDERING_PASS";
            switch (pass)
            {
                case GBUFFER_PASS:
                    return new ShaderMacro(RENDERING_PASS, 0);
                case DEPTH_PASS:
                    return new ShaderMacro(RENDERING_PASS, 1);
                case FORWARD_PASS:
                    return new ShaderMacro(RENDERING_PASS, 2);
                case HIGHLIGHT_PASS:
                    return new ShaderMacro(RENDERING_PASS, 3);
                case FOLIAGE_STREAMING_PASS:
                    return new ShaderMacro(RENDERING_PASS, 4);
                case STATIC_GLASS_PASS:
                    return new ShaderMacro(RENDERING_PASS, 5);
                default:
                    throw new Exception();
            }
        }

        internal static MyStringId MapTechniqueToDefaultPass(MyMeshDrawTechnique technique)
        {
            switch (technique)
            {
                case MyMeshDrawTechnique.GLASS:
                    return STATIC_GLASS_PASS_ID;
                default:
                    return GBUFFER_PASS_ID;
            }
        }

        internal static MyStringId MapTechniqueToShaderMaterial(MyMeshDrawTechnique technique)
        {
            switch (technique)
            {
                case MyMeshDrawTechnique.VOXEL_MAP_SINGLE:
                    return TRIPLANAR_SINGLE_MATERIAL_TAG;
                case MyMeshDrawTechnique.VOXEL_MAP_MULTI:
                    return TRIPLANAR_MULTI_MATERIAL_TAG;
                case MyMeshDrawTechnique.VOXELS_DEBRIS:
                    return TRIPLANAR_DEBRIS_MATERIAL_TAG;
                case MyMeshDrawTechnique.ALPHA_MASKED:
                case MyMeshDrawTechnique.FOLIAGE:
                    return ALPHA_MASKED_MATERIAL_TAG;
                default:
                    return DEFAULT_MATERIAL_TAG;
            }
        }

        internal static void OnDeviceEnd()
        {
            foreach (var id in HashIndex.Values)
            {
                if (Bundles[id.Index].IL != null)
                {
                    Bundles[id.Index].IL.Dispose();
                    Bundles[id.Index].IL = null;
                }
                if (Bundles[id.Index].VS != null)
                {
                    Bundles[id.Index].VS.Dispose();
                    Bundles[id.Index].VS = null;
                }
                if (Bundles[id.Index].PS != null)
                {
                    Bundles[id.Index].PS.Dispose();
                    Bundles[id.Index].PS = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            foreach (var id in HashIndex.Values)
            {
                InitBundle(id);
            }
        }
    }
}
