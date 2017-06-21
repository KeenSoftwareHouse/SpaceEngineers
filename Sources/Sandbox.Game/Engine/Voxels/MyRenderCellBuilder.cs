using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageMath.Spatial;
using VRageRender.Messages;

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

        private MyVector3Grid<int> m_highGrid = new MyVector3Grid<int>(0.5f);

     
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
            ProfilerShort.Begin("MyRenderCellBuilder.BuildCell");
            Debug.Assert(highResMesh != null);

            meta.Cell = args.Cell;
            meta.PositionOffset = highResMesh.PositionOffset;
            meta.PositionScale = highResMesh.PositionScale;
            meta.LocalAabb = BoundingBox.CreateInvalid();

            m_lowVertices.SetSize(0);
            m_highVertices.SetSize(0);

            ProcessLowMesh(highResMesh, lowResMesh);

            //  Increase lookup count, so we will think that all vertices in helper arrays are new
            foreach (var lookup in SM_BatchLookups.Values)
            {
                lookup.ResetBatch();
            }

    
            for (int i = 0; i < highResMesh.VerticesCount; i++)
            {
                MyVoxelVertex vertex;
                ProcessHighVertex(highResMesh, i, out vertex);
            }

            if (lowResMesh != null)
            {
                m_highGrid.ClearFast();

                for (int i = 0; i < m_highVertices.Count; i++)
                {
                    Vector3 position = m_highVertices[i].Position;
                    m_highGrid.AddPoint(ref position, i);
                }

                //TODO: Fix ocassional bad triangles on asteroid
                /*
                //Closest vertex
                for (int l = 0; l < m_lowVertices.Count; l++)
                {
                    Vector3 targetPosition = m_lowVertices[l].Target.Position;

                    int bestV = -1;
                    float ldist = float.MaxValue;
                    float startMeters = 1;
                    float maxDistance = 10;

                    MyVector3Grid<int>.Enumerator points = default(MyVector3Grid<int>.Enumerator);
                    while (startMeters < maxDistance)
                    {
                        points = m_highGrid.GetPointsCloserThan(ref targetPosition, startMeters);

                        while (points.MoveNext())
                        {
                            var dist = Vector3.DistanceSquared(targetPosition, m_highVertices[points.Current].Position);
                            if (dist < ldist)
                            {
                                ldist = dist;
                                bestV = points.Current;
                            }
                        }

                        if (bestV != -1)
                            break;

                        startMeters += 1;
                    }

                    if (bestV != -1)
                    {
                        var vtx = m_highVertices[bestV];
                        vtx.PositionMorph = m_lowVertices[l].Target.Position;
                        vtx.NormalMorph = m_lowVertices[l].Target.Normal;
                        vtx.MaterialMorph = m_lowVertices[l].Target.Material;
                        vtx.AmbientMorph = m_lowVertices[l].Target.Ambient;
                        m_highVertices[bestV] = vtx;
                    }
                    else
                    {
                    }
                }

                */

                //Closest vertex
                float largestDistance = float.MinValue;
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

                    var highVertex = m_highVertices[i];
                    highVertex.PositionMorph = m_lowVertices[bestV].Target.Position;
                    highVertex.NormalMorph = m_lowVertices[bestV].Target.Normal;
                    highVertex.MaterialMorph = m_lowVertices[bestV].Target.Material;
                    highVertex.AmbientMorph = m_lowVertices[bestV].Target.Ambient;

                    float p1 = highVertex.Position.AbsMax();
                    if (p1 > largestDistance)
                        largestDistance = p1;
                    float p2 = highVertex.PositionMorph.AbsMax();
                    if (p2 > largestDistance)
                        largestDistance = p2;

                    m_highVertices[i] = highVertex;
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
            
            //Create batches
            for (int i = 0; i < m_highVertices.Count; i++)
            {
                MyVoxelVertex vertex = m_highVertices[i];

                meta.LocalAabb.Include(vertex.Position * meta.PositionScale + meta.PositionOffset);
                meta.LocalAabb.Include(vertex.PositionMorph * meta.PositionScale + meta.PositionOffset);

                Debug.Assert(vertex.Position.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                Debug.Assert(vertex.PositionMorph.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
            }

            for (int i = 0; i < highResMesh.TrianglesCount; i++)
            {
                MyVoxelTriangle srcTriangle = highResMesh.Triangles[i];
                MyVoxelVertex vertex0 = m_highVertices[srcTriangle.VertexIndex0];
                MyVoxelVertex vertex1 = m_highVertices[srcTriangle.VertexIndex1];
                MyVoxelVertex vertex2 = m_highVertices[srcTriangle.VertexIndex2];
                
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

                    uint matInfo = (uint)matIdx | ((uint)matIdx << 8) | ((uint)matIdx << 16) | ((uint)matIdx << 24);

                    AddVertexToBuffer(materialHelper, ref vertex0, batchLookup, srcTriangle.VertexIndex0, matInfo);
                    AddVertexToBuffer(materialHelper, ref vertex1, batchLookup, srcTriangle.VertexIndex1, matInfo);
                    AddVertexToBuffer(materialHelper, ref vertex2, batchLookup, srcTriangle.VertexIndex2, matInfo);

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

#if false
                    Debug.Assert(materials.X < 1 << 10, "Too many materials");
                    Debug.Assert(materials.Y < 1 << 10, "Too many materials");
                    Debug.Assert(materials.Z < 1 << 10, "Too many materials");
#else
                    //
                    // Right now, we are encoding material indices into per-vertex Byte4 structure, so max
                    // number of materials is 256. If this is not sufficient, than vertex data structure
                    // holding materials indices must be changed to something like UShort4.
                    //
                    Debug.Assert(materials.X < 256, "Too many materials");
                    Debug.Assert(materials.Y < 256, "Too many materials");
                    Debug.Assert(materials.Z < 256, "Too many materials");
#endif

                    int id = materials.X + (materials.Y + (materials.Z << 10) << 10);

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

                    uint V0MatIdx = (uint)vertex0.Material;
                    uint V1MatIdx = (uint)vertex1.Material;
                    uint V2MatIdx = (uint)vertex2.Material;

                    uint matInfoBase = V0MatIdx | (V1MatIdx << 8) | (V2MatIdx << 16);

                    helper.AddVertex(ref vertex0, matInfoBase | (V0MatIdx << 24));
                    helper.AddVertex(ref vertex1, matInfoBase | (V1MatIdx << 24));
                    helper.AddVertex(ref vertex2, matInfoBase | (V2MatIdx << 24));

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
            meta.BatchCount = outBatches.Count;
            ProfilerShort.End();
        }

        private void ProcessLowMesh(MyIsoMesh highResMesh, MyIsoMesh lowResMesh)
        {
            ProfilerShort.Begin("ProcessLowMesh");
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
                    m_morphMap[vertex.Cell] = vertex;
                }
            }
            ProfilerShort.End();
        }



        private void ProcessHighVertex(MyIsoMesh mesh, int vertexIndex, out MyVoxelVertex vertex)
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
                uint[] indices = new uint[materialHelper.IndexCount];
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

                uint[] indices = new uint[helper.Vertices.Count];
                for (ushort i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                outBatches.Add(new MyClipmapCellBatch
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
            VertexInBatchLookup inBatchLookup, ushort srcVertexIdx, uint matInfo)
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

                // NOTE: I don't know C#, so I am not sure if this is OK performance wise
                materialHelper.Vertices[tgtVertexIdx].MaterialInfo = new VRageMath.PackedVector.Byte4(matInfo);


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

            public void AddVertex(ref MyVoxelVertex vertex, uint matInfo)
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
                    MaterialInfo = new VRageMath.PackedVector.Byte4(matInfo),
                });
            }
        }

    }
}
