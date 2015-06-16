using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using VRageRender.Resources;
using System.Diagnostics;
using VRage.Import;
using VRage;
using VRage.Library.Utils;
using System.IO;
using VRage.FileSystem;

namespace VRageRender
{
    struct MyMeshMaterialId
    {
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

    struct MyMaterialProxyId
    {
        internal int Index;

        public static bool operator ==(MyMaterialProxyId x, MyMaterialProxyId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MyMaterialProxyId x, MyMaterialProxyId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MyMaterialProxyId NULL = new MyMaterialProxyId { Index = -1 };
    }

    struct MyVoxelMaterialConstants
    {
        internal Vector3 Scales;
    }

    struct MyVoxelMultiMaterialConstants
    {
        internal Vector3 Scales0;
        float padding_0;
        internal Vector3 Scales1;
        float padding_1;
        internal Vector3 Scales2;
        float padding_2;
    }

    class MyVoxelMaterials1
    {
        internal static Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId> MaterialProxyTripleIndex = new Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId>();
        internal static MyVoxelMaterial1[] Table = new MyVoxelMaterial1[0];
        // not hash set but list
        internal static List<int> MaterialQueryResourcesTable = new List<int>();

        internal static void InvalidateMaterials()
        {
            for(int i=0; i<Table.Length; i++)
            {
                MaterialQueryResourcesTable.Add(i);
            }
        }

        internal static void Set(MyRenderVoxelMaterialData[] list)
        {

            Array.Resize(ref Table, list.Length);

            for (int i = 0; i < list.Length; i++)
            {
                // copy data :O

                Table[i].Near.ColorMetalXZnY_Texture  = X.TEXT(list[i].ColorMetalXZnY);
                Table[i].Near.ColorMetalpY_Texture    = X.TEXT(list[i].ColorMetalY);
                Table[i].Near.NormalGlossXZnY_Texture = X.TEXT(list[i].NormalGlossXZnY);
                Table[i].Near.NormalGlosspY_Texture   = X.TEXT(list[i].NormalGlossY);
                Table[i].Near.ExtXZnY_Texture         = X.TEXT(list[i].ExtXZnY);
                Table[i].Near.ExtpY_Texture           = X.TEXT(list[i].ExtY);
                Table[i].Near.TextureScale = list[i].Scale;

                Table[i].Far1.ColorMetalXZnY_Texture = X.TEXT(list[i].ColorMetalXZnYFar1);
                Table[i].Far1.ColorMetalpY_Texture = X.TEXT(list[i].ColorMetalYFar1);
                Table[i].Far1.NormalGlossXZnY_Texture = X.TEXT(list[i].NormalGlossXZnYFar1);
                Table[i].Far1.NormalGlosspY_Texture = X.TEXT(list[i].NormalGlossYFar1);
                Table[i].Far1.ExtXZnY_Texture = X.TEXT(list[i].ExtXZnYFar1);
                Table[i].Far1.ExtpY_Texture = X.TEXT(list[i].ExtYFar1);
                Table[i].Far1.TextureScale            = list[i].ScaleFar1;

                Table[i].Far2.ColorMetalXZnY_Texture = X.TEXT(list[i].ColorMetalXZnYFar2);
                Table[i].Far2.ColorMetalpY_Texture = X.TEXT(list[i].ColorMetalYFar2);
                Table[i].Far2.NormalGlossXZnY_Texture = X.TEXT(list[i].NormalGlossXZnYFar2);
                Table[i].Far2.NormalGlosspY_Texture = X.TEXT(list[i].NormalGlossYFar2);
                Table[i].Far2.ExtXZnY_Texture = X.TEXT(list[i].ExtXZnYFar2);
                Table[i].Far2.ExtpY_Texture = X.TEXT(list[i].ExtYFar2);
                Table[i].Far2.TextureScale = list[i].ScaleFar2;

                Table[i].FoliageArray_Texture = list[i].ExtensionTextureArray1;
                Table[i].FoliageDensity = list[i].ExtensionDensity;
                Table[i].FoliageScale = list[i].ExtensionScale;
                Table[i].FoliageScaleVariation = list[i].ExtensionRandomRescaleMult;

                MaterialQueryResourcesTable.Add(i);
            }
        }

        internal static bool CheckIndices(MyRenderVoxelMaterialData[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Index != i)
                {
                    return false;
                }
            }
            return true;
        }

