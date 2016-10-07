using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Library;

namespace VRage.Voxels
{
    public partial class MyClipmap
    {
        public delegate VRageMath.ContainmentType PruningFunc(ref BoundingBox bb, bool lazy);
        public struct VoxelKey
        {
            public VoxelKey(uint clipmapId, UInt64 cell)
            {
                ClipmapId = clipmapId;
                Cell = cell;
            }

            public uint ClipmapId;
            public UInt64 Cell;
        }


        public static Func<BoundingFrustumD> CameraFrustumGetter;

        private uint m_id;

        private readonly LodLevel[] m_lodLevels = new LodLevel[MyCellCoord.MAX_LOD_COUNT];
        private readonly RequestCollector m_requestCollector;
        private readonly UpdateQueueItem m_updateQueueItem;
        private readonly IMyClipmapCellHandler m_cellHandler;

        private Vector3D m_lastClippingPosition = Vector3D.PositiveInfinity;
        private Vector3 m_lastClippingForward = Vector3.PositiveInfinity;


        private bool m_updateClippingFrustum = true;
        int m_invalidated;

        private Vector3I m_sizeLod0;
        private BoundingBoxD m_localAABB;
        private BoundingBoxD m_worldAABB;
        private MatrixD m_worldMatrix;
        private MatrixD m_invWorldMatrix;
        private readonly MyClipmapScaleEnum m_scaleGroup;
        Vector3D m_massiveCenter;
        float m_massiveRadius;

        internal Vector3D LastCameraPosition;

        public static bool UseCache = true;
        public static bool NeedsResetCache = false;
        public static LRUCache<UInt64, MyClipmap_CellData> CellsCache;
        protected static Dictionary<UInt64, MyClipmap_CellData> PendingCacheCellData = new Dictionary<UInt64, MyClipmap_CellData>();
        protected static int ClippingCacheHits = 0;
        protected static int ClippingCacheMisses = 0;

        public static bool UseDithering = true;
        public static bool UseLodCut = true;
        public static bool UseQueries = true;
        private PruningFunc m_prunningFunc = null;

        public MatrixD WorldMatrix { get { return m_worldMatrix; } }

        public MyClipmap(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, IMyClipmapCellHandler cellProvider, Vector3D massiveCenter, float massiveRadius, PruningFunc prunningFunc)
        {
            m_id = id;
            m_scaleGroup = scaleGroup;
            m_worldMatrix = worldMatrix;
            m_massiveCenter = massiveCenter;
            m_massiveRadius = massiveRadius;
            MatrixD.Invert(ref m_worldMatrix, out m_invWorldMatrix);
            m_sizeLod0 = sizeLod0;
            m_localAABB = new BoundingBoxD(Vector3D.Zero, new Vector3D(sizeLod0 * MyVoxelCoordSystems.RenderCellSizeInMeters(0)));
            for (int lod = 0; lod < m_lodLevels.Length; ++lod)
            {
                var sizeShift = lod + MyVoxelCoordSystems.RenderCellSizeInLodVoxelsShiftDelta(lod);
                m_lodLevels[lod] = new LodLevel(this, lod, ((m_sizeLod0 - 1) >> sizeShift) + 1);
            }
            m_updateQueueItem = new UpdateQueueItem(this);
            m_requestCollector = new RequestCollector(id);
            m_cellHandler = cellProvider;
            m_prunningFunc = prunningFunc;
        }

        public uint Id { get { return m_id; } }

        public void UpdateWorldAABB(out BoundingBoxD worldAabb)
        {
            worldAabb = m_localAABB.Transform(ref m_worldMatrix);
            m_worldAABB = worldAabb;
        }

        public void UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortCellsIntoCullObjects)
        {
            m_worldMatrix = worldMatrix;
            MatrixD.Invert(ref m_worldMatrix, out m_invWorldMatrix);
            foreach (var lodLevel in m_lodLevels)
                lodLevel.UpdateWorldMatrices(sortCellsIntoCullObjects);

            ResetClipping();
        }

        public void LoadContent()
        {
            ResetClipping();
        }

        public void UnloadContent()
        {
            foreach (var lodLevel in m_lodLevels)
                lodLevel.UnloadContent();

            ResetClipping();
        }

        /// <param name="minCellLod0">Inclusive.</param>
        /// <param name="maxCellLod0">Inclusive.</param>
        public void InvalidateRange(Vector3I minCellLod0, Vector3I maxCellLod0)
        {
            //Debug.Print("InvalidateRange Clipmap: " + Id + " Min: " + minCellLod0 + " Max: " + maxCellLod0);

            if (minCellLod0 == Vector3I.Zero &&
                maxCellLod0 == m_sizeLod0 - 1)
            {
                for (int lod = 0; lod < m_lodLevels.Length; ++lod)
                {
                    m_lodLevels[lod].InvalidateAll();
                }
            }
            else
            {
                for (int lod = 0; lod < m_lodLevels.Length; ++lod)
                {
                    var shift = lod + MyVoxelCoordSystems.RenderCellSizeInLodVoxelsShiftDelta(lod);
                    m_lodLevels[lod].InvalidateRange(
                        minCellLod0 >> shift,
                        maxCellLod0 >> shift);
                }
            }

            m_invalidated = 2;

            ResetClipping();
        }

        public void UpdateCell(MyRenderMessageUpdateClipmapCell msg)
        {
            m_lodLevels[msg.Metadata.Cell.Lod].SetCellMesh(msg);
            m_requestCollector.RequestFulfilled(msg.Metadata.Cell.PackId64());
            //m_updateClipping = true;
        }

