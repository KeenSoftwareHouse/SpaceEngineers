using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using SharpDX.D3DCompiler;
using VRage.FileSystem;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Import;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using System.IO;
using VRage.Render11.RenderContext;
using VRageMath.PackedVector;
using VRageRender.Import;

namespace VRageRender
{
    struct MyMeshMaterialId
    {
        public class CustomMergingEqualityComparer : IEqualityComparer<MyMeshMaterialId>
        {
            public bool Equals(MyMeshMaterialId x, MyMeshMaterialId y)
            {
                //return x == y;

                var first = x.Info;
                var second = y.Info;

                return
                    first.ColorMetal_Texture == second.ColorMetal_Texture
                    && first.NormalGloss_Texture == second.NormalGloss_Texture
                    && first.Alphamask_Texture == second.Alphamask_Texture
                    && first.Extensions_Texture == second.Extensions_Texture
                    && first.TextureTypes == second.TextureTypes
                    && first.Technique == second.Technique
                    && first.Facing == second.Facing;
            }

            public int GetHashCode(MyMeshMaterialId obj)
            {
                return 1;
            }
        }

        internal int Index;

        public static bool operator ==(MyMeshMaterialId x, MyMeshMaterialId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MyMeshMaterialId x, MyMeshMaterialId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MyMeshMaterialId NULL = new MyMeshMaterialId { Index = -1 };

        internal MyMeshMaterialInfo Info { get { return MyMeshMaterials1.Table[Index]; } }
    }

    struct MyGeometryTextureSystemReference
    {
        public bool IsUsed;

        public IDynamicFileArrayTexture ColorMetalTexture;
        public IDynamicFileArrayTexture NormalGlossTexture;
        public IDynamicFileArrayTexture ExtensionTexture;
        public IDynamicFileArrayTexture AlphamaskTexture;

        public int ColorMetalIndex;
        public int NormalGlossIndex;
        public int ExtensionIndex;
        public int AlphamaskIndex;

        public Vector4I TextureSliceIndices
        {
            get { return new Vector4I(ColorMetalIndex, NormalGlossIndex, ExtensionIndex, AlphamaskIndex); }
            set
            {
                ColorMetalIndex = value.X;
                NormalGlossIndex = value.Y;
                ExtensionIndex = value.Z;
                AlphamaskIndex = value.W;
            }
        }
    }

    struct MyMeshMaterialInfo
    {
        internal MyMeshMaterialId Id; // key - direct index, out 
        internal int RepresentationKey; // key - external ref, out 
        internal MyStringId Name;
        internal MyFileTextureEnum TextureTypes;
        internal string ColorMetal_Texture;
        internal string NormalGloss_Texture;
        internal string Extensions_Texture;
        internal string Alphamask_Texture;
        public MyGeometryTextureSystemReference GeometryTextureRef;
        internal MyMeshDrawTechnique Technique;
        internal MyFacingEnum Facing;
        internal Vector2 WindScaleAndFreq;

        internal static void RequestResources(ref MyMeshMaterialInfo info)
        {
            if (!info.GeometryTextureRef.IsUsed)
            {
                MyFileTextureManager texManager = MyManagers.FileTextures;
                texManager.GetTexture(info.ColorMetal_Texture, MyFileTextureEnum.COLOR_METAL, false,
                    info.Facing == MyFacingEnum.Impostor);
                texManager.GetTexture(info.NormalGloss_Texture, MyFileTextureEnum.NORMALMAP_GLOSS);
                texManager.GetTexture(info.Extensions_Texture, MyFileTextureEnum.EXTENSIONS);
                texManager.GetTexture(info.Alphamask_Texture, MyFileTextureEnum.ALPHAMASK);
            }
            else
            {
                if (info.GeometryTextureRef.ColorMetalTexture != null)
                    info.GeometryTextureRef.ColorMetalTexture.Update();
                if (info.GeometryTextureRef.NormalGlossTexture != null)
                    info.GeometryTextureRef.NormalGlossTexture.Update();
                if (info.GeometryTextureRef.ExtensionTexture != null)
                    info.GeometryTextureRef.ExtensionTexture.Update();
                if (info.GeometryTextureRef.AlphamaskTexture != null)
                    info.GeometryTextureRef.AlphamaskTexture.Update();
            }
        }

