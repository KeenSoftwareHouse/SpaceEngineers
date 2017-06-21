using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Generics;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

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
            public Func<int> Priority;
            public Action<Color> DebugDraw;
        }

        private static readonly MyDynamicObjectPool<MyPrecalcJobRender> m_instancePool = new MyDynamicObjectPool<MyPrecalcJobRender>(16);

        public static bool UseIsoCache = false;
        public static LRUCache<UInt64, MyIsoMesh> IsoMeshCache = new LRUCache<UInt64, MyIsoMesh>(4096);


        [ThreadStatic]
        private static MyRenderCellBuilder m_renderCellBuilder;
        private static MyRenderCellBuilder RenderCellBuilder { get { return MyUtils.Init(ref m_renderCellBuilder); } }

        private List<MyClipmapCellBatch> m_batches = new List<MyClipmapCellBatch>();

        private Args m_args;
        private volatile bool m_isCancelled;

        private MyClipmapCellMeshMetadata m_metadata;

        public Args Arguments
        {
            get { return m_args; }
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

            if (!args.RenderWorkTracker.Exists(args.WorkId))
            {
                args.RenderWorkTracker.Add(args.WorkId, job);
            }

            MyPrecalcComponent.EnqueueBack(job);
        }

        public override void DoWork()
        {
            ProfilerShort.Begin("MyPrecalcJobRender.DoWork");
            try
            {
                if (m_isCancelled)
                    return;

                m_metadata.Cell = m_args.Cell;
                m_metadata.LocalAabb = BoundingBox.CreateInvalid();

                var cellSize = MyVoxelCoordSystems.RenderCellSizeInLodVoxels(m_args.Cell.Lod);
                var min = m_args.Cell.CoordInLod * cellSize - 1;
                var max = min + cellSize - 1
                    + 1 // overlap to neighbor so geometry is stitched together within same LOD
                    + 1 // extra overlap so there are more vertices for mapping to parent LOD
                    + 1; // for eg. 9 vertices in row we need 9 + 1 samples (voxels)
                //    + 1 // why not
                //  + 1 // martin kroslak approved

                var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_args.ClipmapId, m_args.Cell.PackId64());
                MyIsoMesh highResMesh = IsoMeshCache.Read(clipmapCellId);

                if (highResMesh == null)
                {
                    highResMesh = MyPrecalcComponent.IsoMesher.Precalc(m_args.Storage, m_args.Cell.Lod, min, max, true, MyFakes.ENABLE_VOXEL_COMPUTED_OCCLUSION);
                    if (UseIsoCache && highResMesh != null)
                        IsoMeshCache.Write(clipmapCellId, highResMesh);
                }

                if (m_isCancelled || highResMesh == null)
                    return;

                MyIsoMesh lowResMesh = null;

                if (m_args.Cell.Lod < 15 && MyFakes.ENABLE_VOXEL_LOD_MORPHING)
                {
                    var nextLodCell = m_args.Cell;
                    nextLodCell.Lod++;
                    clipmapCellId = MyCellCoord.GetClipmapCellHash(m_args.ClipmapId, nextLodCell.PackId64());

                    lowResMesh = IsoMeshCache.Read(clipmapCellId);

                    if (lowResMesh == null)
                    {
                        // Less detailed mesh for vertex morph targets
                        min >>= 1;
                        max >>= 1;
                        min -= 1;
                        max += 2;

                        lowResMesh = MyPrecalcComponent.IsoMesher.Precalc(m_args.Storage, m_args.Cell.Lod + 1, min, max, true, MyFakes.ENABLE_VOXEL_COMPUTED_OCCLUSION);

                        if (UseIsoCache && lowResMesh != null)
                            IsoMeshCache.Write(clipmapCellId, lowResMesh);
                    }
                }

                if (m_isCancelled)
                    return;

                RenderCellBuilder.BuildCell(m_args, highResMesh, lowResMesh, m_batches, out m_metadata);
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
                if (IsValid)
                    MyRenderProxy.UpdateClipmapCell(m_args.ClipmapId, ref m_metadata, ref m_batches);
                else
                {
                    // recompute the whole things when results are not valid
                    restartWork = true;
                }
            }
            else
                Debug.Assert(m_isCancelled, "Clipmap request collector wont know this job finished!");

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

        public override bool IsCanceled
        {
            get
            {
                return m_isCancelled;
            }
        }
        public override int Priority
        {
            get
            {
                Debug.Assert(m_args.Priority != null, "Missing priority function!");

                return m_isCancelled ? 0 : m_args.Priority();
            }
        }

        public override void DebugDraw(Color c)
        {
            if (m_args.DebugDraw != null)
                m_args.DebugDraw(c);
        }
    }
}
