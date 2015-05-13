using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using SysUtils.Utils;
using VRage.Common;
using VRage.Common.Generics;
using VRage.Common.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Graphics;
using Sandbox.Graphics;
using Sandbox.Game.Entities.VoxelMaps.Voxels;

namespace Sandbox.Game.Voxels
{
    class MySingleMaterialHelper
    {
        //  Here we store calculated vertexes (before we send them to vertex buffer) - for single-material triangles
        public MyVertexFormatVoxelSingleData[] Vertices;
        public int VertexCount;
        public short[] Indices;
        public int IndexCount;

        public MyVoxelMaterialDefinition Material;

        //  This pre-initializes this object when preallocating, not when allocating object for specific purpose
        public void LoadData()
        {
            Vertices = new MyVertexFormatVoxelSingleData[MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT];
            Indices = new short[MyVoxelCacheCellRenderHelper.MAX_INDICES_COUNT];
        }

        public void UnloadData()
        {
            Vertices = null;
            Indices = null;
        }

        //  This really starts/initializes this object
        public void SetMaterial(MyVoxelMaterialDefinition material)
        {
            Material = material;
        }
    }

    class MyMultiMaterialHelper
    {
        public MyVertexFormatVoxelSingleData[] Vertices;
        public int VertexCount;

        public MyVoxelMaterialDefinition Material0;
        public MyVoxelMaterialDefinition Material1;
        public MyVoxelMaterialDefinition Material2;

        public void LoadData()
        {
            Vertices = new MyVertexFormatVoxelSingleData[MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT];
        }

        public void UnloadData()
        {
            Vertices = null;
        }

        public void AddVertex(ref MyVoxelVertex vertex)//Vector3 pos, Vector3 normal, MyVoxelMaterialsEnum material, float ambient)
        {
            var material = vertex.Material;
            byte alphaIndex;
            if (Material0.Index == material)
                alphaIndex = 0;
            else if (Material1.Index == material)
                alphaIndex = 1;
            else if (Material2.Index == material)
                alphaIndex = 2;
            else
                throw new System.InvalidOperationException("Should not be there, invalid material");

            Vertices[VertexCount].Position = vertex.Position;
            Vertices[VertexCount].Ambient = vertex.Ambient;
            Vertices[VertexCount].Normal = vertex.Normal;
            Vertices[VertexCount].MaterialAlphaIndex = alphaIndex;
            VertexCount++;
        }

        private bool HasMaterial(MyVoxelMaterialDefinition material)
        {
            if(material == Material0 || material == Material1 || material == Material2)
            {
                return true;
            }
            return false;
        }

        public bool MatchMaterials(MyVoxelMaterialDefinition material0, MyVoxelMaterialDefinition material1, MyVoxelMaterialDefinition material2)
        {
            return HasMaterial(material0) && HasMaterial(material1) && HasMaterial(material2);
        }

        public void SetMaterials(MyVoxelMaterialDefinition mat0, MyVoxelMaterialDefinition mat1, MyVoxelMaterialDefinition mat2)
        {
            Material0 = mat0;
            Material1 = mat1;
            Material2 = mat2;
        }
    }

    static class MyVoxelCacheCellRenderHelper
    {
        public struct MySingleMaterialIndexLookup
        {
            public short VertexIndex;             //  If this vertex is in the list, this is its m_notCompressedIndex 
            public int CalcCounter;               //  For knowing if vertex was calculated in this Begin/End or one of previous (or in this batch!!!)
        }

        public const int MAX_VERTICES_COUNT = short.MaxValue;           //  Max number of vertexes we can hold in vertex buffer (because we support only 16-bit m_notCompressedIndex buffer)
        public const int MAX_INDICES_COUNT = 100000;                    //  Max number of indices we can hold in m_notCompressedIndex buffer (because we don't want to have too huge helper arrays). This number doesn't relate to 16-bit indices.
        public const int MAX_VERTICES_COUNT_STOP = MAX_VERTICES_COUNT - 3;
        public const int MAX_INDICES_COUNT_STOP = MAX_INDICES_COUNT - 3;

        //public static bool[] FinishedSingleMaterials = new bool[MyVoxelMaterials.GetMaterialsCount()];
        //public static Dictionary<int, bool> FinishedMultiMaterials = new Dictionary<int, bool>(MyVoxelConstants.DEFAULT_MULTIMATERIAL_CACHE_SIZE);

        //static MySingleMaterialHelper m_singleMaterialHelper;
        //static MyMultiMaterialHelper m_multiMaterialHelper;

        //  For creating correct indices
        public static MySingleMaterialIndexLookup[][] SingleMaterialIndicesLookup;
        public static int[] SingleMaterialIndicesLookupCount;

        private static MySingleMaterialHelper[] m_preallocatedSingleMaterialHelpers;
        private static Dictionary<int, MyMultiMaterialHelper> m_preallocatedMultiMaterialHelpers;

