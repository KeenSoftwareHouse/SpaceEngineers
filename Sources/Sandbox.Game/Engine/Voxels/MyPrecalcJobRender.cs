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
    internal sealed class MyPrecalcJobRender : MyPrecalcJob
    {
        public struct Args
        {
            public IMyStorage Storage;
            public uint ClipmapId;
            public MyCellCoord Cell;
            public UInt64 WorkId;
            public MyWorkTracker<ulong, MyPrecalcJobRender> RenderWorkTracker;
            public bool IsHighPriority;
        }

        private static readonly MyDynamicObjectPool<MyPrecalcJobRender> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobRender>(16);

        [ThreadStatic]
        private static MyRenderCellBuilder m_renderCellBuilder;
        private static MyRenderCellBuilder RenderCellBuilder
        {
            get
            {
                if (m_renderCellBuilder == null)
                    m_renderCellBuilder = new MyRenderCellBuilder();
                return m_renderCellBuilder;
            }
        }

        private readonly List<MyClipmapCellBatch> m_batches = new List<MyClipmapCellBatch>();

        private Args m_args;
        private volatile bool m_isCancelled;

        private Vector3D m_positionOffset;
        private Vector3 m_positionScale;
        private BoundingBox m_localBoundingBox;

        public Args Arguments
        {
            get { return m_args; }
        }

        public bool IsHighPriority
        {
            get { return m_args.IsHighPriority; }
        }

        public MyPrecalcJobRender() :
            base(true)
        { }

        public static void Start(Args args)
        {
            Debug.Assert(args.Storage != null);
            var job = m_instancePool.Allocate();

            job.m_isCancelled = false;
            job.m_args = args;
            args.RenderWorkTracker.Add(args.WorkId, job);

            MyPrecalcComponent.EnqueueBack(job, false /*job.m_args.IsHighPriority*/);
        }

        public override void DoWork()
        {
            ProfilerShort.Begin("MyPrecalcJobRender.DoWork");
            try
            {
                if (m_isCancelled)
                    return;

                var min = m_args.Cell.CoordInLod * MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
                var max = min + MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS - 1
                    + 1 // overlap to neighbor so geometry is stitched together within same LOD
                    + 1 // extra overlap so there are more vertices for mapping to parent LOD
                    + 1; // for eg. 9 vertices in row we need 9 + 1 samples (voxels)
                var highResMesh = MyPrecalcComponent.IsoMesher.Precalc(m_args.Storage, m_args.Cell.Lod, min, max, true);

                if (m_isCancelled || highResMesh == null)
                    return;

                // Less detailed mesh for vertex morph targets
                min >>= 1;
                max >>= 1;
                min -= 1;
                max += 1;
                var lowResMesh = MyPrecalcComponent.IsoMesher.Precalc(m_args.Storage, m_args.Cell.Lod + 1, min, max, true);

                if (m_isCancelled)
                    return;

                RenderCellBuilder.BuildCell(m_args, highResMesh, lowResMesh, m_batches, out m_positionOffset, out m_positionScale, out m_localBoundingBox);
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        protected override void OnComplete()
        {
            base.OnComplete();

            bool restartWork = false;
            if (MyPrecalcComponent.Loaded && !m_isCancelled && m_args.Storage != null)
            {
                // Update render even if results are not valid. Not updating may result in geometry staying the same for too long.
                MyRenderProxy.UpdateClipmapCell(
                    m_args.ClipmapId,
                    m_args.Cell,
                    m_batches,
                    m_positionOffset,
                    m_positionScale,
                    m_localBoundingBox);
                if (!IsValid)
                {
                    // recompute the whole things when results are not valid
                    restartWork = true;
                }
            }

            if (!m_isCancelled)
            {
                m_args.RenderWorkTracker.Complete(m_args.WorkId);
            }
            m_batches.Clear();
            if (restartWork)
            {
                Start(m_args);
            }
            else
            {
                m_args = default(Args);
                m_instancePool.Deallocate(this);
            }
        }

        public override void Cancel()
        {
            m_isCancelled = true;
        }
    }

    class MyRenderCellBuilder
    {
        // mk:TODO Could index buffer use ushort? Reducing render cell then
        // might even fit inside single batch in worst case.

        private const int MAX_VERTICES_COUNT = short.MaxValue;           //  Max number of vertexes we can hold in vertex buffer (because we support only 16-bit m_notCompressedIndex buffer)
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
        private readonly List<Vertex> m_collisionList = new List<Vertex>();

        struct MorphData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public int Material;
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
            out Vector3D positionOffset,
            out Vector3 positionScale,
            out BoundingBox localBoundingBox)
        {
            Debug.Assert(highResMesh != null);

            {
                positionOffset = highResMesh.PositionOffset;
                positionScale = highResMesh.PositionScale;
            }

            {
                // Find low resolution LoD vertices to map to
                float vertexCellSizeInParentLod = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << (args.Cell.Lod + 1));
                float collisionThreshold = 0.0001f * vertexCellSizeInParentLod * vertexCellSizeInParentLod;
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
                            Target = new MorphData {
                                Position = lowResMesh.Positions[i] * morphScale + morphOffset,
                                Normal = lowResMesh.Normals[i],
                                Material = lowResMesh.Materials[i],
                            },
                            Cell = lowResMesh.Cells[i],
                        };

                        if (m_morphMap.ContainsKey(vertex.Cell))
                        {
                            var collidedVertex = m_morphMap[vertex.Cell];
                            Debug.Assert(collidedVertex.Cell == vertex.Cell);

                            var collisionDist2 = (collidedVertex.Target.Position - vertex.Target.Position).LengthSquared();
                            if (collisionDist2 > collisionThreshold)
                            {
                                //Debug.Fail(string.Format("Collision between vertices! {0} > {1}", collisionDist2, collisionThreshold));
                                m_collisionList.Add(collidedVertex);
                            }
                        }
                        m_morphMap[vertex.Cell] = vertex;
                    }
                }

                if (false)
                {
                    ProcessMorphTargetCollisions(args, vertexCellSizeInParentLod);
                }

                if (false)
                {
                    EnsureMorphTargetExists(args, highResMesh);
                }
            }

            localBoundingBox = BoundingBox.CreateInvalid();

            //  Increase lookup count, so we will think that all vertices in helper arrays are new
            foreach (var lookup in SM_BatchLookups.Values)
            {
                lookup.ResetBatch();
            }

            for (int i = 0; i < highResMesh.Triangles.Count; i++)
            {
                MyVoxelTriangle srcTriangle = highResMesh.Triangles[i];
                MyVoxelVertex vertex0, vertex1, vertex2;
                ProcessVertex(highResMesh, srcTriangle.VertexIndex0, ref localBoundingBox, ref positionScale, ref positionOffset, out vertex0);
                ProcessVertex(highResMesh, srcTriangle.VertexIndex1, ref localBoundingBox, ref positionScale, ref positionOffset, out vertex1);
                ProcessVertex(highResMesh, srcTriangle.VertexIndex2, ref localBoundingBox, ref positionScale, ref positionOffset, out vertex2);

                if (MyPerGameSettings.ConstantVoxelAmbient.HasValue)
                {
                    vertex0.Ambient = vertex1.Ambient = vertex2.Ambient = MyPerGameSettings.ConstantVoxelAmbient.Value;
                }

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

        private void EnsureMorphTargetExists(MyPrecalcJobRender.Args args, MyIsoMesh highResMesh)
        {
            // Ensure as many (ideally all) vertices have some mapping prepared.
            // This is here to resolve any missing parent only once for each vertex (main loop can visit vertices for each triangle they appear in).
            //var geometryData = highResMesh;
            for (int i = 0; i < highResMesh.VerticesCount; ++i)
            {
                Vector3 highResPosition = highResMesh.Positions[i];
                Vertex lowResVertex;

                Vector3I vertexCell = highResMesh.Cells[i] >> 1;
                if (!m_morphMap.TryGetValue(vertexCell, out lowResVertex))
                {
                    float scale = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << args.Cell.Lod);
                    Vector3 cell = highResPosition / scale;
                    cell *= 0.5f;
                    scale = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << (args.Cell.Lod + 1));
                    Vector3 lowResPosition = cell * scale;
                    var cellMin = vertexCell;
                    var cellMax = vertexCell + 1; // usually I need to find parent in this direction
                    float nearestDist = float.PositiveInfinity;
                    int nearestDistManhat = int.MaxValue;
                    Vector3I? nearestCell = null;
                    for (var it = new Vector3I.RangeIterator(ref cellMin, ref cellMax); it.IsValid(); it.MoveNext())
                    {
                        Vertex morph;
                        if (m_morphMap.TryGetValue(it.Current, out morph))
                        {
                            var tmp = it.Current - cellMin;
                            int distManhat = tmp.X + tmp.Y + tmp.Z;
                            float dist = (morph.Target.Position - lowResPosition).LengthSquared();
                            if (distManhat < nearestDistManhat ||
                                (distManhat == nearestDistManhat && dist < nearestDist))
                            {
                                nearestDist = dist;
                                nearestDistManhat = distManhat;
                                nearestCell = it.Current;
                            }
                        }
                    }

                    if (nearestCell.HasValue)
                    {
                        m_morphMap.Add(vertexCell, m_morphMap[nearestCell.Value]);
                    }
                    else
                    {
                        //Debug.Fail("I'm screwed");
                    }
                }
            }
        }

        private void ProcessMorphTargetCollisions(MyPrecalcJobRender.Args args, float vertexCellSizeInParentLod)
        {
            // Process collisions.
            // Remove collided vertices from map and add them to the list of collisions.
            for (int i = 0; i < m_collisionList.Count; ++i)
            {
                var vertexA = m_collisionList[i];
                Vertex vertexB;
                if (m_morphMap.TryGetValue(vertexA.Cell, out vertexB))
                {
                    m_morphMap.Remove(vertexA.Cell);
                    m_collisionList.Add(vertexB);
                }
            }

            // Try find better position for each vertex involved in collision.
            // mk:TODO This would ideally be done wthout MyVoxelCoordSystems
            float vertexHalfExpand = 0.25f * vertexCellSizeInParentLod;
            for (int i = 0; i < m_collisionList.Count; ++i)
            {
                var vertex = m_collisionList[i];
                var center = vertex.Target.Position;
                var min = center - vertexHalfExpand;
                var max = center + vertexHalfExpand;
                Vector3I minVertexCell, maxVertexCell;
                MyVoxelCoordSystems.LocalPositionToVertexCell(args.Cell.Lod + 1, ref min, out minVertexCell);
                MyVoxelCoordSystems.LocalPositionToVertexCell(args.Cell.Lod + 1, ref max, out maxVertexCell);
                if (minVertexCell == maxVertexCell)
                {
                    m_morphMap[minVertexCell] = vertex;
                }
                else
                {
                    for (var it = new Vector3I.RangeIterator(ref minVertexCell, ref maxVertexCell); it.IsValid(); it.MoveNext())
                    {
                        if (!m_morphMap.ContainsKey(it.Current))
                            m_morphMap[it.Current] = vertex;
                    }
                }
            }
            m_collisionList.Clear();
        }

        private void ProcessVertex(MyIsoMesh mesh, int vertexIndex, ref BoundingBox localAabb, ref Vector3 positionScale, ref Vector3D positionOffset, out MyVoxelVertex vertex)
        {
            vertex.Position = mesh.Positions[vertexIndex];
            vertex.Normal = mesh.Normals[vertexIndex];
            vertex.Material = mesh.Materials[vertexIndex];
            vertex.Ambient = 0f;
            Vertex morph;
            if (m_morphMap.TryGetValue(mesh.Cells[vertexIndex] >> 1, out morph))
            {
                vertex.PositionMorph = morph.Target.Position;
                vertex.NormalMorph = morph.Target.Normal;
                vertex.MaterialMorph = morph.Target.Material;
            }
            else
            {
                vertex.PositionMorph = vertex.Position;
                vertex.NormalMorph = vertex.Normal;
                vertex.MaterialMorph = vertex.Material;
            }
            localAabb.Include(vertex.Position * positionScale + positionOffset);
            localAabb.Include(vertex.PositionMorph * positionScale + positionOffset);

            Debug.Assert(vertex.Position.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
            Debug.Assert(vertex.PositionMorph.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
        }

        private void EndSingleMaterial(SingleMaterialHelper materialHelper, List<MyClipmapCellBatch> outBatches)
        {
            //Synchronize to VRage render
            if (materialHelper.IndexCount > 0 && materialHelper.VertexCount > 0)
            {
                //Todo - is it possible without allocations?
                MyVertexFormatVoxelSingleData[] vertices = new MyVertexFormatVoxelSingleData[materialHelper.VertexCount];
                Array.Copy(materialHelper.Vertices, vertices, vertices.Length);
                short[] indices = new short[materialHelper.IndexCount];
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

                short[] indices = new short[helper.Vertices.Count];
                for (short i = 0; i < indices.Length; i++)
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
            VertexInBatchLookup inBatchLookup, short srcVertexIdx)
        {
            if (!inBatchLookup.IsInBatch(srcVertexIdx))
            {
                int tgtVertexIdx = materialHelper.VertexCount;

                //  Short overflow check
                Debug.Assert(tgtVertexIdx <= short.MaxValue);

                materialHelper.Vertices[tgtVertexIdx].Position = vertex.Position;
                materialHelper.Vertices[tgtVertexIdx].PositionMorph = vertex.PositionMorph;
                materialHelper.Vertices[tgtVertexIdx].Ambient = vertex.Ambient;
                materialHelper.Vertices[tgtVertexIdx].Normal = vertex.Normal;
                materialHelper.Vertices[tgtVertexIdx].NormalMorph = vertex.NormalMorph;

                inBatchLookup.PutToBatch(srcVertexIdx, (short)tgtVertexIdx);

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
                public short IndexInBatch;
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

            internal void PutToBatch(short vertexIndex, short indexInBatch)
            {
                m_data[vertexIndex].BatchId = m_idCounter;
                m_data[vertexIndex].IndexInBatch = indexInBatch;
            }

            internal void ResetBatch()
            {
                ++m_idCounter;
            }

            internal short GetIndexInBatch(int vertexIndex)
            {
                return m_data[vertexIndex].IndexInBatch;
            }
        }

        class SingleMaterialHelper
        {
            public readonly MyVertexFormatVoxelSingleData[] Vertices = new MyVertexFormatVoxelSingleData[MyRenderCellBuilder.MAX_VERTICES_COUNT];
            public readonly short[] Indices = new short[MyRenderCellBuilder.MAX_INDICES_COUNT];

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
                    Normal = vertex.Normal,
                    NormalMorph = vertex.NormalMorph,
                    MaterialAlphaIndex = alphaIndex,
                    MaterialMorph = materialMorph,
                });
            }
        }

    }

}