        internal static MyMaterialProxy_2 CreateProxy(ref MyMeshMaterialInfo info)
        {
            ISrvBindable A, B, C, D;
            if (!info.GeometryTextureRef.IsUsed)
            {
                MyFileTextureManager texManager = MyManagers.FileTextures;
                A = texManager.GetTexture(info.ColorMetal_Texture, MyFileTextureEnum.COLOR_METAL);
                B = texManager.GetTexture(info.NormalGloss_Texture, MyFileTextureEnum.NORMALMAP_GLOSS);
                C = texManager.GetTexture(info.Extensions_Texture, MyFileTextureEnum.EXTENSIONS);
                D = texManager.GetTexture(info.Alphamask_Texture, MyFileTextureEnum.ALPHAMASK);
            }
            else
            {
                A = info.GeometryTextureRef.ColorMetalTexture;
                B = info.GeometryTextureRef.NormalGlossTexture;
                C = info.GeometryTextureRef.ExtensionTexture;
                D = info.GeometryTextureRef.AlphamaskTexture;
            }
            var materialSrvs = new MySrvTable
                    {
                        BindFlag = MyBindFlag.BIND_PS,
                        StartSlot = 0,
                        Srvs = new ISrvBindable[] { A, B, C, D },
                        Version = info.Id.GetHashCode()
                    };
            return
                new MyMaterialProxy_2 { MaterialSrvs = materialSrvs };
        }
    }

    class MyMeshMaterials1
    {
        #region DATA
        static MyFreelist<MyMeshMaterialInfo> MaterialsPool = new MyFreelist<MyMeshMaterialInfo>(256);

        internal static MyMeshMaterialInfo[] Table { get { return MaterialsPool.Data; } }

        static Dictionary<MyMeshMaterialId, MyMaterialProxyId> MaterialProxyIndex = new Dictionary<MyMeshMaterialId, MyMaterialProxyId>();
        internal static Dictionary<int, MyMeshMaterialId> MaterialRkIndex = new Dictionary<int, MyMeshMaterialId>();

        static Dictionary<MyStringId, MyMeshMaterialId> MaterialNameIndex = new Dictionary<MyStringId, MyMeshMaterialId>(MyStringId.Comparer); // only for uniquely named materials! used by destruction models

        internal static HashSet<int> MergableRKs = new HashSet<int>();

        static List<MyMeshMaterialId> MaterialQueryResourcesTable = new List<MyMeshMaterialId>();
        #endregion

        internal static MyMeshMaterialId DebugMaterialId;
        internal static MyMeshMaterialId NullMaterialId;