        static MyVoxelCacheCellRenderHelper()
        {
            /*
            for (int i = 0; i < SingleMaterialIndicesLookup.Length; i++)
            {
                SingleMaterialIndicesLookup[i] = new MySingleMaterialIndexLookup[MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT];
            } */
        }

        public static void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelCacheCellRenderHelper.LoadData");

            MySandboxGame.Log.WriteLine("MyVoxelCacheCellRenderHelper.LoadData - START");
            MySandboxGame.Log.IncreaseIndent();

            /*
            if (m_singleMaterialHelper == null)
            {
                m_singleMaterialHelper = new MySingleMaterialHelper();
                m_singleMaterialHelper.LoadData();

                m_multiMaterialHelper = new MyMultiMaterialHelper();
                m_multiMaterialHelper.LoadData();
            } */

            if (SingleMaterialIndicesLookup == null)
            {
                SingleMaterialIndicesLookup = new MySingleMaterialIndexLookup[MyDefinitionManager.Static.VoxelMaterialCount][];
                SingleMaterialIndicesLookupCount = new int[SingleMaterialIndicesLookup.Length];
            }


            if (m_preallocatedSingleMaterialHelpers == null)
            {
                m_preallocatedSingleMaterialHelpers = new MySingleMaterialHelper[MyDefinitionManager.Static.VoxelMaterialCount];
                m_preallocatedMultiMaterialHelpers = new Dictionary<int, MyMultiMaterialHelper>();
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelCacheCellRenderHelper.LoadData - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UnloadData()
        {
            if (m_preallocatedMultiMaterialHelpers != null)
            {
                foreach (var pair in m_preallocatedMultiMaterialHelpers)
                {
                    pair.Value.UnloadData();
                }
                m_preallocatedMultiMaterialHelpers = null;
            }

            if (m_preallocatedSingleMaterialHelpers != null)
            {
                for (int i = 0; i < m_preallocatedSingleMaterialHelpers.Length; i++)
                {
                    if (m_preallocatedSingleMaterialHelpers[i] != null)
                    {
                        m_preallocatedSingleMaterialHelpers[i].UnloadData();
                        m_preallocatedSingleMaterialHelpers[i] = null;
                    }
                }
                m_preallocatedSingleMaterialHelpers = null;
            }

            SingleMaterialIndicesLookup = null;
            SingleMaterialIndicesLookupCount = null;
        }

        public static MySingleMaterialHelper GetForMaterial(MyVoxelMaterialDefinition material)
        {
            if (m_preallocatedSingleMaterialHelpers[(int)material.Index] == null)
            {
                m_preallocatedSingleMaterialHelpers[(int)material.Index] = new MySingleMaterialHelper();
                m_preallocatedSingleMaterialHelpers[(int)material.Index].LoadData();
                m_preallocatedSingleMaterialHelpers[(int)material.Index].SetMaterial(material);
            }

            return m_preallocatedSingleMaterialHelpers[(int)material.Index];
            //m_singleMaterialHelper.SetMaterial(material);
            //return m_singleMaterialHelper;
        }

        public static MyMultiMaterialHelper GetForMultimaterial(int material0, int material1, int material2)
        {
            int id = MyVoxelCacheCellRender.GetMultimaterialId(material0, material1, material2);
            MyMultiMaterialHelper helper = null;
            m_preallocatedMultiMaterialHelpers.TryGetValue(id, out helper);
            if (helper == null)
            {
                helper = new MyMultiMaterialHelper();
                helper.LoadData();
                helper.SetMaterials(MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)material0),
                    MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)material1),
                    MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)material2));
                m_preallocatedMultiMaterialHelpers.Add(id, helper);
            }
            return helper;

            //m_multiMaterialHelper.SetMaterials(material0, material1, material2);
            //return m_multiMaterialHelper;
        }

        public static void Begin()
        {
            foreach (MySingleMaterialHelper helper in m_preallocatedSingleMaterialHelpers)
            {
                if (helper != null)
                {
                    helper.IndexCount = 0;
                    helper.VertexCount = 0;
                }
            }

            foreach (var pair in m_preallocatedMultiMaterialHelpers)
            {
                pair.Value.VertexCount = 0;
            }
        }

        public static MySingleMaterialHelper[] GetSingleMaterialHelpers()
        {
            return m_preallocatedSingleMaterialHelpers;
        }

        public static Dictionary<int, MyMultiMaterialHelper> GetMultiMaterialHelpers()
        {
            return m_preallocatedMultiMaterialHelpers;
        }

        internal static void CreateArrayIfNotExist(int matIndex)
        {
            if (SingleMaterialIndicesLookup[matIndex] == null)
            {
                SingleMaterialIndicesLookup[matIndex] = new MySingleMaterialIndexLookup[MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT];
            } 
        }
    }
}