        internal static void RebuildMaterialFoliageTable()
        {
            var array = new MaterialFoliageConstantsElem[256];
            int N = Table.Length;
            for (int i = 0; i < N; i++)
            {
                var arrayTexId = MyTextures.GetTexture(Table[i].FoliageArray_Texture, MyTextureEnum.COLOR_METAL, true);

                array[i] = new MaterialFoliageConstantsElem {
                    Scale = Table[i].FoliageScale, 
                    ScaleVar = Table[i].FoliageScaleVariation,
                    TexturesNum = (uint)((Texture2D)MyTextures.Textures.Data[arrayTexId.Index].Resource).Description.ArraySize
                };
            }
            var mapping = MyMapping.MapDiscard(MyCommon.MaterialFoliageTableConstants);
            mapping.stream.WriteRange(array, 0, N);
            mapping.Unmap();
        }

        internal static MyMaterialProxyId GetMaterialProxyId(MyVoxelMaterialTriple materialSet)
        {
            if (!MaterialProxyTripleIndex.ContainsKey(materialSet))
            {
                var matId = MaterialProxyTripleIndex[materialSet] = MyMaterials1.AllocateProxy();
                MyMaterials1.ProxyPool.Data[matId.Index] = CreateProxy(materialSet);
            }
            return MaterialProxyTripleIndex[materialSet];
        }

