using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.FileSystem;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender.Import;
using VRageRender.Vertex;

namespace VRageRender
{
    // a bit duplicated with MyDrawSubmesh 
    // stores raw info from file
    struct MySubmeshInfo
    {
        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;

        internal int[] BonesMapping;

        internal MyMeshDrawTechnique Technique; 
        internal string Material;
    }

    class MyRenderLodInfo
    {
        internal int LodNum;
        internal float Distance;

        internal MyRenderMeshInfo m_meshInfo;
        //internal MyVertexDataProxy m_vdProxy;

        internal MyRenderLodInfo()
        {
            LodNum = 1;
            Distance = 0;
        }
    }

    class MyRenderMeshInfo
    {
        internal MyVertexInputLayout VertexLayout;
        internal IVertexBuffer[] VB;
        internal IIndexBuffer IB;
        internal int Id;
        internal Dictionary<MyMeshDrawTechnique, MyDrawSubmesh[]> Parts = new Dictionary<MyMeshDrawTechnique, MyDrawSubmesh[]>();
        internal Dictionary<MyMeshDrawTechnique, MySubmeshInfo[]> PartsMetadata = new Dictionary<MyMeshDrawTechnique, MySubmeshInfo[]>(); // well, we need this too after all
        internal int PartsNum { get { return Parts.Count; } }
        internal BoundingBox? BoundingBox;
        internal BoundingSphere? BoundingSphere;
        internal bool IsAnimated { get; set; }

        // kind of duplicated info with MyDrawSubmesh - should be merged
        // this one stores more "metadata" kind of info, so can be used by other parts
        //internal Dictionary<string, MySubmeshInfo[]> m_submeshes = new Dictionary<string, MySubmeshInfo[]>();
        internal List<MySubmeshInfo> m_submeshes = new List<MySubmeshInfo>();

        internal int VerticesNum;
        internal int IndicesNum;

        // raw data for instancing mostly
        internal ushort[] Indices;
        internal MyVertexFormatPositionH4[] VertexPositions;
        internal MyVertexFormatTexcoordNormalTangent[] VertexExtendedData;

        internal void ReleaseBuffers()
        {
            if(IB != null)
            {
                MyManagers.Buffers.Dispose(IB);
                IB = null;
            }
            if(VB != null)
            {
                foreach(var vb in VB)
                {
                    //vb.Dispose();
                    MyManagers.Buffers.Dispose(vb);
                }
                VB = null;
            }
        }
    }

    // just to clarify parts for now
    enum MyAssetLoadingEnum
    {
        Unassigned,
        Waiting,
        Ready
    }

    // data abstraction for meshes in game
    class MyMesh
    {
        protected string m_name;

        internal string Name { get { return m_name; } }
        internal MyRenderLodInfo[] LODs;
        internal MyAssetLoadingEnum LoadingStatus { get { return m_loadingStatus; } }
        internal bool IsAnimated { get; set; }

        protected MyAssetLoadingEnum m_loadingStatus;

        internal MyMesh()
        {
            LODs = null;
            m_loadingStatus = MyAssetLoadingEnum.Unassigned;
            IsAnimated = false;
        }

        internal virtual void Release()
        {
            foreach(var lod in LODs)
            {
                lod.m_meshInfo.ReleaseBuffers();
            }

            LODs = null;
            m_loadingStatus = MyAssetLoadingEnum.Unassigned;
        }

        // up to 10 bytes!
        internal virtual int GetSortingID(int lodNum)
        {
            return 0;
        }

        internal void DebugWriteInfo()
        {
            Debug.WriteLine(String.Format("Loaded mesh: {0}, lods: {1}, closest lod parts: {2}", m_name, LODs.Length, LODs[0].m_meshInfo.PartsNum));
            for (int i = 0; i < LODs.Length; i++)
            {
                Debug.WriteLine(String.Format("\tlod {0} : V {1}, I {2}", i, LODs[i].m_meshInfo.VerticesNum, LODs[i].m_meshInfo.IndicesNum));
            }
        }
    }


    class MyAssetsLoader
    {
        internal const bool LOG_MESH_STATISTICS = false;

        static Dictionary<string, MyAssetMesh> m_meshes = new Dictionary<string, MyAssetMesh>();
        internal static Dictionary<string, string> ModelRemap = new Dictionary<string, string>();

        internal static MyAssetMesh m_debugMesh;

        internal static MyAssetMesh GetModel(string assetName)
        {
            var model = m_meshes.Get(assetName);
            if(model != null)
            {
                return model;
            }

            model = new MyAssetMesh(assetName);
            model.LoadAsset();

            if (LOG_MESH_STATISTICS)
            {
                model.DebugWriteInfo();
            }

            m_meshes[assetName] = model;
            LogModel(model, assetName);
            return model;
        }

        internal static MyAssetMesh GetDebugMesh()
        {
            // TODO: sth better
            return GetModel("Models\\Cubes\\large\\StoneSlope.mwm");
        }

        internal static float LoadedMeshSize = 0;
        [Conditional("DEBUG")]
        private static void LogModel(MyAssetMesh mesh, string assetName)
        {
            var fsPath = Path.IsPathRooted(assetName) ? assetName : Path.Combine(MyFileSystem.ContentPath, assetName);
            var fi = new FileInfo(fsPath);
            if (MyPerformanceCounter.LogFiles)
                MyPerformanceCounter.PerAppLifetime.LoadedModelFiles.Add(fsPath);

            if (!fi.Exists)
                return;

            MyPerformanceCounter.PerAppLifetime.MyModelsFilesSize += (int)fi.Length;
            MyPerformanceCounter.PerAppLifetime.ModelsCount++;
            foreach(var lod in mesh.LODs)
            {
                foreach (var vb in lod.m_meshInfo.VB)
                {
                    MyPerformanceCounter.PerAppLifetime.ModelVertexBuffersSize += vb.ByteSize;
                    MyPerformanceCounter.PerAppLifetime.MyModelsVertexesCount += vb.ElementCount;
                }

                MyPerformanceCounter.PerAppLifetime.ModelIndexBuffersSize += lod.m_meshInfo.IB.ByteSize;
            }
        }

        internal static MyAssetMesh GetMaterials(string assetName)
        {
            var model = m_meshes.Get(assetName);
            if (model != null)
            {
                return model;
            }

            MyAssetMesh.LoadMaterials(assetName);

            return model;
        }

        internal static void ReloadMeshes()
        {
            foreach (var mesh in m_meshes)
            { 
                mesh.Value.LoadAsset();
            }
        }

        internal static void ClearMeshes()
        {
            foreach(var mesh in m_meshes)
            {
                mesh.Value.Release();
            }
            m_meshes.Clear();
        }
    }
}
