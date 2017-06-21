using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using VRage;
using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{
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

        public MyMaterialProxy_2 Info
        {
            get { return MyMaterials1.ProxyPool.Data[Index]; }
        }
    }

    struct MyVoxelMaterialEntry
    {
        public Vector4 DistancesAndScale;
        public Vector3 DistancesAndScaleFar;
        float _padding0;
        public Vector3 DistancesAndScaleFar2;
        float _padding1;
        public Vector3 DistancesAndScaleFar3;
        public float ExtensionDetailScale;
        public Vector4 Far3Color;

        public Vector4I SliceNear1;
        public Vector4I SliceNear2;
        public Vector4I SliceFar1;
        public Vector4I SliceFar2;
        public Vector4I SliceFar21;
        public Vector4I SliceFar22;
    }

    struct MyVoxelMaterialConstants
    {
        public MyVoxelMaterialEntry entry;
    }

    struct MyVoxelMultiMaterialConstants
    {
        public MyVoxelMaterialEntry entry0;
        public MyVoxelMaterialEntry entry1;
        public MyVoxelMaterialEntry entry2;
    }

    static class MyVoxelMaterials1
    {
        private static readonly Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId> m_materialProxyTripleIndex = new Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId>(MyVoxelMaterialTriple.Comparer);
        internal static MyVoxelMaterial1[] Table = new MyVoxelMaterial1[0];
        // not hash set but list
        private static readonly List<int> m_materialQueryResourcesTable = new List<int>();

        private static string[] CreateStringArray(string str1, string str2, string str3)
        {
            MyRenderProxy.Assert(str1 != "");
            MyRenderProxy.Assert(str2 != "");
            MyRenderProxy.Assert(str3 != "");
            
            string[] str = new string[3];
            str[0] = str1;
            str[1] = str2;
            str[2] = str3;
            return str;
        }
        
        internal static void Set(MyRenderVoxelMaterialData[] list, bool update = false)
        {
            if (!update)
                Array.Resize(ref Table, list.Length);

            for (int i = 0; i < list.Length; i++)
            {
                // copy data 
                int index = update ? list[i].Index : i;

                MyCommon.VoxelMaterialsConstants.Invalidate(index);

                Table[index].Resource.ColorMetalXZnY_Filepaths = CreateStringArray(list[i].ColorMetalXZnY,
                    list[i].ColorMetalXZnYFar1, list[i].ColorMetalXZnYFar2);
                Table[index].Resource.ColorMetalY_Filepaths = CreateStringArray(list[i].ColorMetalY,
                    list[i].ColorMetalYFar1, list[i].ColorMetalYFar2);
                Table[index].Resource.NormalGlossXZnY_Filepaths = CreateStringArray(list[i].NormalGlossXZnY,
                    list[i].NormalGlossXZnYFar1, list[i].NormalGlossXZnYFar2);
                Table[index].Resource.NormalGlossY_Filepaths = CreateStringArray(list[i].NormalGlossY, 
                    list[i].NormalGlossYFar1, list[i].NormalGlossYFar2);
                Table[index].Resource.ExtXZnY_Filepaths = CreateStringArray(list[i].ExtXZnY, 
                    list[i].ExtXZnYFar1, list[i].ExtXZnYFar2);
                Table[index].Resource.ExtY_Filepaths = CreateStringArray(list[i].ExtY, list[i].ExtYFar1, list[i].ExtYFar2);
                
                Table[index].FoliageArray_Texture = list[i].ExtensionTextureArray1;
                Table[index].FoliageArray_NormalTexture = list[i].ExtensionTextureArray2;

                MyFileArrayTextureManager arrayManager = MyManagers.FileArrayTextures;
                if (list[i].FoliageColorTextureArray != null)
                    Table[index].FoliageColorTextureArray = arrayManager.CreateFromFiles("MyVoxelMaterial1.FoliageColorTextureArray", list[i].FoliageColorTextureArray, MyFileTextureEnum.COLOR_METAL, MyGeneratedTexturePatterns.ColorMetal_BC7_SRgb, Format.BC7_UNorm_SRgb, true);
                else
                    Table[index].FoliageColorTextureArray = null;
                
                if (list[i].FoliageNormalTextureArray != null)
                    Table[index].FoliageNormalTextureArray = arrayManager.CreateFromFiles("MyVoxelMaterial1.FoliageNormalTextureArray", list[i].FoliageNormalTextureArray, MyFileTextureEnum.NORMALMAP_GLOSS, MyGeneratedTexturePatterns.NormalGloss_BC7, Format.BC7_UNorm, true);
                else
                    Table[index].FoliageNormalTextureArray = null;
                
                Table[index].FoliageDensity = list[i].ExtensionDensity;
                Table[index].FoliageScale = list[i].ExtensionScale;
                Table[index].FoliageScaleVariation = list[i].ExtensionRandomRescaleMult;
                Table[index].FoliageType = list[i].ExtensionType;

                Table[index].DistanceAndScale = list[i].DistanceAndScale;
                Table[index].DistanceAndScaleFar = list[i].DistanceAndScaleFar;
                Table[index].DistanceAndScaleFar3 = list[i].DistanceAndScaleFar3;
                Table[index].Far3Color = list[i].Far3Color;
                Table[index].ExtensionDetailScale = list[i].ExtensionDetailScale;

                m_materialQueryResourcesTable.Add(index);
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

        private static unsafe void RebuildMaterialFoliageTable()
        {
            var array = stackalloc MaterialFoliageConstantsElem[256];
            int N = Table.Length;
            for (int i = 0; i < N; i++)
            {
                uint arraySize = 0;

                if (Table[i].FoliageColorTextureArray != null)
                {
                    arraySize = (uint)Table[i].FoliageColorTextureArray.SubtexturesCount;
                }
                else
                {
                    MyFileTextureManager texManager = MyManagers.FileTextures;
                    var arrayTex = texManager.GetTexture(Table[i].FoliageArray_Texture, MyFileTextureEnum.COLOR_METAL, true);
                    arraySize = (uint)((Texture2D)arrayTex.Resource).Description.ArraySize;
                }

                array[i] = new MaterialFoliageConstantsElem {
                    Scale = Table[i].FoliageScale,
                    ScaleVar = Table[i].FoliageScaleVariation,
                    TexturesNum = arraySize
                };
            }
            var mapping = MyMapping.MapDiscard(MyCommon.MaterialFoliageTableConstants);
            for (int arrayIndex = 0; arrayIndex < N; ++arrayIndex)
                mapping.WriteAndPosition(ref array[arrayIndex]);
            mapping.Unmap();
        }

        internal static MyMaterialProxyId GetMaterialProxyId(MyVoxelMaterialTriple materialSet)
        {
            MyMaterialProxyId pid;
            if (!m_materialProxyTripleIndex.TryGetValue(materialSet, out pid))
            {
                pid = m_materialProxyTripleIndex[materialSet] = MyMaterials1.AllocateProxy();
                MyMaterials1.ProxyPool.Data[pid.Index] = CreateProxyWithValidMaterialConstants(materialSet);
            }
            return pid;
        }

        static void UpdateVoxelSlices(ref MyVoxelMaterialEntry entry, IDynamicFileArrayTexture cm, string[] cmXZnY,
            string[] cmY, IDynamicFileArrayTexture ng,
            string[] ngXZnY, string[] ngY, IDynamicFileArrayTexture ext, string[] extXZnY, string[] extY)
        {
            int index = 0;
            entry.SliceNear1.X = cm.GetOrAddSlice(cmXZnY[index]);
            entry.SliceNear1.Y = cm.GetOrAddSlice(cmY[index]);
            entry.SliceNear1.Z = ng.GetOrAddSlice(ngXZnY[index]);
            entry.SliceNear1.W = ng.GetOrAddSlice(ngY[index]);
            entry.SliceNear2.X = ext.GetOrAddSlice(extXZnY[index]);
            entry.SliceNear2.Y = ext.GetOrAddSlice(extY[index]);

            index = 1;
            entry.SliceFar1.X = cm.GetOrAddSlice(cmXZnY[index]);
            entry.SliceFar1.Y = cm.GetOrAddSlice(cmY[index]);
            entry.SliceFar1.Z = ng.GetOrAddSlice(ngXZnY[index]);
            entry.SliceFar1.W = ng.GetOrAddSlice(ngY[index]);
            entry.SliceFar2.X = ext.GetOrAddSlice(extXZnY[index]);
            entry.SliceFar2.Y = ext.GetOrAddSlice(extY[index]);

            index = 2;
            entry.SliceFar21.X = cm.GetOrAddSlice(cmXZnY[index]);
            entry.SliceFar21.Y = cm.GetOrAddSlice(cmY[index]);
            entry.SliceFar21.Z = ng.GetOrAddSlice(ngXZnY[index]);
            entry.SliceFar21.W = ng.GetOrAddSlice(ngY[index]);
            entry.SliceFar22.X = ext.GetOrAddSlice(extXZnY[index]);
            entry.SliceFar22.Y = ext.GetOrAddSlice(extY[index]);
        }

        static void FillVoxelMaterialEntry(ref MyVoxelMaterialEntry entry, ref MyVoxelMaterial1 voxelMaterial1)
        {
            entry.DistancesAndScale = voxelMaterial1.DistanceAndScale;
            entry.DistancesAndScaleFar = new Vector3(voxelMaterial1.DistanceAndScaleFar.X, voxelMaterial1.DistanceAndScaleFar.Y, 1);
            entry.DistancesAndScaleFar2 = new Vector3(voxelMaterial1.DistanceAndScaleFar.Z, voxelMaterial1.DistanceAndScaleFar.W, 2);
            entry.DistancesAndScaleFar3 = new Vector3(voxelMaterial1.DistanceAndScaleFar3.X, voxelMaterial1.DistanceAndScaleFar3.Y, 3);
            entry.Far3Color = voxelMaterial1.Far3Color;
            entry.ExtensionDetailScale = voxelMaterial1.ExtensionDetailScale;

            IDynamicFileArrayTexture texColorMetal = MyGlobalResources.FileArrayTextureVoxelCM;
            IDynamicFileArrayTexture texNormalGloss = MyGlobalResources.FileArrayTextureVoxelNG;
            IDynamicFileArrayTexture texExt = MyGlobalResources.FileArrayTextureVoxelExt;

            MyVoxelMaterialDetailSet set = voxelMaterial1.Resource;
            UpdateVoxelSlices(ref entry, texColorMetal, set.ColorMetalXZnY_Filepaths, set.ColorMetalY_Filepaths,
                texNormalGloss, set.NormalGlossXZnY_Filepaths, set.NormalGlossY_Filepaths,
                texExt, set.ExtY_Filepaths, set.ExtY_Filepaths);
        }

        static void ResetVoxelMaterialEntry(out MyVoxelMaterialEntry entry)
        {
            MyVoxelMaterialEntry zero = new MyVoxelMaterialEntry();
            entry = zero;
        }

        public static void UpdateGlobalVoxelMaterialsCB(int matId)
        {
            if (MyCommon.VoxelMaterialsConstants.NeedsUpdate(matId))
            {
                MyVoxelMaterialConstants constantsData = new MyVoxelMaterialConstants();

                FillVoxelMaterialEntry(ref constantsData.entry, ref Table[matId]);
                MyCommon.VoxelMaterialsConstants.UpdateEntry(matId, ref constantsData.entry);
            }
        }

        static MyMaterialProxy_2 CreateProxyWithPlaceholderdMaterialConstants(MyVoxelMaterialTriple triple)
        {
            var version = triple.I0.GetHashCode();
            MyHashHelper.Combine(ref version, triple.I1.GetHashCode());
            MyHashHelper.Combine(ref version, triple.I2.GetHashCode());

            MySrvTable srvTable = new MySrvTable
            {
                BindFlag = MyBindFlag.BIND_PS,
                StartSlot = 0,
                Version = version,
                Srvs = new ISrvBindable[] 
							{ 
                                MyGlobalResources.FileArrayTextureVoxelCM,
                                MyGlobalResources.FileArrayTextureVoxelNG,
                                MyGlobalResources.FileArrayTextureVoxelExt,
							}
            };

            return new MyMaterialProxy_2
            {
                MaterialConstants = new MyConstantsPack(),
                MaterialSrvs = srvTable
            };
        }

        static unsafe MyMaterialProxy_2 CreateProxyWithValidMaterialConstants(MyVoxelMaterialTriple triple)
        {
            byte[] buffer;
            int size;

            MyRenderProxy.Assert(triple.I0 < Table.Length, "Index to table incorrect");
            MyRenderProxy.Assert(triple.I1 < Table.Length, "Index to table incorrect");
            MyRenderProxy.Assert(triple.I2 < Table.Length, "Index to table incorrect");

            //TODO: This shouldnt happen if Table is created correctly
            if (triple.I0 >= Table.Length) triple.I0 = 0;
            if (triple.I1 >= Table.Length) triple.I1 = -1;
            if (triple.I2 >= Table.Length) triple.I2 = -1;            
            //////end of hack

            bool singleMaterial = triple.I1 == -1 && triple.I2 == -1;

            if(singleMaterial)
            {
                // this is for the old rendering and also for the debris
                size = sizeof(MyVoxelMaterialConstants);
                MyVoxelMaterialConstants constantsData = new MyVoxelMaterialConstants();

                buffer = new byte[size];
                fixed (byte* dstPtr = buffer)
                {
                    FillVoxelMaterialEntry(ref constantsData.entry, ref Table[triple.I0]);
#if XB1
                    SharpDX.Utilities.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), size);
#else // !XB1
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), (uint)size);
#endif // !XB1
                }
            }
            else
            {
                // this is for the old rendering and also for the debris
                size = sizeof(MyVoxelMultiMaterialConstants);
                MyVoxelMultiMaterialConstants constantsData = new MyVoxelMultiMaterialConstants();

                FillVoxelMaterialEntry(ref constantsData.entry0, ref Table[triple.I0]);
                FillVoxelMaterialEntry(ref constantsData.entry1, ref Table[triple.I1]);

                if (triple.I2 >= 0)
                {
                    FillVoxelMaterialEntry(ref constantsData.entry2, ref Table[triple.I2]);
                }
                else
                    ResetVoxelMaterialEntry(out constantsData.entry2);

                buffer = new byte[size];
                fixed (byte* dstPtr = buffer)
                {
#if XB1
                    SharpDX.Utilities.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), size);
