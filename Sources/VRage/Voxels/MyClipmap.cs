using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public partial class MyClipmap
    {
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
        public static bool UseCache = true;
        public static bool NEW_VOXEL_CLIPPING = false;

        private uint m_id;

        private readonly LodLevel[] m_lodLevels = new LodLevel[MyCellCoord.MAX_LOD_COUNT];
        private readonly RequestCollector m_requestCollector;
        private readonly UpdateQueueItem m_updateQueueItem;
        private readonly IMyClipmapCellHandler m_cellHandler;

        private Vector3D m_lastClippingPosition = Vector3D.PositiveInfinity;
        private bool m_updateClipping = true;

        private Vector3I m_sizeLod0;
        private BoundingBoxD m_localAABB;
        private BoundingBoxD m_worldAABB;
        private MatrixD m_worldMatrix;
        private MatrixD m_invWorldMatrix;
        private readonly MyClipmapScaleEnum m_scaleGroup;

        protected static LRUCache<UInt64, CellData> CellsCache = new LRUCache<UInt64, CellData>(32768);
        protected static int ClippingCacheHits = 0;
        protected static int ClippingCacheMisses = 0;



        /// <summary>
        /// adjusts loaded terrain quality
        /// negative value represents lowes loaded lod
        /// positive value extends number of loaded surrounding lod0 cells
        /// </summary>
        private int m_clipingAdjustment = 0;
        private static bool ENABLE_CLIPPING_ADJUSTMENT = false;

        public MyClipmap(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, IMyClipmapCellHandler cellProvider)
        {
            m_id = id;
            m_scaleGroup = scaleGroup;
            m_worldMatrix = worldMatrix;
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
            m_updateClipping = true;
        }

        public void LoadContent()
        {
            m_updateClipping = true;
        }

        public void UnloadContent()
        {
            foreach (var lodLevel in m_lodLevels)
                lodLevel.UnloadContent();

            m_updateClipping = true;
        }

        /// <param name="minCellLod0">Inclusive.</param>
        /// <param name="maxCellLod0">Inclusive.</param>
        public void InvalidateRange(Vector3I minCellLod0, Vector3I maxCellLod0)
        {
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
            m_updateClipping = true;
        }

        public void UpdateCell(MyRenderMessageUpdateClipmapCell msg)
        {
            m_lodLevels[msg.Metadata.Cell.Lod].SetCellMesh(msg);
            m_requestCollector.RequestFulfilled(msg.Metadata.Cell.PackId64());
            //m_updateClipping = true;
        }

        private void Update(ref Vector3D cameraPos, float farPlaneDistance)
        {
            ProfilerShort.Begin("MyRenderClipmap.Update");


            Vector3D localPosition;
            Vector3D.Transform(ref cameraPos, ref m_invWorldMatrix, out localPosition);

            double cellSizeHalf = MyVoxelCoordSystems.RenderCellSizeInMetersHalf(0);
            double threshold = cellSizeHalf * cellSizeHalf;
            if (!m_updateClipping && Vector3D.DistanceSquared(localPosition, m_lastClippingPosition) > threshold)
            {
                m_updateClipping = true;
            }

            //modified clipping routine
            //we clip only when there are no old requests since we are not able to combine
            //multiple clippings reasonably
            //(need the structure to hold and merge result otherwise holes are inevitable)
            if (!m_updateClipping && m_requestCollector.SentRequestsEmpty && m_clipingAdjustment < 5)
            {
                m_clipingAdjustment += 2;
                m_updateClipping = true;
            }

            if (m_updateClipping)
            {
                m_requestCollector.Submit();
                if (m_requestCollector.SentRequestsEmpty)
                {
                    ProfilerShort.Begin("KeepOrDiscardClippedCells");
                    for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
                    {
                        m_lodLevels[lod].KeepOrDiscardClippedCells(m_requestCollector);
                    }
                    ProfilerShort.End();
                    if (!ENABLE_CLIPPING_ADJUSTMENT)
                        m_clipingAdjustment = 1;
                    var startLod = MathHelper.Clamp(-1 * m_clipingAdjustment, 0, m_lodLevels.Length - 1);
                    ProfilerShort.Begin("DoClipping");

                    if (NEW_VOXEL_CLIPPING)
                    {
                        m_lodLevels[startLod].DoClipping(localPosition, m_requestCollector, MathHelper.Clamp(m_clipingAdjustment, 0, 5));
                    }
                    else
                    {
                        Debug.Assert(m_scaleGroup == MyClipmapScaleEnum.Normal);
                        for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
                        {
                            ProfilerShort.Begin("Lod " + lod);
                            m_lodLevels[lod].DoClipping_Old(localPosition, farPlaneDistance, m_requestCollector);
                            ProfilerShort.End();
                        }
                    }


                    ProfilerShort.End();
                    if (m_requestCollector.SentRequestsEmpty)
                    {
                        m_clipingAdjustment += 2;
                    }
                }
                //else
                //    m_clipingAdjustment -= 2;

                m_lastClippingPosition = localPosition;
                m_updateClipping = false;
            }

            ProfilerShort.Begin("UpdateCellsInScene");
            for (int lod = m_lodLevels.Length - 1; lod >= 0; --lod)
            {
                m_lodLevels[lod].UpdateCellsInScene(localPosition);
            }
            ProfilerShort.End();

            m_clipingAdjustment = MathHelper.Clamp(m_clipingAdjustment, -m_lodLevels.Length + 3, 5);
            m_requestCollector.Submit();
            if (m_requestCollector.SentRequestsEmpty)
                m_notReady.Remove(this);

            ProfilerShort.End();
        }

        public void DebugDraw()
        {
            for (uint lod = 0; lod < m_lodLevels.Length; lod++)
            {
                m_lodLevels[lod].DebugDraw();
            }
        }
    }
}
