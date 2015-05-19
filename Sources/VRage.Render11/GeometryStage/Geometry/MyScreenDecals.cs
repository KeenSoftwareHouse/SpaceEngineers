using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    struct MyScreenDecal
    {
        internal Matrix LocalOBB;
        internal uint ID; // backref
        internal uint ParentID;
        internal MyMeshMaterialId Material;
    }

    internal class MyScreenDecalComparer : IComparer<int>
    {
        MyFreelist<MyScreenDecal> m_freelist;

        public MyScreenDecalComparer(MyFreelist<MyScreenDecal> freelist)
        {
            m_freelist = freelist;
        }

        public int Compare(int x, int y)
        {
            return m_freelist.Data[x].Material.Index.CompareTo(m_freelist.Data[y].Material.Index);
        }
    }
    
    static class MyScreenDecals
    {
        static VertexShaderId m_vs = VertexShaderId.NULL;
        static PixelShaderId m_ps = PixelShaderId.NULL;

        internal static Dictionary<uint, int> IdIndex = new Dictionary<uint,int>();
        internal static Dictionary<uint, List<int>> EntityDecals = new Dictionary<uint, List<int>>();
        internal static MyFreelist<MyScreenDecal> Decals = new MyFreelist<MyScreenDecal>(1024);

        public static MyScreenDecalComparer DecalsMaterialComparer = new MyScreenDecalComparer(Decals);

        internal static void Init()
        {
            //m_vs = MyShaders.CreateVs("decal.hlsl", "vs");
            //m_ps = MyShaders.CreatePs("decal.hlsl", "ps");
            
        }

        internal static void AddDecal(uint ID, uint ParentID, Matrix localOBB)
        {
            var handle = Decals.Allocate();

            Decals.Data[handle].ID = ID;
            Decals.Data[handle].ParentID = ParentID;
            Decals.Data[handle].LocalOBB = localOBB;

            IdIndex[ID] = handle;
            
            if(!EntityDecals.ContainsKey(ParentID))
            {
                EntityDecals[ParentID] = new List<int>();
            }
            EntityDecals[ParentID].Add(handle);
        }

        internal static void RemoveDecal(uint ID)
        {
            var handle = IdIndex[ID];
            var parent = Decals.Data[handle].ParentID;
            EntityDecals[parent].Remove(handle);
            Decals.Free(handle);
            IdIndex.Remove(ID);
        }

        internal static void RemoveDecalByHandle(int handle)
        {
            IdIndex.Remove(Decals.Data[handle].ID);
            Decals.Free(handle);
        }

        internal static void RemoveEntityDecals(uint id)
        {
            if (!EntityDecals.ContainsKey(id))
            {
                return;
            }

            foreach (var handle in EntityDecals[id])
            {
                RemoveDecalByHandle(handle);
            }

            EntityDecals[id] = null;
        }

        // can be on another job
        internal static void Draw()
        {
            // calculate box ()
            var cubeVertices = new Vector3[8] {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f,  0.5f, -0.5f),
                    new Vector3( 0.5f,  0.5f, -0.5f),
                    new Vector3( 0.5f, -0.5f, -0.5f),

                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f,  0.5f, 0.5f),
                    new Vector3( 0.5f,  0.5f, 0.5f),
                    new Vector3( 0.5f, -0.5f, 0.5f)
                };

            var worldVertices = new Vector3[8];

            var decals = IdIndex.Values.ToArray();
            // sort visible decals by material
            Array.Sort(decals, DecalsMaterialComparer);

            ///
            // copy gbuffer with normals for read (uhoh)
            // bind copy and depth for read
            // bind gbuffer for write

            var batch = MyLinesRenderer.CreateBatch();

            for (int i = 0; i < decals.Length; ++i)
            {
                var index = decals[i];

                var parentMatrix = MyIDTracker<MyActor>.FindByID(Decals.Data[index].ParentID).WorldMatrix;
                var volumeMatrix = Decals.Data[index].LocalOBB * parentMatrix;
                var invVolumeMatrix = Matrix.Invert(volumeMatrix);

                Vector3.Transform(cubeVertices, ref volumeMatrix, worldVertices);

                batch.Add6FacedConvex(worldVertices, Color.Red);
            }

            batch.Commit();
        }

    }
}