#else // !XB1
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), (uint)size);
#endif // !XB1
                }
            }
            var version = triple.I0.GetHashCode();
            MyHashHelper.Combine(ref version, triple.I1.GetHashCode());
            MyHashHelper.Combine(ref version, triple.I2.GetHashCode());

            MyConstantsPack materialConstants = new MyConstantsPack
            {
                BindFlag = MyBindFlag.BIND_PS,
                CB = MyCommon.GetMaterialCB(size),
                Version = version,
                Data = buffer
            };

			MySrvTable srvTable = new MySrvTable
				{
					// NOTE(AF) Adding BIND_VS here will interfere with shadows, causing flickering in the first cascade
					BindFlag = MyBindFlag.BIND_PS, 
					StartSlot = 0,
					Version = version,
					Srvs = new ISrvBindable[] 
							{ 
                                MyGlobalResources.FileArrayTextureVoxelCM,
                                MyGlobalResources.FileArrayTextureVoxelNG,
                                MyGlobalResources.FileArrayTextureVoxelExt,
							}
				};

            return new MyMaterialProxy_2
                {
                    MaterialConstants = materialConstants,
                    MaterialSrvs = srvTable
                };
        }

        static MyMaterialProxy_2 CreateProxy(MyVoxelMaterialTriple triple)
        {
            if (triple.IsFillingMaterialCB)
                return CreateProxyWithValidMaterialConstants(triple);
            else
                return CreateProxyWithPlaceholderdMaterialConstants(triple);
        }
        

        //internal static void ReleaseResources()
        //{
        //    for (int i = 0; i < Table.Length; i++)
        //    {
        //        MyFileArrayTextureManager manager = MyManagers.FileArrayTextures;
        //        if (Table[i].FoliageColorTextureArray != null)
        //            manager.DisposeTex(ref Table[i].FoliageColorTextureArray);
        //        if (Table[i].FoliageNormalTextureArray != null)
        //            manager.DisposeTex(ref Table[i].FoliageNormalTextureArray);
        //    }
        //}

        internal static void OnResourcesRequesting()
        {
            foreach (var id in m_materialQueryResourcesTable)
            {
                // query all textures
                MyVoxelMaterialDetailSet.RequestResources(ref Table[id].Resource);
                //MyTextureManager.GetTexture(Table[id].FoliageArray_Texture);
                MyFileTextureManager texManager = MyManagers.FileTextures;
                texManager.GetTexture(Table[id].FoliageArray_Texture, MyFileTextureEnum.COLOR_METAL);
            }
        }

        internal static void OnResourcesGather()
        {
            if (m_materialQueryResourcesTable.Count > 0)
            {
                // because array of foliage might have changed
                RebuildMaterialFoliageTable();

                // traverse and update all existing proxies
                foreach (var kv in m_materialProxyTripleIndex)
                {
                    MyMaterials1.ProxyPool.Data[kv.Value.Index] = CreateProxy(kv.Key);
                }
            }

            m_materialQueryResourcesTable.Clear();
        }

        internal static void Init()
        {
            //MyCallbacks.RegisterResourceRequestListener(new OnResourceRequestDelegate(OnResourcesRequesting));
            //MyCallbacks.RegisterResourceGatherListener(new OnResourceGatherDelegate(OnResourcesGather));
            //MyCallbacks.RegisterSessionEndListener(new OnSessionEndDelegate(OnSessionEnd));
            //MyCallbacks.RegisterDeviceEndListener(new OnDeviceEndDelegate(OnDeviceEnd));
            //MyCallbacks.RegisterTexturesReloadListener(new OnTexturesReloadDelegate(OnTexturesReload));
        }

        internal static void OnDeviceReset()
        {
            InvalidateMaterials();
        }
		
        internal static void InvalidateMaterials()
        {
            for (int i = 0; i < Table.Length; i++)
            {
                m_materialQueryResourcesTable.Add(i);
            }
        }

        internal static void OnSessionEnd()
        {
            m_materialQueryResourcesTable.Clear();
            // clear material proxies
            m_materialProxyTripleIndex.Clear();
        }

        internal static void OnDeviceEnd()
        {
            OnSessionEnd();

            // clear resources
            //ReleaseResources();
        }
    }


    static class MyMaterials1
    {
        internal static readonly MyFreelist<MyMaterialProxy_2> ProxyPool = new MyFreelist<MyMaterialProxy_2>(512);

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