        private void Update(ref Vector3D cameraPos, ref Vector3 cameraForward, float farPlaneDistance)
        {
            ProfilerShort.Begin("MyRenderClipmap.Update");

            LastCameraPosition = cameraPos;

            if (!MyEnvironment.Is64BitProcess)
                UseCache = false;

            if (NeedsResetCache)
            {
                MyClipmap.CellsCache.Reset();
                NeedsResetCache = false;
            }

            for (uint lod = 0; lod < m_lodLevels.Length; lod++)
            {
                if (m_lodLevels[lod].IsDitheringInProgress())
                    m_lodLevels[lod].UpdateDithering();
            }

            Vector3D localPosition;
            Vector3D.Transform(ref cameraPos, ref m_invWorldMatrix, out localPosition);

            Vector3 localForward;
            Vector3.TransformNormal(ref cameraForward, ref m_invWorldMatrix, out localForward);

            double cellSizeHalf = MyVoxelCoordSystems.RenderCellSizeInMetersHalf(0);
            double threshold = cellSizeHalf / 4.0f;
            float thresholdRotation = 0.03f;
            if (!m_updateClippingFrustum && (Vector3D.DistanceSquared(localPosition, m_lastClippingPosition) > threshold) || (Vector3.DistanceSquared(localForward, m_lastClippingForward) > thresholdRotation) || m_invalidated > 0)
            {
                ResetClipping();

                m_lastClippingPosition = localPosition;
                m_lastClippingForward = localForward;
            }

            float camDistanceFromCenter = Vector3.Distance(m_massiveCenter, cameraPos);

            if (m_requestCollector.SentRequestsEmpty && m_updateClippingFrustum)
            {
                ProfilerShort.Begin("DoClipping");
                //Top priority for 0 lod when invalidated (drill)
                if (m_invalidated == 2)
                {
                    m_lodLevels[0].DoClipping(camDistanceFromCenter, localPosition, farPlaneDistance, m_requestCollector, true, 1);
                    m_lodLevels[0].DiscardClippedCells(m_requestCollector);
                    m_lodLevels[0].UpdateCellsInScene(camDistanceFromCenter, localPosition);

                    if (!m_requestCollector.SentRequestsEmpty)
                    {
                        m_requestCollector.Submit();
                        ProfilerShort.End();    // DoClipping
                        ProfilerShort.End();    // Update
                        return;
                    }

                    m_updateClippingFrustum = false;
                    m_invalidated = 1;
                }
                else
                {
                      //Most important frustum culling
                    for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
                    {
                        ProfilerShort.Begin("Lod " + lod);
                        m_lodLevels[lod].DoClipping(camDistanceFromCenter, localPosition, farPlaneDistance, m_requestCollector, true, 1);
                        ProfilerShort.End();
                    }
                    //ProfilerShort.End();

                    ProfilerShort.Begin("KeepOrDiscardClippedCells");
                    for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
                    {
                        m_lodLevels[lod].DiscardClippedCells(m_requestCollector);
                    }
                    ProfilerShort.End();

                    ProfilerShort.Begin("UpdateCellsInScene");
                    for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
                    {
                        m_lodLevels[lod].UpdateCellsInScene(camDistanceFromCenter, localPosition);
                    }
                    ProfilerShort.End();

                    if (!m_requestCollector.SentRequestsEmpty)
                    {
                        m_requestCollector.Submit();
                        ProfilerShort.End();    // DoClipping
                        ProfilerShort.End();    // Update
                        return;
                    }

                    m_invalidated = 0;
                    m_notReady.Remove(this);
                    m_updateClippingFrustum = false;
                }
                ProfilerShort.End();
            }

            ProfilerShort.End();
        }

        public static readonly Vector4[] LOD_COLORS = 
    {
	new Vector4( 1, 0, 0, 1 ),
	new Vector4(  0, 1, 0, 1 ),
	new Vector4(  0, 0, 1, 1 ),

	new Vector4(  1, 1, 0, 1 ),
	new Vector4(  0, 1, 1, 1 ),
	new Vector4(  1, 0, 1, 1 ),

	new Vector4(  0.5f, 0, 1, 1 ),
	new Vector4(  0.5f, 1, 0, 1 ),
	new Vector4(  1, 0, 0.5f, 1 ),
	new Vector4(  0, 1, 0.5f, 1 ),

	new Vector4(  1, 0.5f, 0, 1 ),
	new Vector4(  0, 0.5f, 1, 1 ),

	new Vector4(  0.5f, 1, 1, 1 ),
	new Vector4(  1, 0.5f, 1, 1 ),
	new Vector4(  1, 1, 0.5f, 1 ),
	new Vector4(  0.5f, 0.5f, 1, 1 ),	
};

        public void DebugDraw()
        {
            for (uint lod = 0; lod < m_lodLevels.Length; lod++)
            {
                m_lodLevels[lod].DebugDraw();
            }
        }

        public void DebugDrawMergedMeshCells()
        {
            m_cellHandler.DebugDrawMergedCells();
        }

        public static void UnloadCache()
        {
            CellsCache.Reset();
            PendingCacheCellData.Clear();
            ClippingCacheHits = 0;
            ClippingCacheMisses = 0;
        }

        void ResetClipping()
        {
            m_updateClippingFrustum = true;
        }

        public void RequestMergeAll()
        {
            for (int lodIndex = 0; lodIndex < m_lodLevels.Length; ++lodIndex)
                m_lodLevels[lodIndex].RequestMergeAll();
        }

        public bool IsDitheringInProgress()
        {
            for (uint lod = 0; lod < m_lodLevels.Length; lod++)
            {
                if (m_lodLevels[lod].IsDitheringInProgress())
                    return true;
            }

            return false;
        }

        public Vector3I LodSizeMinusOne(int lod)
        {
            return m_lodLevels[lod].LodSizeMinusOne;
        }
    }
}
