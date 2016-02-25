using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Collections;
using VRage.Generics;
using VRage.Library.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    internal sealed class MyPrecalcJobMerge : MyPrecalcJob
    {
        public struct Args
        {
            public IMyStorage Storage;
            public uint ClipmapId;
            public MyCellCoord Cell;
            public List<MyClipmapCellMeshMetadata> LodMeshMetadata;
            public List<MyClipmapCellBatch> InBatches;

            public UInt64 WorkId;
            public MyWorkTracker<ulong, MyPrecalcJobMerge> RenderWorkTracker;
            public Func<int> Priority;
            public Action<Color> DebugDraw;
        }

        private static readonly MyDynamicObjectPool<MyPrecalcJobMerge> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobMerge>(16);

        private readonly Dictionary<int, MyMaterialHelper> m_materialHelperMappings = new Dictionary<int, MyMaterialHelper>();

        private bool m_isCancelled;
        private Args m_args;
        private readonly List<MyClipmapCellBatch> m_outBatches = new List<MyClipmapCellBatch>();

        private MyClipmapCellMeshMetadata m_metadata;

        public override bool IsCanceled { get { return m_isCancelled; } }
        public override int Priority { get { Debug.Assert(m_args.Priority != null, "Missing priority function!"); return m_isCancelled ? 0 : m_args.Priority(); } }

        public MyPrecalcJobMerge()
            : base(true)
        { }

        public static void Start(Args args)
        {
            var job = m_instancePool.Allocate();
            Debug.Assert(job.m_outBatches.Count == 0, "Merge job batches not cleared");

            job.m_isCancelled = false;
            job.m_args = args;

            if (!args.RenderWorkTracker.Exists(args.WorkId))
            {
                args.RenderWorkTracker.Add(args.WorkId, job);
            }

            MyPrecalcComponent.EnqueueBack(job);
        }

        public override void DoWork()
        {
            ProfilerShort.Begin("MyPrecalcJobMerge.DoWork");
            try
            {
                if (m_isCancelled)
                    return;

                double largestDistance = double.MinValue;

                m_metadata.Cell = m_args.Cell;
                m_metadata.LocalAabb = BoundingBox.CreateInvalid();

                m_metadata.PositionOffset = Vector3D.MaxValue;
                foreach (var metadata in m_args.LodMeshMetadata)
                {
                    m_metadata.LocalAabb.Include(metadata.LocalAabb);

                    // Find the smallest offset
                    m_metadata.PositionOffset = Vector3D.Min(m_metadata.PositionOffset, metadata.PositionOffset);
                }

                if (m_isCancelled)
                    return;

                var inputBatches = m_args.InBatches;
                int totalVertexCount = 0;

                foreach(MyClipmapCellBatch inputBatch in inputBatches)
                {
                    totalVertexCount += inputBatch.Vertices.Length;
                }
                int currentMesh = 0;
                int currentBatch = 0;
                uint currentVertex = 0;

                if (m_isCancelled)
                    return;

                for (int meshIndex = currentMesh; meshIndex < m_args.LodMeshMetadata.Count; ++meshIndex, ++currentMesh)
                {
                    var meshScale = m_args.LodMeshMetadata[meshIndex].PositionScale;
                    var meshOffset = m_args.LodMeshMetadata[meshIndex].PositionOffset;
                    var relativeOffset = meshOffset - m_metadata.PositionOffset;
                    for (int batchIndex = 0; batchIndex < m_args.LodMeshMetadata[meshIndex].BatchCount; ++batchIndex, ++currentBatch)
                    {
                        int material0 = inputBatches[currentBatch].Material0, material1 = inputBatches[currentBatch].Material1, material2 = inputBatches[currentBatch].Material2;
                        int batchMaterial = MyMaterialHelper.GetMaterialIndex(material0, material1, material2);

                        MyMaterialHelper materialHelper = null;
                        if (!m_materialHelperMappings.TryGetValue(batchMaterial, out materialHelper))
                        {
                            materialHelper = MyMaterialHelper.Allocate();
                            materialHelper.Init(material0, material1, material2);
                            m_materialHelperMappings.Add(batchMaterial, materialHelper);
                        }

                        var inputVertices = inputBatches[currentBatch].Vertices;
                        uint vertexOffset = (uint)materialHelper.GetCurrentVertexIndex();
                        for (int vertexIndex = 0; vertexIndex < inputVertices.Length; ++vertexIndex, ++currentVertex)
                        {
                            Vector3D position = inputVertices[vertexIndex].Position;
                            Vector3D positionMorph = inputVertices[vertexIndex].PositionMorph;

                            position *= meshScale;
                            position += relativeOffset;
                            positionMorph *= meshScale;
                            positionMorph += relativeOffset;

                            double newDistance = Math.Max(position.AbsMax(), positionMorph.AbsMax());
                            largestDistance = Math.Max(largestDistance, newDistance);

                            materialHelper.AddVertex(ref inputVertices[vertexIndex], ref position, ref positionMorph);
                        }

                        var inputBatchIndices = inputBatches[currentBatch].Indices;
                        for (int indexIndex = 0; indexIndex < inputBatchIndices.Length; ++indexIndex)
                        {
                            materialHelper.AddIndex(inputBatchIndices[indexIndex] + vertexOffset);
                        }
                    }

                    if (m_isCancelled)
                    {
                        m_outBatches.SetSize(0);
                        return;
                    }
                }

                foreach (var materialHelper in m_materialHelperMappings.Values)
                {
                    m_outBatches.Add(MyMaterialHelper.CreateBatch(materialHelper, largestDistance));
                    materialHelper.Release();
                }
                m_materialHelperMappings.Clear();
               
                m_metadata.PositionScale = Vector3.One*(float)largestDistance;
                m_metadata.BatchCount = m_outBatches.Count;
            }
            finally
            {
                foreach (var materialHelper in m_materialHelperMappings.Values)
                    materialHelper.Release();

                m_materialHelperMappings.Clear();

                ProfilerShort.End();
            }
        }

        protected override void OnComplete()
        {
            base.OnComplete();

            bool restartWork = false;
            if (MyPrecalcComponent.Loaded && !m_isCancelled)
            {
                // Update render even if results are not valid. Not updating may result in geometry staying the same for too long.
                MyRenderProxy.UpdateMergedVoxelMesh(
                    m_args.ClipmapId,
                    m_args.Cell.Lod,
                    m_args.WorkId,
                    m_metadata,
                    m_outBatches);
                if (!IsValid)
                {
                    // recompute the whole things when results are not valid
                    restartWork = true;
                }
            }
            else
                Debug.Assert(m_isCancelled, "Clipmap request collector wont know this job finished!");

            m_outBatches.Clear();
            if (!m_isCancelled)
            {
                m_args.RenderWorkTracker.Complete(m_args.WorkId);
            }

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

        // Each MaterialHelper contains information for once batch
        private class MyMaterialHelper
        {
            private readonly List<MyVertexFormatVoxelSingleData> m_vertexHelper = new List<MyVertexFormatVoxelSingleData>();
            private readonly List<uint> m_indexHelper = new List<uint>();
            private readonly List<MyTuple<Vector3D, Vector3D>> m_positionHelper = new List<MyTuple<Vector3D,Vector3D>>();
            internal int Material0 = -1;
            internal int Material1 = -1;
            internal int Material2 = -1;

            public int MaterialIndex { get { return GetMaterialIndex(Material0, Material1, Material2); } }

            #region Object pool
            private static MyConcurrentQueue<MyMaterialHelper> m_helperPool = new MyConcurrentQueue<MyMaterialHelper>(16);

            internal static MyMaterialHelper Allocate()
            {
                MyMaterialHelper materialHelper;
                if(!m_helperPool.TryDequeue(out materialHelper))
                    materialHelper = new MyMaterialHelper();

                return materialHelper;
            }

            // Releases the helper back into the pool. Does not clear internal state
            internal static void Release(MyMaterialHelper helper)
            {
                m_helperPool.Enqueue(helper);
            }
            #endregion

            internal void Init(int material0, int material1, int material2)
            {
                Debug.Assert(m_vertexHelper.Count == 0 && m_indexHelper.Count == 0 && m_positionHelper.Count == 0 && Material0 == -1 && Material1 == -1 && Material2 == -1, "Material helper not cleared!");
                Material0 = material0;
                Material1 = material1;
                Material2 = material2;
            }

            internal void Release()
            {
                Clear();
                MyMaterialHelper.Release(this);
            }

            internal void AddVertex(ref MyVertexFormatVoxelSingleData vertex, ref Vector3D positionUnnormalized, ref Vector3D positionMorphUnnormalized)
            {
                m_vertexHelper.Add(vertex);
                m_positionHelper.Add(MyTuple.Create(positionUnnormalized, positionMorphUnnormalized));
            }

            internal void AddIndex(uint index)
            {
                m_indexHelper.Add(index);
            }

            internal int GetCurrentVertexIndex()
            {
                return m_vertexHelper.Count;
            }

            internal void Clear()
            {
                m_vertexHelper.SetSize(0);
                m_indexHelper.SetSize(0);
                m_positionHelper.SetSize(0);
                Material0 = -1;
                Material1 = -1;
                Material2 = -1;
            }

            internal static int GetMaterialIndex(int material0, int material1, int material2)
            {
                return material0 + (material1 + material2 << 10) << 10;
            }

            internal static MyClipmapCellBatch CreateBatch(MyMaterialHelper materialHelper, double largestDistance)
            {
                Debug.Assert(materialHelper.Material0 != -1 || materialHelper.Material1 != -1 || materialHelper.Material2 != -1, "Material helper not initialized!");
                for (int vertexIndex = 0; vertexIndex < materialHelper.m_positionHelper.Count; ++vertexIndex )
                {
                    var vertex = materialHelper.m_vertexHelper[vertexIndex];
                    vertex.Position = materialHelper.m_positionHelper[vertexIndex].Item1 / largestDistance;
                    vertex.PositionMorph = materialHelper.m_positionHelper[vertexIndex].Item2 / largestDistance;
                    materialHelper.m_vertexHelper[vertexIndex] = vertex;
                }

                return new MyClipmapCellBatch
                {
                    Vertices = materialHelper.m_vertexHelper.ToArray(),
                    Indices = materialHelper.m_indexHelper.ToArray(),
                    Material0 = materialHelper.Material0,
                    Material1 = materialHelper.Material1,
                    Material2 = materialHelper.Material2,
                };
            }
        }
    }
}