        static readonly HashSet<MyStringId> MERGABLE_MATERIAL_NAMES;
        static MyMeshMaterials1()
        {
            MERGABLE_MATERIAL_NAMES = new HashSet<MyStringId>(MyStringId.Comparer);
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("BlockSheet"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("CubesSheet"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("CubesMetalSheet"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("RoofSheet"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("StoneSheet"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("House_Texture"));
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("RoofSheetRound"));
        }

        internal static bool IsMergable(MyMeshMaterialId matId)
        {
            return MergableRKs.Contains(Table[matId.Index].RepresentationKey);
        }

        internal static MyMeshMaterialId GetMaterialId(string name)
        {
            return MaterialNameIndex.Get(X.TEXT_(name));
        }

        internal static MyMaterialProxyId GetProxyId(MyMeshMaterialId id)
        {
            if (MaterialProxyIndex.ContainsKey(id))
            {
                return MaterialProxyIndex[id];
            }

            MyRender11.Log.WriteLine("MeshMaterialId missing");

            return MaterialProxyIndex[DebugMaterialId];
        }

        internal static int CalculateRK(ref MyMeshMaterialInfo desc)
        {
            var key = desc.ColorMetal_Texture.GetHashCode();
            MyHashHelper.Combine(ref key, desc.NormalGloss_Texture.GetHashCode());
            MyHashHelper.Combine(ref key, desc.Extensions_Texture.GetHashCode());
            MyHashHelper.Combine(ref key, desc.Alphamask_Texture.GetHashCode());
            MyHashHelper.Combine(ref key, desc.Technique.GetHashCode());
            MyHashHelper.Combine(ref key, desc.Name.GetHashCode());
            return key;
        }

        internal static MyMeshMaterialId GetMaterialId(ref MyMeshMaterialInfo desc, string assetFile = null)
        {
            var rk = CalculateRK(ref desc);

            if (!MaterialRkIndex.ContainsKey(rk))
            {
                var id = MaterialRkIndex[rk] = new MyMeshMaterialId { Index = MaterialsPool.Allocate() };

                desc.Id = id;
                desc.RepresentationKey = rk;

                MaterialsPool.Data[id.Index] = desc;
                MaterialProxyIndex[id] = MyMaterials1.AllocateProxy();

                MaterialQueryResourcesTable.Add(id);

                MaterialNameIndex[desc.Name] = id;

                if (MERGABLE_MATERIAL_NAMES.Contains(desc.Name))
                {
                    MergableRKs.Add(desc.RepresentationKey);
                }

                else if (assetFile != null)
                {
                    VRageRender.MyRender11.Log.WriteLine(String.Format("Asset {0} tries to overrwrite material {1} with different textures", assetFile, desc.Name.ToString()));
                }

                return id;
            }

            return MaterialRkIndex[rk];
        }

        internal static MyMeshMaterialId GetMaterialId(string name, string contentPath, string colorMetalTexture, string normalGlossTexture, string extensionTexture, MyMeshDrawTechnique technique)
        {
            MyMeshMaterialInfo desc;
            desc = new MyMeshMaterialInfo
            {
                Name = X.TEXT_(name),
                ColorMetal_Texture = MyResourceUtils.GetTextureFullPath(colorMetalTexture, contentPath),
                NormalGloss_Texture = MyResourceUtils.GetTextureFullPath(normalGlossTexture, contentPath),
                Extensions_Texture = MyResourceUtils.GetTextureFullPath(extensionTexture, contentPath),
                Alphamask_Texture = String.Empty,
                Technique = technique,
                TextureTypes = GetMaterialTextureTypes(colorMetalTexture, normalGlossTexture, extensionTexture, null),
                Facing = MyFacingEnum.None,
            };

            return GetMaterialId(ref desc);
        }

        public static MyMeshMaterialInfo ConvertImportDescToMeshMaterialInfo(MyMaterialDescriptor importDesc,
            string contentPath, string assetFile = null)
        {
            string colorMetalTexture = importDesc.Textures.Get("ColorMetalTexture", "");
            string normalGlossTexture = importDesc.Textures.Get("NormalGlossTexture", "");
            string extensionTexture = importDesc.Textures.Get("AddMapsTexture", "");
            string alphamaskTexture = importDesc.Textures.Get("AlphamaskTexture", null);

            MyMeshMaterialInfo info = new MyMeshMaterialInfo
            {
                Name = X.TEXT_(importDesc.MaterialName),
                ColorMetal_Texture = MyResourceUtils.GetTextureFullPath(colorMetalTexture, contentPath),
                NormalGloss_Texture = MyResourceUtils.GetTextureFullPath(normalGlossTexture, contentPath),
                Extensions_Texture = MyResourceUtils.GetTextureFullPath(extensionTexture, contentPath),
                Alphamask_Texture = MyResourceUtils.GetTextureFullPath(alphamaskTexture, contentPath),
                TextureTypes = GetMaterialTextureTypes(colorMetalTexture, normalGlossTexture, extensionTexture, alphamaskTexture),
                Technique = importDesc.TechniqueEnum,
                Facing = importDesc.Facing,
                WindScaleAndFreq = importDesc.WindScaleAndFreq
            };
            return info;
        }

        internal static MyMeshMaterialId GetMaterialId(MyMaterialDescriptor importDesc, string contentPath, string assetFile = null)
        {
            MyMeshMaterialInfo info;
            if (importDesc != null)
            {
                info = ConvertImportDescToMeshMaterialInfo(importDesc, contentPath, assetFile);
            }
            else
            {
                return NullMaterialId;
            }

            return GetMaterialId(ref info, assetFile);
        }

        internal static void OnResourcesRequesting()
        {
            foreach (var id in MaterialQueryResourcesTable)
            {
                // ask for resources
                MyMeshMaterialInfo.RequestResources(ref MaterialsPool.Data[id.Index]);
            }
        }

        internal static void OnResourcesGathering()
        {
            if (MaterialQueryResourcesTable.Count > 0)
            {
                // update proxies foreach material
                foreach (var id in MaterialQueryResourcesTable)
                {
                    MyMaterials1.ProxyPool.Data[MaterialProxyIndex[id].Index] = MyMeshMaterialInfo.CreateProxy(ref MaterialsPool.Data[id.Index]);
                }
            }

            MaterialQueryResourcesTable.Clear();
        }

        internal static void CreateCommonMaterials()
        {
            var nullMatDesc = new MyMeshMaterialInfo
            {
                Name = X.TEXT_("__NULL_MATERIAL"),
                ColorMetal_Texture = string.Empty,
                NormalGloss_Texture = string.Empty,
                Extensions_Texture = string.Empty,
                Alphamask_Texture = string.Empty,
                Technique = MyMeshDrawTechnique.MESH
            };
            NullMaterialId = GetMaterialId(ref nullMatDesc);

            var debugMatDesc = new MyMeshMaterialInfo
            {
                Name = X.TEXT_("__DEBUG_MATERIAL"),
                ColorMetal_Texture = MyRender11.DebugMode ? "PINK" : string.Empty,
                NormalGloss_Texture = string.Empty,
                Extensions_Texture = string.Empty,
                Alphamask_Texture = string.Empty,
                Technique = MyMeshDrawTechnique.MESH
            };
            DebugMaterialId = GetMaterialId(ref debugMatDesc);
        }

        internal static void Init()
        {
            //MyCallbacks.RegisterResourceRequestListener(new OnResourceRequestDelegate(OnResourcesRequesting));
            //MyCallbacks.RegisterResourceGatherListener(new OnResourceGatherDelegate(OnResourcesGathering));

            CreateCommonMaterials();
        }

        internal static void OnDeviceReset()
        {
            InvalidateMaterials();
        }

        internal static void InvalidateMaterial(MyMeshMaterialId id)
        {
            MaterialQueryResourcesTable.Add(id);
        }

        internal static void InvalidateMaterials()
        {
            foreach (var id in MaterialRkIndex.Values)
            {
                MaterialQueryResourcesTable.Add(id);
            }
        }

        internal static void OnSessionEnd()
        {
            MergableRKs.Clear();
            MaterialQueryResourcesTable.Clear();
            MaterialRkIndex.Clear();
            MaterialsPool.Clear();
            MaterialProxyIndex.Clear();
            MaterialNameIndex.Clear();

            CreateCommonMaterials();
        }

        public static MyFileTextureEnum GetMaterialTextureTypes(string colorMetalTexture,
            string normalGlossTexture, string extensionTexture, string alphamaskTexture)
        {
            MyFileTextureEnum ret = MyFileTextureEnum.UNSPECIFIED;
            if (!string.IsNullOrEmpty(colorMetalTexture))
                ret |= MyFileTextureEnum.COLOR_METAL;

            if (!string.IsNullOrEmpty(normalGlossTexture))
                ret |= MyFileTextureEnum.NORMALMAP_GLOSS;

            if (!string.IsNullOrEmpty(extensionTexture))
                ret |= MyFileTextureEnum.EXTENSIONS;

            if (!string.IsNullOrEmpty(alphamaskTexture))
                ret |= MyFileTextureEnum.ALPHAMASK;

            return ret;
        }

        /// <summary>Get macro bundles for texture types</summary>
        public static ShaderMacro[] GetMaterialTextureMacros(MyFileTextureEnum textures)
        {
            const string USE_COLORMETAL_TEXTURE = "USE_COLORMETAL_TEXTURE";
            const string USE_NORMALGLOSS_TEXTURE = "USE_NORMALGLOSS_TEXTURE";
            const string USE_EXTENSIONS_TEXTURE = "USE_EXTENSIONS_TEXTURE";

            List<ShaderMacro> macros = new List<ShaderMacro>();
            if (textures.HasFlag(MyFileTextureEnum.COLOR_METAL))
                macros.Add(new ShaderMacro(USE_COLORMETAL_TEXTURE, null));

            if (textures.HasFlag(MyFileTextureEnum.NORMALMAP_GLOSS))
                macros.Add(new ShaderMacro(USE_NORMALGLOSS_TEXTURE, null));

            if (textures.HasFlag(MyFileTextureEnum.EXTENSIONS))
                macros.Add(new ShaderMacro(USE_EXTENSIONS_TEXTURE, null));

            return macros.ToArray();
        }

        /// <summary>Bind blend states for alpha blending</summary>
        public static void BindMaterialTextureBlendStates(MyRenderContext rc, MyFileTextureEnum textures, bool premultipliedAlpha)
        {
            textures &= ~MyFileTextureEnum.ALPHAMASK;
            switch (textures)
            {
                case MyFileTextureEnum.COLOR_METAL:
                    if (premultipliedAlpha)
                        rc.SetBlendState(MyBlendStateManager.BlendDecalColor);
                    else
                        rc.SetBlendState(MyBlendStateManager.BlendDecalColorNoPremult);
                    break;
                case MyFileTextureEnum.NORMALMAP_GLOSS:
                    if (premultipliedAlpha)
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormal);
                    else
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormalNoPremult);
                    break;
                case MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS:
                    if (premultipliedAlpha)
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormalColor);
                    else
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormalColorNoPremult);
                    break;
                case MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS | MyFileTextureEnum.EXTENSIONS:
                    if (premultipliedAlpha)
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormalColorExt);
                    else
                        rc.SetBlendState(MyBlendStateManager.BlendDecalNormalColorExtNoPremult);
                    break;
                default:
                    throw new Exception("Unknown texture bundle type");
            }
        }
    }

}