        static unsafe MyMaterialProxy_2 CreateProxy(MyVoxelMaterialTriple triple)
        {
            byte[] buffer;
            int size;
            bool singleMaterial = triple.I1 == -1 && triple.I2 == -1;

            if(singleMaterial)
            {
                size = sizeof(MyVoxelMaterialConstants);
                MyVoxelMaterialConstants constantsData = new MyVoxelMaterialConstants();
                constantsData.Scales = Table[triple.I0].ScaleFactors;
                buffer = new byte[size];
                fixed(byte* dstPtr = buffer)
                {
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), (uint)size);
                }
            }
            else
            {
                size = sizeof(MyVoxelMultiMaterialConstants);
                MyVoxelMultiMaterialConstants constantsData = new MyVoxelMultiMaterialConstants();

                constantsData.Scales0 = Table[triple.I0].ScaleFactors;
                constantsData.Scales1 = Table[triple.I1].ScaleFactors;
                constantsData.Scales2 = triple.I2 >= 0 ? Table[triple.I2].ScaleFactors : Vector3.One;
                buffer = new byte[size];
                fixed (byte* dstPtr = buffer)
                {
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), (uint)size);
                }
            }

            var version = triple.I0.GetHashCode();
            MyHashHelper.Combine(ref version, triple.I1.GetHashCode());
            MyHashHelper.Combine(ref version, triple.I2.GetHashCode());

            return new MyMaterialProxy_2
                {
                    MaterialConstants = 
                    { 
                        BindFlag = MyBindFlag.BIND_PS,
                        CB = MyCommon.GetMaterialCB(size),
                        Version = version,
                        Data = buffer
                    },
                    MaterialSRVs = { 
                        BindFlag = MyBindFlag.BIND_PS, 
                        StartSlot = 0,
                        Version = version,
                        SRVs = singleMaterial
                            ? 
                                new ShaderResourceView[] 
                                { 
                                    Table[triple.I0].Near.ColorMetalArray.ShaderView, Table[triple.I0].Far1.ColorMetalArray.ShaderView, Table[triple.I0].Far2.ColorMetalArray.ShaderView,
                                    Table[triple.I0].Near.NormalGlossArray.ShaderView, Table[triple.I0].Far1.NormalGlossArray.ShaderView, Table[triple.I0].Far2.NormalGlossArray.ShaderView,
                                }
                            : 
                            (
                            triple.I2 == -1 
                                ?
                                new ShaderResourceView[] 
                                { 
                                    Table[triple.I0].Near.ColorMetalArray.ShaderView, Table[triple.I0].Far1.ColorMetalArray.ShaderView, Table[triple.I0].Far2.ColorMetalArray.ShaderView,
                                    Table[triple.I1].Near.ColorMetalArray.ShaderView, Table[triple.I1].Far1.ColorMetalArray.ShaderView, Table[triple.I1].Far2.ColorMetalArray.ShaderView,
                                    null, null, null,
                                    Table[triple.I0].Near.NormalGlossArray.ShaderView, Table[triple.I0].Far1.NormalGlossArray.ShaderView, Table[triple.I0].Far2.NormalGlossArray.ShaderView,
                                    Table[triple.I1].Near.NormalGlossArray.ShaderView, Table[triple.I1].Far1.NormalGlossArray.ShaderView, Table[triple.I1].Far2.NormalGlossArray.ShaderView,
                                    null, null, null,

                                    null, null, null,
                                    null, null, null,
                                    null, null, null
                                }
                                :
                                new ShaderResourceView[] 
                                { 
                                    Table[triple.I0].Near.ColorMetalArray.ShaderView, Table[triple.I0].Far1.ColorMetalArray.ShaderView, Table[triple.I0].Far2.ColorMetalArray.ShaderView,
                                    Table[triple.I1].Near.ColorMetalArray.ShaderView, Table[triple.I1].Far1.ColorMetalArray.ShaderView, Table[triple.I1].Far2.ColorMetalArray.ShaderView,
                                    Table[triple.I2].Near.ColorMetalArray.ShaderView, Table[triple.I2].Far1.ColorMetalArray.ShaderView, Table[triple.I2].Far2.ColorMetalArray.ShaderView,

                                    Table[triple.I0].Near.NormalGlossArray.ShaderView, Table[triple.I0].Far1.NormalGlossArray.ShaderView, Table[triple.I0].Far2.NormalGlossArray.ShaderView,
                                    Table[triple.I1].Near.NormalGlossArray.ShaderView, Table[triple.I1].Far1.NormalGlossArray.ShaderView, Table[triple.I1].Far2.NormalGlossArray.ShaderView,
                                    Table[triple.I2].Near.NormalGlossArray.ShaderView, Table[triple.I2].Far1.NormalGlossArray.ShaderView, Table[triple.I2].Far2.NormalGlossArray.ShaderView,

                                    null, null, null,
                                    null, null, null,
                                    null, null, null
                                }
                                )
                           
                    }
                };
        }

        internal static void Init()
        {
            //MyCallbacks.RegisterResourceRequestListener(new OnResourceRequestDelegate(OnResourcesRequesting));
            //MyCallbacks.RegisterResourceGatherListener(new OnResourceGatherDelegate(OnResourcesGather));
            //MyCallbacks.RegisterSessionEndListener(new OnSessionEndDelegate(OnSessionEnd));
            //MyCallbacks.RegisterDeviceEndListener(new OnDeviceEndDelegate(OnDeviceEnd));
            //MyCallbacks.RegisterTexturesReloadListener(new OnTexturesReloadDelegate(OnTexturesReload));
        }

        internal static void ReleaseResources()
        {
            for (int i = 0; i < Table.Length; i++)
            {
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Near);
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Far1);
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Far2);
            }
        }

        internal static void OnResourcesRequesting()
        {
            foreach (var id in MaterialQueryResourcesTable)
            {
                // query all textures
                MyVoxelMaterialDetailSet.RequestResources(ref Table[id].Near);
                MyVoxelMaterialDetailSet.RequestResources(ref Table[id].Far1);
                MyVoxelMaterialDetailSet.RequestResources(ref Table[id].Far2);
                //MyTextureManager.GetTexture(Table[id].FoliageArray_Texture);
                MyTextures.GetTexture(Table[id].FoliageArray_Texture, MyTextureEnum.COLOR_METAL);
            }
        }

        internal static void OnResourcesGather()
        {
            foreach (var id in MaterialQueryResourcesTable)
            {
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[id].Near);
                MyVoxelMaterialDetailSet.PrepareArrays(ref Table[id].Near);

                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[id].Far1);
                MyVoxelMaterialDetailSet.PrepareArrays(ref Table[id].Far1);

                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[id].Far2);
                MyVoxelMaterialDetailSet.PrepareArrays(ref Table[id].Far2);
            }

            if (MaterialQueryResourcesTable.Count > 0)
            {
                // because array of foliage might have changed
                RebuildMaterialFoliageTable();

                // traverse and update all existing proxies
                foreach (var kv in MaterialProxyTripleIndex)
                {
                    MyMaterials1.ProxyPool.Data[kv.Value.Index] = CreateProxy(kv.Key);
                }
            }

            MaterialQueryResourcesTable.Clear();
        }

        internal static void OnSessionEnd()
        {
            MaterialQueryResourcesTable.Clear();
            // clear material proxies
            MaterialProxyTripleIndex.Clear();
        }

        internal static void OnDeviceEnd()
        {
            OnSessionEnd();

            // clear resources
            ReleaseResources();
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

        static readonly HashSet<MyStringId> MERGABLE_MATERIAL_NAMES = new HashSet<MyStringId>(MyStringId.Comparer) 
        { 
            X.TEXT("BlockSheet"), 
            X.TEXT("CubesSheet"), 
            X.TEXT("CubesMetalSheet"), 
            X.TEXT("RoofSheet"), 
            X.TEXT("StoneSheet"), 
            X.TEXT("House_Texture"), 
            X.TEXT("RoofSheetRound") 
        };        

        internal static bool IsMergable(MyMeshMaterialId matId)
        {
            return MergableRKs.Contains(Table[matId.Index].RepresentationKey);
        }

        internal static MyMeshMaterialId GetMaterialId(string name)
        {
            return MaterialNameIndex.Get(X.TEXT(name));
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
            if (desc.ContentPath != null)
            {
                MyHashHelper.Combine(ref key, desc.ContentPath.GetHashCode());
            }

            return key;
        }

        internal static MyMeshMaterialId GetMaterialId(ref MyMeshMaterialInfo desc, string assetFile = null)
        {
            var rk = CalculateRK(ref desc);

            if(!MaterialRkIndex.ContainsKey(rk))
            {
                var id = MaterialRkIndex[rk] = new MyMeshMaterialId { Index = MaterialsPool.Allocate() };

                desc.Id = id;
                desc.RepresentationKey = rk;

                MaterialsPool.Data[id.Index] = desc;
                MaterialProxyIndex[id] = MyMaterials1.AllocateProxy();

                MaterialQueryResourcesTable.Add(id);

                var nameIndex = desc.Name;

                if(MERGABLE_MATERIAL_NAMES.Contains(nameIndex))
                {
                    MergableRKs.Add(desc.RepresentationKey);
                }

                if(!MaterialNameIndex.ContainsKey(nameIndex))
                {
                    MaterialNameIndex[nameIndex] = id;
                }
                else if(assetFile != null)
                {
                    VRageRender.MyRender11.Log.WriteLine(String.Format("Asset {0} tries to overrwrite material {1} with different textures", assetFile, desc.Name.ToString()));
                }

                return id;
            }

            return MaterialRkIndex[rk];
        }

        internal static MyMeshMaterialId GetMaterialId(string name, string contentPath, string colorMetalTexture, string normalGlossTexture, string extensionTexture, string technique)
        {
            MyMeshMaterialInfo desc;
            desc = new MyMeshMaterialInfo
            {
                Name = X.TEXT(name),
                ContentPath = contentPath,
                ColorMetal_Texture = X.TEXT(colorMetalTexture),
                NormalGloss_Texture = X.TEXT(normalGlossTexture),
                Extensions_Texture = X.TEXT(extensionTexture),
                Technique = technique
            };

            return GetMaterialId(ref desc);
        }

        internal static MyMeshMaterialId GetMaterialId(MyMaterialDescriptor importDesc, string contentPath, string assetFile = null)
        {
            MyMeshMaterialInfo desc;
            if(importDesc != null)
            {
                desc = new MyMeshMaterialInfo
                {
                    Name = X.TEXT(importDesc.MaterialName),
                    ContentPath = contentPath,
                    ColorMetal_Texture = X.TEXT(importDesc.Textures.Get("ColorMetalTexture", "")),
                    NormalGloss_Texture = X.TEXT(importDesc.Textures.Get("NormalGlossTexture", "")),
                    Extensions_Texture = X.TEXT(importDesc.Textures.Get("AddMapsTexture", "")),
                    Alphamask_Texture = X.TEXT(importDesc.Textures.Get("AlphamaskTexture", null)),
                    Technique = importDesc.Technique
                };
            }
            else
            {
                return NullMaterialId;
            }

            return GetMaterialId(ref desc, assetFile);
        }

        internal static void Init()
        {
            //MyCallbacks.RegisterResourceRequestListener(new OnResourceRequestDelegate(OnResourcesRequesting));
            //MyCallbacks.RegisterResourceGatherListener(new OnResourceGatherDelegate(OnResourcesGathering));

            CreateCommonMaterials();
        }

        internal static void InvalidateMaterials()
        {
            foreach (var id in MaterialRkIndex.Values)
            {
                MaterialQueryResourcesTable.Add(id);
            }
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
                Name = X.TEXT("__NULL_MATERIAL"),
                ColorMetal_Texture = MyStringId.NullOrEmpty,
                NormalGloss_Texture = MyStringId.NullOrEmpty,
                Extensions_Texture = MyStringId.NullOrEmpty,
                Alphamask_Texture = MyStringId.NullOrEmpty,
                Technique = "MESH"
            };
            NullMaterialId = GetMaterialId(ref nullMatDesc);

            var debugMatDesc = new MyMeshMaterialInfo
            {
                Name = X.TEXT("__DEBUG_MATERIAL"),
                ColorMetal_Texture = MyRender11.DebugMode ? X.TEXT("Pink") : MyStringId.NullOrEmpty,
                NormalGloss_Texture = MyStringId.NullOrEmpty,
                Extensions_Texture = MyStringId.NullOrEmpty,
                Alphamask_Texture = MyStringId.NullOrEmpty,
                Technique = "MESH"
            };
            DebugMaterialId = GetMaterialId(ref debugMatDesc);
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
    }

    class MyMaterials1
    {
        internal static MyFreelist<MyMaterialProxy_2> ProxyPool = new MyFreelist<MyMaterialProxy_2>(512);

        internal static MyMaterialProxyId AllocateProxy()
        {
            return new MyMaterialProxyId { Index = ProxyPool.Allocate() };
        }

        internal static void FreeProxy(MyMaterialProxyId id)
        {
            ProxyPool.Free(id.Index);
        }

        internal static void Init()
        {
        }

        internal static void OnSessionEnd()
        {
            ProxyPool.Clear();
        }
    }
}
