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
        internal Vector4 DistancesAndScale;
        internal Vector4 DistancesAndScaleFar;
        internal Vector2 DistancesAndScaleFar3;
        internal float ExtensionDetailScale;
        float _padding;
        internal Vector4 Far3Color;        
    }

    struct MyVoxelMultiMaterialConstants
    {
        internal Vector4 DistancesAndScale0;
        internal Vector4 DistancesAndScale1;
        internal Vector4 DistancesAndScale2;
        internal Vector4 DistancesAndScaleFar0;
        internal Vector4 DistancesAndScaleFar1;
        internal Vector4 DistancesAndScaleFar2;
        internal Vector4 DistancesAndScaleFar31;
        internal Vector4 DistancesAndScaleFar32;
        internal Vector4 DistancesAndScaleFar33;
        internal Vector4 Far3Color1;
        internal Vector4 Far3Color2;
        internal Vector4 Far3Color3;

        internal float ExtensionDetailScale0;
        internal float ExtensionDetailScale1;
        internal float ExtensionDetailScale2;

        float _padding;
    }

    class MyVoxelMaterials1
    {
        internal static Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId> MaterialProxyTripleIndex = new Dictionary<MyVoxelMaterialTriple, MyMaterialProxyId>(MyVoxelMaterialTriple.Comparer);
        internal static MyVoxelMaterial1[] Table = new MyVoxelMaterial1[0];
        // not hash set but list
        internal static List<int> MaterialQueryResourcesTable = new List<int>();

        internal static void Set(MyRenderVoxelMaterialData[] list, bool update = false)
        {
            if (!update)
                Array.Resize(ref Table, list.Length);

            for (int i = 0; i < list.Length; i++)
            {
                // copy data 
                int index = update ? list[i].Index : i;

                Table[index].Near.ColorMetalXZnY_Texture = X.TEXT_(list[i].ColorMetalXZnY);
                Table[index].Near.ColorMetalpY_Texture = X.TEXT_(list[i].ColorMetalY);
                Table[index].Near.NormalGlossXZnY_Texture = X.TEXT_(list[i].NormalGlossXZnY);
                Table[index].Near.NormalGlossY_Texture = X.TEXT_(list[i].NormalGlossY);
                Table[index].Near.ExtXZnY_Texture = X.TEXT_(list[i].ExtXZnY);
                Table[index].Near.ExtY_Texture = X.TEXT_(list[i].ExtY);

                Table[index].Far1.ColorMetalXZnY_Texture = X.TEXT_(list[i].ColorMetalXZnYFar1);
                Table[index].Far1.ColorMetalpY_Texture = X.TEXT_(list[i].ColorMetalYFar1);
                Table[index].Far1.NormalGlossXZnY_Texture = X.TEXT_(list[i].NormalGlossXZnYFar1);
                Table[index].Far1.NormalGlossY_Texture = X.TEXT_(list[i].NormalGlossYFar1);
                Table[index].Far1.ExtXZnY_Texture = X.TEXT_(list[i].ExtXZnYFar1);
                Table[index].Far1.ExtY_Texture = X.TEXT_(list[i].ExtYFar1);

                Table[index].Far2.ColorMetalXZnY_Texture = X.TEXT_(list[i].ColorMetalXZnYFar2);
                Table[index].Far2.ColorMetalpY_Texture = X.TEXT_(list[i].ColorMetalYFar2);
                Table[index].Far2.NormalGlossXZnY_Texture = X.TEXT_(list[i].NormalGlossXZnYFar2);
                Table[index].Far2.NormalGlossY_Texture = X.TEXT_(list[i].NormalGlossYFar2);
                Table[index].Far2.ExtXZnY_Texture = X.TEXT_(list[i].ExtXZnYFar2);
                Table[index].Far2.ExtY_Texture = X.TEXT_(list[i].ExtYFar2);

                Table[index].FoliageArray_Texture = list[i].ExtensionTextureArray1;
                Table[index].FoliageArray_NormalTexture = list[i].ExtensionTextureArray2;

                Table[index].FoliageColorTextureArray = MyTextureArray.FromStringArray(list[i].FoliageColorTextureArray, MyTextureEnum.COLOR_METAL, "CM Foliage");
                Table[index].FoliageNormalTextureArray = MyTextureArray.FromStringArray(list[i].FoliageNormalTextureArray, MyTextureEnum.NORMALMAP_GLOSS, "NG Foliage");

                Table[index].FoliageDensity = list[i].ExtensionDensity;
                Table[index].FoliageScale = list[i].ExtensionScale;
                Table[index].FoliageScaleVariation = list[i].ExtensionRandomRescaleMult;
                Table[index].FoliageType = list[i].ExtensionType;

                Table[index].DistanceAndScale = list[i].DistanceAndScale;
                Table[index].DistanceAndScaleFar = list[i].DistanceAndScaleFar;
                Table[index].DistanceAndScaleFar3 = list[i].DistanceAndScaleFar3;
                Table[index].Far3Color = list[i].Far3Color;
                Table[index].ExtensionDetailScale = list[i].ExtensionDetailScale;

                MaterialQueryResourcesTable.Add(index);
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

        internal unsafe static void RebuildMaterialFoliageTable()
        {
            var array = stackalloc MaterialFoliageConstantsElem[256];
            int N = Table.Length;
            for (int i = 0; i < N; i++)
            {
                uint arraySize = 0;

                if (Table[i].FoliageColorTextureArray != null)
                {
                    arraySize = (uint)Table[i].FoliageColorTextureArray.ArrayLen;
                }
                else
                { 
                    var arrayTexId = MyTextures.GetTexture(Table[i].FoliageArray_Texture, MyTextureEnum.COLOR_METAL, true);
                    arraySize = (uint)((Texture2D)MyTextures.Textures.Data[arrayTexId.Index].Resource).Description.ArraySize;
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
            if (!MaterialProxyTripleIndex.TryGetValue(materialSet, out pid))
            {
                pid = MaterialProxyTripleIndex[materialSet] = MyMaterials1.AllocateProxy();
                MyMaterials1.ProxyPool.Data[pid.Index] = CreateProxy(materialSet);
            }
            return pid;
        }

        static unsafe MyMaterialProxy_2 CreateProxy(MyVoxelMaterialTriple triple)
        {
            byte[] buffer;
            int size;

            System.Diagnostics.Debug.Assert(triple.I0 < Table.Length, "Index to table incorrect");
            System.Diagnostics.Debug.Assert(triple.I1 < Table.Length, "Index to table incorrect");
            System.Diagnostics.Debug.Assert(triple.I2 < Table.Length, "Index to table incorrect");

            //TODO: This shouldnt happen if Table is created correctly
            if (triple.I0 >= Table.Length) triple.I0 = 0;
            if (triple.I1 >= Table.Length) triple.I1 = -1;
            if (triple.I2 >= Table.Length) triple.I2 = -1;            
            //////end of hack


            bool singleMaterial = triple.I1 == -1 && triple.I2 == -1;

            if(singleMaterial)
            {
                size = sizeof(MyVoxelMaterialConstants);
                MyVoxelMaterialConstants constantsData = new MyVoxelMaterialConstants();
                constantsData.DistancesAndScale = Table[triple.I0].DistanceAndScale;
                constantsData.DistancesAndScaleFar = Table[triple.I0].DistanceAndScaleFar;
                constantsData.DistancesAndScaleFar3 = Table[triple.I0].DistanceAndScaleFar3;
                constantsData.Far3Color = Table[triple.I0].Far3Color;
                constantsData.ExtensionDetailScale = Table[triple.I0].ExtensionDetailScale;

                buffer = new byte[size];
                fixed(byte* dstPtr = buffer)
                {
#if XB1
                    SharpDX.Utilities.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), size);
#else // !XB1
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constantsData), (uint)size);
#endif // !XB1
                }
            }
            else
            {
                size = sizeof(MyVoxelMultiMaterialConstants);
                MyVoxelMultiMaterialConstants constantsData = new MyVoxelMultiMaterialConstants();

                constantsData.DistancesAndScale0 = Table[triple.I0].DistanceAndScale;
                constantsData.DistancesAndScale1 = Table[triple.I1].DistanceAndScale;
                constantsData.DistancesAndScale2 = triple.I2 >= 0 ? Table[triple.I2].DistanceAndScale : Vector4.Zero;
                constantsData.DistancesAndScaleFar0 = Table[triple.I0].DistanceAndScaleFar;
                constantsData.DistancesAndScaleFar1 = Table[triple.I1].DistanceAndScaleFar;
                constantsData.DistancesAndScaleFar2 = triple.I2 >= 0 ? Table[triple.I2].DistanceAndScaleFar : Vector4.Zero;
                constantsData.DistancesAndScaleFar31 = new Vector4(Table[triple.I0].DistanceAndScaleFar3.X, Table[triple.I0].DistanceAndScaleFar3.Y, 0, 0);
                constantsData.DistancesAndScaleFar32 = new Vector4(Table[triple.I1].DistanceAndScaleFar3.X, Table[triple.I1].DistanceAndScaleFar3.Y, 0, 0);
                constantsData.DistancesAndScaleFar33 = triple.I2 >= 0 ? new Vector4(Table[triple.I2].DistanceAndScaleFar3.X, Table[triple.I2].DistanceAndScaleFar3.Y, 0, 0) : Vector4.Zero;
                constantsData.Far3Color1 = Table[triple.I0].Far3Color.ToVector4();
                constantsData.Far3Color2 = Table[triple.I1].Far3Color.ToVector4();
                constantsData.Far3Color3 = triple.I2 >= 0 ? Table[triple.I2].Far3Color.ToVector4() : Vector4.Zero;
                constantsData.ExtensionDetailScale0 = Table[triple.I0].ExtensionDetailScale;
                constantsData.ExtensionDetailScale1 = Table[triple.I1].ExtensionDetailScale;
                constantsData.ExtensionDetailScale2 = triple.I2 >= 0 ? Table[triple.I2].ExtensionDetailScale : 0;

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
					SRVs = singleMaterial
						? 
                            new IShaderResourceBindable[] 
							{ 
								Table[triple.I0].Near.ColorMetalArray, Table[triple.I0].Far1.ColorMetalArray, Table[triple.I0].Far2.ColorMetalArray,
								Table[triple.I0].Near.NormalGlossArray, Table[triple.I0].Far1.NormalGlossArray, Table[triple.I0].Far2.NormalGlossArray,
								Table[triple.I0].Near.ExtArray, Table[triple.I0].Far1.ExtArray, Table[triple.I0].Far2.ExtArray,
							}
						: 
						(
						triple.I2 == -1 
							?
                            new IShaderResourceBindable[] 
							{ 
								Table[triple.I0].Near.ColorMetalArray, Table[triple.I0].Far1.ColorMetalArray, Table[triple.I0].Far2.ColorMetalArray,
								Table[triple.I1].Near.ColorMetalArray, Table[triple.I1].Far1.ColorMetalArray, Table[triple.I1].Far2.ColorMetalArray,
								null, null, null,
								Table[triple.I0].Near.NormalGlossArray, Table[triple.I0].Far1.NormalGlossArray, Table[triple.I0].Far2.NormalGlossArray,
								Table[triple.I1].Near.NormalGlossArray, Table[triple.I1].Far1.NormalGlossArray, Table[triple.I1].Far2.NormalGlossArray,
								null, null, null,

								Table[triple.I0].Near.ExtArray, Table[triple.I0].Far1.ExtArray, Table[triple.I0].Far2.ExtArray,
								Table[triple.I1].Near.ExtArray, Table[triple.I1].Far1.ExtArray, Table[triple.I1].Far2.ExtArray,
								null, null, null
							}
							:
                            new IShaderResourceBindable[] 
							{ 
								Table[triple.I0].Near.ColorMetalArray, Table[triple.I0].Far1.ColorMetalArray, Table[triple.I0].Far2.ColorMetalArray,
								Table[triple.I1].Near.ColorMetalArray, Table[triple.I1].Far1.ColorMetalArray, Table[triple.I1].Far2.ColorMetalArray,
								Table[triple.I2].Near.ColorMetalArray, Table[triple.I2].Far1.ColorMetalArray, Table[triple.I2].Far2.ColorMetalArray,

								Table[triple.I0].Near.NormalGlossArray, Table[triple.I0].Far1.NormalGlossArray, Table[triple.I0].Far2.NormalGlossArray,
								Table[triple.I1].Near.NormalGlossArray, Table[triple.I1].Far1.NormalGlossArray, Table[triple.I1].Far2.NormalGlossArray,
								Table[triple.I2].Near.NormalGlossArray, Table[triple.I2].Far1.NormalGlossArray, Table[triple.I2].Far2.NormalGlossArray,

								Table[triple.I0].Near.ExtArray, Table[triple.I0].Far1.ExtArray, Table[triple.I0].Far2.ExtArray,
								Table[triple.I1].Near.ExtArray, Table[triple.I1].Far1.ExtArray, Table[triple.I1].Far2.ExtArray,
								Table[triple.I2].Near.ExtArray, Table[triple.I2].Far1.ExtArray, Table[triple.I2].Far2.ExtArray,
							}
						)
				};

            return new MyMaterialProxy_2
                {
                    MaterialConstants = materialConstants,
                    MaterialSRVs = srvTable
                };
        }

        internal static void ReleaseResources()
        {
            for (int i = 0; i < Table.Length; i++)
            {
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Near);
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Far1);
                MyVoxelMaterialDetailSet.ReleaseResources(ref Table[i].Far2);
                if (Table[i].FoliageColorTextureArray != null)
                    Table[i].FoliageColorTextureArray.Dispose();
                if (Table[i].FoliageNormalTextureArray != null)
                    Table[i].FoliageNormalTextureArray.Dispose();
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
                MaterialQueryResourcesTable.Add(i);
            }
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
