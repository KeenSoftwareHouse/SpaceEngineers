using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Common;
using VRage.Generics;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    class MyRenderCellBuilder
    {
        private const int MAX_VERTICES_COUNT = ushort.MaxValue;           //  Max number of vertexes we can hold in vertex buffer (because we support only 16-bit m_notCompressedIndex buffer)
        private const int MAX_INDICES_COUNT = 100000;                    //  Max number of indices we can hold in m_notCompressedIndex buffer (because we don't want to have too huge helper arrays). This number doesn't relate to 16-bit indices.
        private const int MAX_VERTICES_COUNT_STOP = MAX_VERTICES_COUNT - 3;
        private const int MAX_INDICES_COUNT_STOP = MAX_INDICES_COUNT - 3;

        private static MyConcurrentQueue<VertexInBatchLookup> SM_BatchLookupPool = new MyConcurrentQueue<VertexInBatchLookup>();
        private static MyConcurrentQueue<SingleMaterialHelper> SM_HelperPool = new MyConcurrentQueue<SingleMaterialHelper>();
        private static MyConcurrentQueue<MultiMaterialHelper> MM_HelperPool = new MyConcurrentQueue<MultiMaterialHelper>();

        private readonly Dictionary<int, VertexInBatchLookup> SM_BatchLookups = new Dictionary<int, VertexInBatchLookup>();
        private readonly Dictionary<int, SingleMaterialHelper> SM_Helpers = new Dictionary<int, SingleMaterialHelper>();
        private readonly Dictionary<int, MultiMaterialHelper> MM_Helpers = new Dictionary<int, MultiMaterialHelper>();

        private readonly Dictionary<Vector3I, Vertex> m_morphMap = new Dictionary<Vector3I, Vertex>(Vector3I.Comparer);
        private List<Vertex> m_lowVertices = new List<Vertex>();
        private List<MyVoxelVertex> m_highVertices = new List<MyVoxelVertex>();
        private List<MyVoxelTriangle> m_highTriangles = new List<MyVoxelTriangle>();
        private List<List<int>> m_lowToHighMapping = new List<List<int>>();
        private List<int> m_highToLowMapping = new List<int>();

     
        struct MorphData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public int Material;
            public float Ambient;
        }

        struct Vertex
        {
            public MorphData Target;
            public Vector3I Cell;
        }

     
        internal void BuildCell(
            MyPrecalcJobRender.Args args,
            MyIsoMesh highResMesh,
            MyIsoMesh lowResMesh,
            List<MyClipmapCellBatch> outBatches,
            out MyClipmapCellMeshMetadata meta)
        {
            Debug.Assert(highResMesh != null);

            meta.Cell = args.Cell;
            meta.PositionOffset = highResMesh.PositionOffset;
            meta.PositionScale = highResMesh.PositionScale;
            meta.LocalAabb = BoundingBox.CreateInvalid();

            m_lowVertices.Clear();
            m_highVertices.Clear();
            m_lowToHighMapping.Clear();
            m_highToLowMapping.Clear();
            m_highTriangles.Clear();


            ProcessLowMesh(highResMesh, lowResMesh);

            //  Increase lookup count, so we will think that all vertices in helper arrays are new
            foreach (var lookup in SM_BatchLookups.Values)
            {
                lookup.ResetBatch();
            }

            //for (int i = 0; i < highResMesh.TrianglesCount; i++)
            //{
            //    var v0 = highResMesh.Triangles[i].VertexIndex0;
            //    var v1 = highResMesh.Triangles[i].VertexIndex1;
            //    var v2 = highResMesh.Triangles[i].VertexIndex2;

            //    MyVoxelVertex vertex;
            //    ProcessHighVertex(highResMesh, v0, ref meta, out vertex);
            //    ProcessHighVertex(highResMesh, v1, ref meta, out vertex);
            //    ProcessHighVertex(highResMesh, v2, ref meta, out vertex);

            //    m_highTriangles.Add(new MyVoxelTriangle()
            //    {
            //        VertexIndex0 = (ushort)(i * 3),
            //        VertexIndex1 = (ushort)(i * 3 + 1),
            //        VertexIndex2 = (ushort)(i * 3 + 2),
            //    });
            //}

            for (int i = 0; i < highResMesh.TrianglesCount; i++)
            {
                m_highTriangles.Add(highResMesh.Triangles[i]);
            }
            for (int i = 0; i < highResMesh.VerticesCount; i++)
            {
                MyVoxelVertex vertex;
                ProcessHighVertex(highResMesh, i, ref meta, out vertex);
            }

            if (lowResMesh != null)
            {
                //Closest vertex

                for (int i = 0; i < m_highVertices.Count; i++)
                {
                    float ldist = float.MaxValue;
                    int bestV = -1;

                    for (int l = 0; l < m_lowVertices.Count; l++)
                    {
                        var dist = Vector3.DistanceSquared(m_lowVertices[l].Target.Position, m_highVertices[i].Position);
                        if (dist < ldist)
                        {
                            ldist = dist;
                            bestV = l;
                        }
                    }

                    var vtx = m_highVertices[i];
                    vtx.PositionMorph = m_lowVertices[bestV].Target.Position;
                    vtx.Normal= m_lowVertices[bestV].Target.Normal;
                    vtx.Material = m_lowVertices[bestV].Target.Material;
                    vtx.Ambient = m_lowVertices[bestV].Target.Ambient;
                    m_highVertices[i] = vtx;
                }



                ////







                //HashSet<int> alreadySetHighTris = new HashSet<int>();

                ////all low tris must have at least one tri from high set, otherwise holes appear. 
                //for (int l = 0; l < lowResMesh.TrianglesCount; l++)
                //{
                //    float triDist = float.MaxValue;
                //    int bestT = -1;

                //    for (int i = 0; i < m_highTriangles.Count; i++)
                //    {
                //        if (alreadySetHighTris.Contains(i))
                //            continue;

                //        float dist = 0;
                //        dist += Vector3.DistanceSquared(m_highVertices[m_highTriangles[i].VertexIndex0].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Position);
                //        dist += Vector3.DistanceSquared(m_highVertices[m_highTriangles[i].VertexIndex1].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Position);
                //        dist += Vector3.DistanceSquared(m_highVertices[m_highTriangles[i].VertexIndex2].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Position);

                //        if (dist < triDist)
                //        {
                //            triDist = dist;
                //            bestT = i;
                //        }
                //    }

                //    if (bestT == -1)
                //    {
                //        //Happens when LOD0 has less tris than LOD1
                //        bestT = 0;
                //    }

                //    alreadySetHighTris.Add(bestT);

                //    var v0 = m_highVertices[m_highTriangles[bestT].VertexIndex0];
                //    v0.PositionMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Position;
                //    v0.NormalMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Normal;
                //    v0.AmbientMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Ambient;
                //    m_highVertices[m_highTriangles[bestT].VertexIndex0] = v0;

                //    var v1 = m_highVertices[m_highTriangles[bestT].VertexIndex1];
                //    v1.PositionMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Position;
                //    v1.NormalMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Normal;
                //    v1.AmbientMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Ambient;
                //    m_highVertices[m_highTriangles[bestT].VertexIndex1] = v1;

                //    var v2 = m_highVertices[m_highTriangles[bestT].VertexIndex2];
                //    v2.PositionMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Position;
                //    v2.NormalMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Normal;
                //    v2.AmbientMorph = m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Ambient;
                //    m_highVertices[m_highTriangles[bestT].VertexIndex2] = v2;
                //}


                //List<MyVoxelTriangle> restHighTriangles = new List<MyVoxelTriangle>(m_highTriangles);
                //List<int> toRemove = new List<int>();
                //foreach (var i in alreadySetHighTris)
                //{
                //    toRemove.Add(i);
                //}
                //toRemove.Sort();

                //for (int i = toRemove.Count - 1; i >= 0; i--)
                //{
                //    restHighTriangles.RemoveAt(toRemove[i]);
                //}



                //for (int i = 0; i < restHighTriangles.Count; i++)
                //{
                //    float triDist = float.MaxValue;
                //    int bestT = -1;

                //    int swtch = 0;


                //    for (int l = 0; l < lowResMesh.TrianglesCount; l++)
                //    {
                //        float dist = 0;
                //        dist += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex0].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Position);
                //        dist += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex1].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Position);
                //        dist += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex2].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Position);

                //        float dist1 = 0;
                //        dist1 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex0].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Position);
                //        dist1 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex1].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Position);
                //        dist1 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex2].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Position);

                //        float dist2 = 0;
                //        dist2 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex0].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex2].Target.Position);
                //        dist2 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex1].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex0].Target.Position);
                //        dist2 += Vector3.DistanceSquared(m_highVertices[restHighTriangles[i].VertexIndex2].Position, m_lowVertices[lowResMesh.Triangles[l].VertexIndex1].Target.Position);

                //        int sw = 0;

                //        if (dist1 < dist && dist1 < dist2)
                //        {
                //            dist = dist1;
                //            sw = 1;
                //        }

                //        if (dist2 < dist && dist2 < dist1)
                //        {
                //            dist = dist2;
                //            sw = 2;
                //        }

                //        if (dist < triDist)
                //        {
                //            triDist = dist;
                //            bestT = l;
                //            swtch = sw;
                //        }
                //    }


                //    //bestT = i % lowResMesh.TrianglesCount;
                //    //bestT = MyUtils.GetRandomInt(lowResMesh.TrianglesCount);

                //    int vi0 = lowResMesh.Triangles[bestT].VertexIndex0;
                //    int vi1 = lowResMesh.Triangles[bestT].VertexIndex1;
                //    int vi2 = lowResMesh.Triangles[bestT].VertexIndex2;

                //    if (swtch == 1)
                //    {
                //        vi0 = lowResMesh.Triangles[bestT].VertexIndex1;
                //        vi1 = lowResMesh.Triangles[bestT].VertexIndex2;
                //        vi2 = lowResMesh.Triangles[bestT].VertexIndex0;
                //    }

                //    if (swtch == 2)
                //    {
                //        vi0 = lowResMesh.Triangles[bestT].VertexIndex2;
                //        vi1 = lowResMesh.Triangles[bestT].VertexIndex0;
                //        vi2 = lowResMesh.Triangles[bestT].VertexIndex1;
                //    }

                //    var v0 = m_highVertices[restHighTriangles[i].VertexIndex0];
                //    v0.PositionMorph = m_lowVertices[vi0].Target.Position;
                //    v0.NormalMorph = m_lowVertices[vi0].Target.Normal;
                //    v0.AmbientMorph = m_lowVertices[vi0].Target.Ambient;
                //    m_highVertices[restHighTriangles[i].VertexIndex0] = v0;

                //    var v1 = m_highVertices[restHighTriangles[i].VertexIndex1];
                //    v1.PositionMorph = m_lowVertices[vi1].Target.Position;
                //    v1.NormalMorph = m_lowVertices[vi1].Target.Normal;
                //    v1.AmbientMorph = m_lowVertices[vi1].Target.Ambient;
                //    m_highVertices[restHighTriangles[i].VertexIndex1] = v1;

                //    var v2 = m_highVertices[restHighTriangles[i].VertexIndex2];
                //    v2.PositionMorph = m_lowVertices[vi2].Target.Position;
                //    v2.NormalMorph = m_lowVertices[vi2].Target.Normal;
                //    v2.AmbientMorph = m_lowVertices[vi2].Target.Ambient;
                //    m_highVertices[restHighTriangles[i].VertexIndex2] = v2;

                //}

                ////add low lod
                //for (int i = 0; i < lowResMesh.TrianglesCount; i++)
                //{
                //    var lt = lowResMesh.Triangles[i];

                //    m_highTriangles.Add(new MyVoxelTriangle()
                //    {
                //        VertexIndex0 = (ushort)(lt.VertexIndex0 + m_highVertices.Count),
                //        VertexIndex1 = (ushort)(lt.VertexIndex1 + m_highVertices.Count),
                //        VertexIndex2 = (ushort)(lt.VertexIndex2 + m_highVertices.Count),
                //    }
                //    );
                //}

                //for (int i = 0; i < m_lowVertices.Count; i++)
                //{
                //    var vertex = new MyVoxelVertex();
                //    vertex.Position = m_lowVertices[i].Target.Position;
                //    vertex.Normal = m_lowVertices[i].Target.Normal;
                //    vertex.Material = m_lowVertices[i].Target.Material;
                //    vertex.Ambient = 0;
                //    vertex.PositionMorph = vertex.Position;
                //    vertex.NormalMorph = vertex.Normal;
                //    vertex.MaterialMorph = vertex.Material;
                //    vertex.AmbientMorph = vertex.Ambient;
                //    m_highVertices.Add(vertex);
                //}
                

            //    List<MyVoxelTriangle> newTriangles = new List<MyVoxelTriangle>();

            //    for (int i = 0; i < m_highTriangles.Count; i++)
            //    {
            //        MyVoxelVertex v0 = m_highVertices[m_highTriangles[i].VertexIndex0];
            //        MyVoxelVertex v1 = m_highVertices[m_highTriangles[i].VertexIndex1];
            //        MyVoxelVertex v2 = m_highVertices[m_highTriangles[i].VertexIndex2];

            //        for (int v = 0; v < m_highVertices.Count; v++)
            //        {
            //            if (v0.Position == m_highVertices[v].Position)
            //            {
            //                if (v0.PositionMorph == m_highVertices[v].PositionMorph)
            //                {
            //                    var t = m_highTriangles[i];
            //                    t.VertexIndex0 = (ushort)v;
            //                    m_highTriangles[i] = t;
            //                    break;
            //                }
            //                else
            //                {
            //                    var vert1 = m_highVertices[m_highTriangles[i].VertexIndex1];
            //                    var vert2 = m_highVertices[m_highTriangles[i].VertexIndex2];

            //                    vert1.Position = m_highVertices[v].Position;
            //                    vert2.Position = m_highVertices[v].Position;

            //                    m_highVertices.Add(vert1);
            //                    m_highVertices.Add(vert2);

            //                    newTriangles.Add(new MyVoxelTriangle()
            //                    {
            //                        VertexIndex0 = (ushort)v,
            //                        VertexIndex1 = (ushort)(m_highVertices.Count - 2),
            //                        VertexIndex2 = (ushort)(m_highVertices.Count - 1)
            //                    });
            //                }
            //            }
            //        }

            //        for (int v = 0; v < m_highVertices.Count; v++)
            //        {
            //            if (v1.Position == m_highVertices[v].Position)
            //            {
            //                if (v1.PositionMorph == m_highVertices[v].PositionMorph)
            //                {
            //                    var t = m_highTriangles[i];
            //                    t.VertexIndex1 = (ushort)v;
            //                    m_highTriangles[i] = t;
            //                    break;
            //                }
            //                else
            //                {
            //                    var vert0 = m_highVertices[m_highTriangles[i].VertexIndex0];
            //                    var vert2 = m_highVertices[m_highTriangles[i].VertexIndex2];

            //                    vert0.Position = m_highVertices[v].Position;
            //                    vert2.Position = m_highVertices[v].Position;

            //                    m_highVertices.Add(vert0);
            //                    m_highVertices.Add(vert2);

            //                    newTriangles.Add(new MyVoxelTriangle()
            //                    {
            //                        VertexIndex0 = (ushort)(m_highVertices.Count - 2),
            //                        VertexIndex1 = (ushort)v,
            //                        VertexIndex2 = (ushort)(m_highVertices.Count - 1)
            //                    });
            //                }
                      
            //            }
            //        }

            //        for (int v = 0; v < m_highVertices.Count; v++)
            //        {
            //            if (v2.Position == m_highVertices[v].Position)
            //            {
            //                if (v2.PositionMorph == m_highVertices[v].PositionMorph)
            //                {
            //                    var t = m_highTriangles[i];
            //                    t.VertexIndex2 = (ushort)v;
            //                    m_highTriangles[i] = t;
            //                    break;
            //                }
            //                else
            //                {
            //                    var vert0 = m_highVertices[m_highTriangles[i].VertexIndex0];
            //                    var vert1 = m_highVertices[m_highTriangles[i].VertexIndex1];

            //                    vert0.Position = m_highVertices[v].Position;
            //                    vert1.Position = m_highVertices[v].Position;

            //                    m_highVertices.Add(vert0);
            //                    m_highVertices.Add(vert1);

            //                    newTriangles.Add(new MyVoxelTriangle()
            //                    {
            //                        VertexIndex0 = (ushort)(m_highVertices.Count - 2),
            //                        VertexIndex1 = (ushort)(m_highVertices.Count - 1),
            //                        VertexIndex2 = (ushort)v
            //                    });
            //                }
                         
            //            }
            //        }
            //    }


            //    foreach (var t in newTriangles)
            //    {
            //        m_highTriangles.Add(t);
            //    }


                float largestDistance = float.MinValue;
                for (int i = 0; i < m_highVertices.Count; i++)
                {
                    MyVoxelVertex vertex = m_highVertices[i];
                    float p1 = vertex.Position.AbsMax();
                    if (p1 > largestDistance)
                        largestDistance = p1;
                    float p2 = vertex.PositionMorph.AbsMax();
                    if (p2 > largestDistance)
                        largestDistance = p2;
                }


                for (int i = 0; i < m_highVertices.Count; i++)
                {
                    MyVoxelVertex vertex = m_highVertices[i];

                    vertex.Position /= largestDistance;
                    vertex.PositionMorph /= largestDistance;

                    m_highVertices[i] = vertex;

                }

                meta.PositionScale *= largestDistance;
            }
            else
            {
            }

         

            for (int i = 0; i < m_highVertices.Count; i++)
            {
                MyVoxelVertex vertex = m_highVertices[i];

                meta.LocalAabb.Include(vertex.Position * meta.PositionScale + meta.PositionOffset);
                meta.LocalAabb.Include(vertex.PositionMorph * meta.PositionScale + meta.PositionOffset);

                Debug.Assert(vertex.Position.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                Debug.Assert(vertex.PositionMorph.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
            }
            

            for (int i = 0; i < m_highTriangles.Count; i++)
            {
                MyVoxelTriangle srcTriangle = m_highTriangles[i];
                MyVoxelVertex vertex0, vertex1, vertex2;
                vertex0 = m_highVertices[srcTriangle.VertexIndex0];
                vertex1 = m_highVertices[srcTriangle.VertexIndex1];
                vertex2 = m_highVertices[srcTriangle.VertexIndex2];
                
                if (vertex0.Material == vertex1.Material &&
                    vertex0.Material == vertex2.Material &&
                    vertex0.Material == vertex0.MaterialMorph &&
                    vertex0.Material == vertex1.MaterialMorph &&
                    vertex0.Material == vertex2.MaterialMorph)
                { // single material
                    var matIdx = vertex0.Material;

                    //  This is single-texture triangleVertexes, so we can choose material from any edge
                    SingleMaterialHelper materialHelper;
                    if (!SM_Helpers.TryGetValue(matIdx, out materialHelper))
                    {
                        if (!SM_HelperPool.TryDequeue(out materialHelper))
                            materialHelper = new SingleMaterialHelper();
                        materialHelper.Material = matIdx;
                        SM_Helpers.Add(matIdx, materialHelper);
                    }

                    VertexInBatchLookup batchLookup;
                    if (!SM_BatchLookups.TryGetValue(matIdx, out batchLookup))
                    {
                        if (!SM_BatchLookupPool.TryDequeue(out batchLookup))
                            batchLookup = new VertexInBatchLookup();
                        SM_BatchLookups.Add(matIdx, batchLookup);
                    }

                    AddVertexToBuffer(materialHelper, ref vertex0, batchLookup, srcTriangle.VertexIndex0);
                    AddVertexToBuffer(materialHelper, ref vertex1, batchLookup, srcTriangle.VertexIndex1);
                    AddVertexToBuffer(materialHelper, ref vertex2, batchLookup, srcTriangle.VertexIndex2);

                    //  Add indices
                    int nextTriangleIndex = materialHelper.IndexCount;
                    materialHelper.Indices[nextTriangleIndex + 0] = batchLookup.GetIndexInBatch(srcTriangle.VertexIndex0);
                    materialHelper.Indices[nextTriangleIndex + 1] = batchLookup.GetIndexInBatch(srcTriangle.VertexIndex1);
                    materialHelper.Indices[nextTriangleIndex + 2] = batchLookup.GetIndexInBatch(srcTriangle.VertexIndex2);
                    materialHelper.IndexCount += 3;

                    if ((materialHelper.VertexCount >= MAX_VERTICES_COUNT_STOP) ||
                        (materialHelper.IndexCount >= MAX_INDICES_COUNT_STOP))
                    {
                        //  If this batch is almost full (or is full), we end it and start with new one
                        EndSingleMaterial(materialHelper, outBatches);
                    }
                }
                else
                {
                    Vector3I materials = GetMaterials(ref vertex0, ref vertex1, ref vertex2);
                    var voxelMaterialCount = MyDefinitionManager.Static.VoxelMaterialCount;
                    int id = materials.X + voxelMaterialCount * (materials.Y + materials.Z * voxelMaterialCount);

                    // Assign current material
                    MultiMaterialHelper helper = null;
                    if (!MM_Helpers.TryGetValue(id, out helper))
                    {
                        if (!MM_HelperPool.TryDequeue(out helper))
                            helper = new MultiMaterialHelper();
                        helper.Material0 = materials.X;
                        helper.Material1 = materials.Y;
                        helper.Material2 = materials.Z;
                        MM_Helpers.Add(id, helper);
                    }

                    helper.AddVertex(ref vertex0);
                    helper.AddVertex(ref vertex1);
                    helper.AddVertex(ref vertex2);

                    if (helper.Vertices.Count >= MAX_VERTICES_COUNT_STOP)
                    {
                        EndMultiMaterial(helper, outBatches);
                    }
                }
            }

            { //renderCell.End();
                foreach (var helper in SM_Helpers.Values)
                {
                    Debug.Assert(helper != null);
                    if (helper.IndexCount > 0)
                    {
                        EndSingleMaterial(helper, outBatches);
                    }
                    helper.IndexCount = 0;
                    helper.VertexCount = 0;
                    SM_HelperPool.Enqueue(helper);
                }
                SM_Helpers.Clear();

                foreach (var helper in MM_Helpers.Values)
                {
                    if (helper.Vertices.Count > 0)
                    {
                        EndMultiMaterial(helper, outBatches);
                    }
                    helper.Vertices.Clear();
                    MM_HelperPool.Enqueue(helper);
                }
                MM_Helpers.Clear();

                foreach (var lookup in SM_BatchLookups.Values)
                {
                    SM_BatchLookupPool.Enqueue(lookup);
                }
                SM_BatchLookups.Clear();
            }

            m_morphMap.Clear();
        }

        private void ProcessLowMesh(MyIsoMesh highResMesh, MyIsoMesh lowResMesh)
        {
            if (lowResMesh != null)
            {
                /*
                 * Derived transformation of normalized coordinates from low res mesh to high res mesh.
                 * _l is for low res, _h for high res, x is vertex position
                 x = x_l * scale_l + offset_l
                 x_h = (x - offset_h) / scale_h
                 x_h = ((x_l * scale_l + offset_l) - offset_h) / scale_h
                 x_h = (x_l * scale_l + offset_l - offset_h) / scale_h
                 x_h = x_l * (scale_l / scale_h) + ((offset_l - offset_h) / scale_h)
                 */
                Vector3 morphOffset, morphScale;
                morphScale = lowResMesh.PositionScale / highResMesh.PositionScale;
                morphOffset = (Vector3)(lowResMesh.PositionOffset - highResMesh.PositionOffset) / highResMesh.PositionScale;
                for (int i = 0; i < lowResMesh.VerticesCount; ++i)
                {
                    var vertex = new Vertex
                    {
                        Target = new MorphData
                        {
                            Position = lowResMesh.Positions[i] * morphScale + morphOffset,
                            Normal = lowResMesh.Normals[i],
                            Material = lowResMesh.Materials[i],
                            Ambient = lowResMesh.Ambient[i],
                        },
                        Cell = lowResMesh.Cells[i],
                    };

                    m_lowVertices.Add(vertex);
                    m_lowToHighMapping.Add(new List<int>());
                    m_morphMap[vertex.Cell] = vertex;
                }
            }
        }



        private void ProcessHighVertex(MyIsoMesh mesh, int vertexIndex, ref MyClipmapCellMeshMetadata meta, out MyVoxelVertex vertex)
        {
            vertex = new MyVoxelVertex();
            vertex.Position = mesh.Positions[vertexIndex];
            vertex.Normal = mesh.Normals[vertexIndex];
            vertex.Material = mesh.Materials[vertexIndex];
            vertex.Ambient = mesh.Ambient[vertexIndex];
            vertex.Cell = mesh.Cells[vertexIndex];
            vertex.PositionMorph = vertex.Position;
            vertex.NormalMorph = vertex.Normal;
            vertex.MaterialMorph = vertex.Material;
            vertex.AmbientMorph = vertex.Ambient;
            
            
            //Vertex morph;
            //Vector3I lowerLodCell = (mesh.Cells[vertexIndex]) >> 1;

            //if (m_morphMap.TryGetValue(lowerLodCell, out morph))
            //{
            //    vertex.PositionMorph = morph.Target.Position;
            //    vertex.NormalMorph = morph.Target.Normal;
            //    vertex.MaterialMorph = morph.Target.Material;
            //    vertex.AmbientMorph = morph.Target.Ambient;
            //}
            //else
            //{

            //}

            //if (m_lowVertices.Count > 0)
            //{

            //    float closestDistance = float.MaxValue;
            //    int closestLowVertex = -1;
            //    for (int i = 0; i < m_lowVertices.Count; i++)
            //    {
            //        float dist = Vector3.DistanceSquared(vertex.Position, m_lowVertices[i].Target.Position);
            //        if (dist < closestDistance)
            //        {
            //            closestDistance = dist;
            //            closestLowVertex = i;
            //        }
            //    }

            //    vertex.PositionMorph = m_lowVertices[closestLowVertex].Target.Position;
            //    vertex.NormalMorph = m_lowVertices[closestLowVertex].Target.Normal;
            //    vertex.MaterialMorph = m_lowVertices[closestLowVertex].Target.Material;
            //    vertex.AmbientMorph = m_lowVertices[closestLowVertex].Target.Ambient;

            //    m_lowToHighMapping[closestLowVertex].Add(vertexIndex);
            //    m_highToLowMapping.Add(closestLowVertex);
            //}

            m_highVertices.Add(vertex);
        }

        private void EndSingleMaterial(SingleMaterialHelper materialHelper, List<MyClipmapCellBatch> outBatches)
        {
            //Synchronize to VRage render
            if (materialHelper.IndexCount > 0 && materialHelper.VertexCount > 0)
            {
                //Todo - is it possible without allocations?
                MyVertexFormatVoxelSingleData[] vertices = new MyVertexFormatVoxelSingleData[materialHelper.VertexCount];
                Array.Copy(materialHelper.Vertices, vertices, vertices.Length);
                ushort[] indices = new ushort[materialHelper.IndexCount];
                Array.Copy(materialHelper.Indices, indices, indices.Length);

                outBatches.Add(new MyClipmapCellBatch()
                {
                    Vertices = vertices,
                    Indices = indices,
                    Material0 = materialHelper.Material,
                    Material1 = -1,
                    Material2 = -1,
                });
            }

            //  Reset helper arrays, so we can start adding triangles to them again
            materialHelper.IndexCount = 0;
            materialHelper.VertexCount = 0;
            SM_BatchLookups[materialHelper.Material].ResetBatch();
        }

        /// <summary>
        /// Multimaterial vertices are not removing duplicities using indices.
        /// They just add indexing on top of duplicating vertices.
        /// </summary>
        private void EndMultiMaterial(MultiMaterialHelper helper, List<MyClipmapCellBatch> outBatches)
        {
            if (helper.Vertices.Count > 0)
            {
                //Todo - is it possible without allocations?
                MyVertexFormatVoxelSingleData[] vertices = new MyVertexFormatVoxelSingleData[helper.Vertices.Count];
                Array.Copy(helper.Vertices.GetInternalArray(), vertices, vertices.Length);

                ushort[] indices = new ushort[helper.Vertices.Count];
                for (ushort i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                outBatches.Add(new MyClipmapCellBatch()
                {
                    Vertices = vertices,
                    Indices = indices,
                    Material0 = helper.Material0,
                    Material1 = helper.Material1,
                    Material2 = helper.Material2,
                });
            }

            //  Reset helper arrays, so we can start adding triangles to them again
            helper.Vertices.Clear();
        }

        private unsafe void AddIfNotPresent(int* buffer, ref int count, int length, int value)
        {
            if (count == length)
                return;

            for (int i = 0; i < count; i++)
            {
                if (buffer[i] == value)
                    return;
            }

            buffer[count++] = value;
        }

        private Vector3I GetMaterials(ref MyVoxelVertex v0, ref MyVoxelVertex v1, ref MyVoxelVertex v2)
        {
            unsafe
            {
                const int BUFFER_SIZE = 3;
                int count = 0;
                int* materials = stackalloc int[BUFFER_SIZE];
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v0.Material);
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v1.Material);
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v2.Material);
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v0.MaterialMorph);
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v1.MaterialMorph);
                AddIfNotPresent(materials, ref count, BUFFER_SIZE, v2.MaterialMorph);
                while (count < BUFFER_SIZE)
                {
                    materials[count++] = 0;
                }
                if (materials[0] > materials[1]) MyUtils.Swap(ref materials[0], ref materials[1]);
                if (materials[1] > materials[2]) MyUtils.Swap(ref materials[1], ref materials[2]);
                if (materials[0] > materials[1]) MyUtils.Swap(ref materials[0], ref materials[1]);
                return new Vector3I(materials[0], materials[1], materials[2]);
            }
        }

        private void AddVertexToBuffer(SingleMaterialHelper materialHelper, ref MyVoxelVertex vertex,
            VertexInBatchLookup inBatchLookup, ushort srcVertexIdx)
        {
            if (!inBatchLookup.IsInBatch(srcVertexIdx))
            {
                int tgtVertexIdx = materialHelper.VertexCount;

                //  Short overflow check
                Debug.Assert(tgtVertexIdx <= ushort.MaxValue);

                materialHelper.Vertices[tgtVertexIdx].Position = vertex.Position;
                materialHelper.Vertices[tgtVertexIdx].PositionMorph = vertex.PositionMorph;
                materialHelper.Vertices[tgtVertexIdx].Ambient = vertex.Ambient;
                materialHelper.Vertices[tgtVertexIdx].AmbientMorph = vertex.AmbientMorph;
                materialHelper.Vertices[tgtVertexIdx].Normal = vertex.Normal;
                materialHelper.Vertices[tgtVertexIdx].NormalMorph = vertex.NormalMorph;

                inBatchLookup.PutToBatch(srcVertexIdx, (ushort)tgtVertexIdx);

                materialHelper.VertexCount++;
            }
        }

        /// <summary>
        /// Helper for mapping original vertex indices (within geometry cells?)
        /// to indices for vertex buffers. This is because render cell can
        /// require more batches even for single material (when number of
        /// vertices exceeds MAX_VERTICES_COUNT).
        /// </summary>
        class VertexInBatchLookup
        {
            struct VertexData
            {
                public ushort IndexInBatch;
                public int BatchId;
            }

            private readonly VertexData[] m_data = new VertexData[MyRenderCellBuilder.MAX_VERTICES_COUNT];

            /// <summary>
            /// Incremented at the beginning of geometry cell and when ending single material.
            /// Compared to CalcCounter in Data for a vertex and when different, vertex is added to buffer and CalcCounter updated so it's not added again.
            /// </summary>
            private int m_idCounter = 1;

            public bool IsInBatch(int vertexIndex)
            {
                return m_data[vertexIndex].BatchId == m_idCounter;
            }

            internal void PutToBatch(ushort vertexIndex, ushort indexInBatch)
            {
                m_data[vertexIndex].BatchId = m_idCounter;
                m_data[vertexIndex].IndexInBatch = indexInBatch;
            }

            internal void ResetBatch()
            {
                ++m_idCounter;
            }

            internal ushort GetIndexInBatch(int vertexIndex)
            {
                return m_data[vertexIndex].IndexInBatch;
            }
        }

        class SingleMaterialHelper
        {
            public readonly MyVertexFormatVoxelSingleData[] Vertices = new MyVertexFormatVoxelSingleData[MyRenderCellBuilder.MAX_VERTICES_COUNT];
            public readonly ushort[] Indices = new ushort[MyRenderCellBuilder.MAX_INDICES_COUNT];

            public int Material;
            public int VertexCount;
            public int IndexCount;
        }

        class MultiMaterialHelper
        {
            public readonly List<MyVertexFormatVoxelSingleData> Vertices = new List<MyVertexFormatVoxelSingleData>();
            public int Material0;
            public int Material1;
            public int Material2;

            public void AddVertex(ref MyVoxelVertex vertex)
            {
                Debug.Assert(Material0 != Material1 || Material0 != Material2 || Material1 != Material2);

                var material = vertex.Material;
                byte alphaIndex;
                if (Material0 == material)
                    alphaIndex = 0;
                else if (Material1 == material)
                    alphaIndex = 1;
                else if (Material2 == material)
                    alphaIndex = 2;
                else
                    throw new System.InvalidOperationException("Should not be there, invalid material");

                byte materialMorph = alphaIndex;
                if (Material0 == vertex.MaterialMorph) materialMorph = 0;
                else if (Material1 == vertex.MaterialMorph) materialMorph = 1;
                else if (Material2 == vertex.MaterialMorph) materialMorph = 2;

                Vertices.Add(new MyVertexFormatVoxelSingleData()
                {
                    Position = vertex.Position,
                    PositionMorph = vertex.PositionMorph,
                    Ambient = vertex.Ambient,
                    AmbientMorph = vertex.AmbientMorph,
                    Normal = vertex.Normal,
                    NormalMorph = vertex.NormalMorph,
                    Material = alphaIndex,
                    MaterialMorph = materialMorph,
                });
            }
        }

    }
}
