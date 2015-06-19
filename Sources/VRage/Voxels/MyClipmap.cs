using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public interface IMyClipmapCell
    {
        void UpdateMesh(MyRenderMessageUpdateClipmapCell msg);

        void UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortIntoCullObjects);
    }

    public interface IMyClipmapCellHandler
    {
        IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref MatrixD worldMatrix);

        void DeleteCell(IMyClipmapCell cell);

        void AddToScene(IMyClipmapCell cell);

        void RemoveFromScene(IMyClipmapCell cell);
    }

    public partial class MyClipmap
    {
        public static float DebugClipmapMostDetailedLod = 0f;
        private uint m_lastMostDetailedLod = 0;

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

        public MyClipmap(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, IMyClipmapCellHandler cellProvider)
        {
            m_scaleGroup = scaleGroup;
            m_worldMatrix = worldMatrix;
            MatrixD.Invert(ref m_worldMatrix, out m_invWorldMatrix);
            m_sizeLod0 = sizeLod0;
            m_localAABB = new BoundingBoxD(Vector3D.Zero, new Vector3D(sizeLod0 * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES));
            for (int lod = 0; lod < m_lodLevels.Length; ++lod)
            {
                m_lodLevels[lod] = new LodLevel(this, lod, ((m_sizeLod0 - 1) >> lod) + 1);
            }
            m_updateQueueItem = new UpdateQueueItem(this);
            m_requestCollector = new RequestCollector(id);
            m_cellHandler = cellProvider;
        }

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
                    m_lodLevels[lod].InvalidateRange(
                        minCellLod0 >> lod,
                        maxCellLod0 >> lod);
                }
            }
            m_updateClipping = true;
        }

        public void UpdateCell(MyRenderMessageUpdateClipmapCell msg)
        {
            m_lodLevels[msg.Cell.Lod].SetCellMesh(msg);
            m_requestCollector.RequestFulfilled(msg.Cell.PackId64());
            m_updateClipping = true;
        }

        private void Update(ref Vector3D cameraPos, float farPlaneDistance)
        {
            ProfilerShort.Begin("MyRenderClipmap.Update");

            var mostDetailedLod = (uint)DebugClipmapMostDetailedLod;
            if (m_lastMostDetailedLod != mostDetailedLod)
                m_updateClipping = true;
            m_lastMostDetailedLod = mostDetailedLod;
            for (uint lod = 0; lod < m_lodLevels.Length; ++lod)
            {
                m_lodLevels[lod].Visible = lod >= mostDetailedLod;
            }

            Vector3D localPosition;
            Vector3D.Transform(ref cameraPos, ref m_invWorldMatrix, out localPosition);

            const double THRESHOLD = MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES_HALF * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES_HALF;
            if (!m_updateClipping && Vector3D.DistanceSquared(localPosition, m_lastClippingPosition) > THRESHOLD)
            {
                m_updateClipping = true;
            }

            if (m_updateClipping)
            {
                ProfilerShort.Begin("DoClipping");
                Debug.Assert(m_scaleGroup == MyClipmapScaleEnum.Normal || m_scaleGroup == MyClipmapScaleEnum.Massive);
                for (int lod = m_lodLevels.Length - 1; lod >= mostDetailedLod; --lod)
                {
                    ProfilerShort.Begin("Lod " + lod);
                    m_lodLevels[lod].DoClipping(localPosition, farPlaneDistance, m_requestCollector);
                    ProfilerShort.End();
                }
                ProfilerShort.End();

                ProfilerShort.Begin("KeepOrDiscardClippedCells");
                for (int lod = m_lodLevels.Length - 1; lod >= mostDetailedLod; --lod)
                {
                    m_lodLevels[lod].KeepOrDiscardClippedCells(m_requestCollector);
                }
                ProfilerShort.End();

                m_lastClippingPosition = localPosition;
                m_updateClipping = false;
            }

            ProfilerShort.Begin("UpdateCellsInScene");
            for (int lod = m_lodLevels.Length - 1; lod >= mostDetailedLod; --lod)
            {
                m_lodLevels[lod].UpdateCellsInScene(localPosition);
            }
            ProfilerShort.End();

            m_requestCollector.Submit();
            if (m_requestCollector.SentRequestsEmpty)
                m_notReady.Remove(this);

            ProfilerShort.End();
        }

        enum CellState
        {
            Invalid,
            Pending,
            Loaded
        }

        class CellData
        {
            public CellState State;
            public IMyClipmapCell Cell;
            public bool InScene;
            public bool WasLoaded;
        }

        class LodLevel
        {
            private float m_nearDistance;
            private float m_farDistance;

            private Dictionary<UInt64, CellData> m_storedCellData = new Dictionary<UInt64, CellData>();
            private Dictionary<UInt64, CellData> m_nonEmptyCells = new Dictionary<UInt64, CellData>();

            // temporary dictionaries
            private Dictionary<UInt64, CellData> m_clippedCells = new Dictionary<UInt64, CellData>();

            /// <summary>
            /// Indicator that LoD is too large to even render with current setttings.
            /// </summary>
            private bool m_fitsInFrustum;

            private int m_lodIndex;
            private Vector3I m_lodSizeMinusOne;
            private MyClipmap m_parent;
            private Vector3I m_lastMin = Vector3I.MaxValue;
            private Vector3I m_lastMax = Vector3I.MinValue;
            private bool m_visible;
            public bool Visible
            {
                get { return m_visible; }
                set
                {
                    if (m_visible != value)
                    {
                        m_visible = value;
                        if (m_visible)
                        {
                            foreach (var data in m_nonEmptyCells.Values)
                            {
                                if (data.InScene)
                                    m_parent.m_cellHandler.AddToScene(data.Cell);
                            }
                        }
                        else
                        {
                            foreach (var data in m_nonEmptyCells.Values)
                            {
                                if (data.InScene)
                                    m_parent.m_cellHandler.RemoveFromScene(data.Cell);
                            }
                        }
                    }
                }
            }

            internal LodLevel(MyClipmap parent, int lodIndex, Vector3I lodSize)
            {
                m_parent = parent;
                m_lodIndex = lodIndex;
                m_lodSizeMinusOne = lodSize - 1;
            }

            internal void Hide()
            {
                foreach (var data in m_nonEmptyCells.Values)
                {
                    if (data.InScene)
                        m_parent.m_cellHandler.RemoveFromScene(data.Cell);
                }
            }

            internal void UnloadContent()
            {
                foreach (var data in m_nonEmptyCells.Values)
                {
                    if (data.InScene)
                        m_parent.m_cellHandler.RemoveFromScene(data.Cell);
                    m_parent.m_cellHandler.DeleteCell(data.Cell);
                }
                m_nonEmptyCells.Clear();
                m_storedCellData.Clear();
            }

            internal void InvalidateRange(Vector3I lodMin, Vector3I lodMax)
            {
                var cell = new MyCellCoord(m_lodIndex, lodMin);
                for (var it = new Vector3I.RangeIterator(ref lodMin, ref lodMax);
                    it.IsValid(); it.GetNext(out cell.CoordInLod))
                {
                    CellData data;
                    var id = cell.PackId64();
                    if (m_storedCellData.TryGetValue(id, out data))
                    {
                        data.State = CellState.Invalid;
                    }
                }
            }

            internal void InvalidateAll()
            {
                foreach (var cell in m_storedCellData.Values)
                {
                    cell.State = CellState.Invalid;
                }
            }

            internal void SetCellMesh(MyRenderMessageUpdateClipmapCell msg)
            {
                var cellId = msg.Cell.PackId64();
                CellData data;
                if (m_storedCellData.TryGetValue(cellId, out data))
                {
                    if (data.Cell == null && msg.Batches.Count != 0)
                    {
                        data.Cell = m_parent.m_cellHandler.CreateCell(m_parent.m_scaleGroup, msg.Cell, ref m_parent.m_worldMatrix);
                        m_nonEmptyCells[cellId] = data;
                    }
                    else if (data.Cell != null && msg.Batches.Count == 0)
                    {
                        RemoveFromScene(cellId, data);
                        m_nonEmptyCells.Remove(cellId);
                        m_parent.m_cellHandler.DeleteCell(data.Cell);
                        data.Cell = null;
                    }

                    if (data.Cell != null)
                    {
                        data.Cell.UpdateMesh(msg);
                    }
                    data.State = CellState.Loaded;
                    data.WasLoaded = true;
                }
            }

            internal void DoClipping(Vector3D localPosition, float farPlaneDistance, RequestCollector collector)
            {
                MyClipmap.ComputeLodViewBounds(m_parent.m_scaleGroup, m_lodIndex, out m_nearDistance, out m_farDistance);

                m_fitsInFrustum = (farPlaneDistance * 1.25f) > m_nearDistance;

                if (!m_fitsInFrustum)
                    return;

                Vector3I min, max;
                {
                    var minD = localPosition - m_farDistance;
                    var maxD = localPosition + m_farDistance;
                    MyVoxelCoordSystems.LocalPositionToRenderCellCoord(ref minD, out min);
                    MyVoxelCoordSystems.LocalPositionToRenderCellCoord(ref maxD, out max);
                    Vector3I.Max(ref min, ref Vector3I.Zero, out min);
                    Vector3I.Max(ref max, ref Vector3I.Zero, out max);
                    min >>= m_lodIndex;
                    max >>= m_lodIndex;

                    Vector3I.Min(ref min, ref m_lodSizeMinusOne, out min);
                    Vector3I.Min(ref max, ref m_lodSizeMinusOne, out max);
                }

                if (m_lastMin == min && m_lastMax == max && !m_parent.m_updateClipping)
                    return;

                m_lastMin = min;
                m_lastMax = max;

                LodLevel parentLod, childLod;
                GetNearbyLodLevels(out parentLod, out childLod);

                // Moves cells which are still needed from one collection to another.
                // All that is left behind is unloaded as no longer needed.

                // Move everything in range to collection of next stored cells.
                MyUtils.Swap(ref m_storedCellData, ref m_clippedCells);
                m_storedCellData.Clear();
                MyCellCoord cell = new MyCellCoord(m_lodIndex, ref min);
                for (var it = new Vector3I.RangeIterator(ref min, ref max);
                    it.IsValid(); it.GetNext(out cell.CoordInLod))
                {
                    if (!WasAncestorCellLoaded(parentLod, ref cell))
                        continue;

                    var cellId = cell.PackId64();
                    CellData data;
                    if (m_clippedCells.TryGetValue(cellId, out data))
                        m_clippedCells.Remove(cellId);
                    else
                        data = new CellData();

                    if (data.State == CellState.Invalid)
                    {
                        collector.AddRequest(cellId, data.WasLoaded);
                        data.State = CellState.Pending;
                    }
                    m_storedCellData.Add(cellId, data);
                }
            }

            private static void TestClipSpheres(ref MyCellCoord cell, ref BoundingSphereD nearClipSphere, ref BoundingSphereD farClipSphere, out ContainmentType nearClipRes, out ContainmentType farClipRes)
            {
                BoundingBoxD localAabb;
                MyVoxelCoordSystems.RenderCellCoordToLocalAABB(ref cell, out localAabb);
                localAabb.Inflate(MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << cell.Lod));
                nearClipSphere.Contains(ref localAabb, out nearClipRes);
                farClipSphere.Contains(ref localAabb, out farClipRes);
            }

            internal void KeepOrDiscardClippedCells(RequestCollector collector)
            {
                LodLevel parentLod, childLod;
                GetNearbyLodLevels(out parentLod, out childLod);

                MyCellCoord thisLodCell = new MyCellCoord();
                foreach (var entry in m_clippedCells)
                {
                    var data = entry.Value;
                    bool needed = false;

                    // too far, but less detailed data might be missing so we still check parent
                    thisLodCell.SetUnpack(entry.Key);
                    needed = !WasAncestorCellLoaded(parentLod, ref thisLodCell);

                    if (needed)
                    {
                        if (data.State == CellState.Invalid)
                        {
                            collector.AddRequest(entry.Key, data.WasLoaded);
                            data.State = CellState.Pending;
                        }

                        m_storedCellData.Add(entry.Key, data);
                    }
                    else
                    {
                        if (data.State == CellState.Pending)
                            collector.CancelRequest(entry.Key);
                        if (data.Cell != null)
                            Delete(entry.Key, data);
                    }
                }

                m_clippedCells.Clear();
            }

            internal void UpdateCellsInScene(Vector3D localPosition)
            {
                LodLevel parentLod, childLod;
                GetNearbyLodLevels(out parentLod, out childLod);

                MyCellCoord thisLodCell = new MyCellCoord();
                foreach (var entry in m_nonEmptyCells)
                {
                    var data = entry.Value;
                    Debug.Assert(data.Cell != null);
                    thisLodCell.SetUnpack(entry.Key);

                    if (ChildrenWereLoaded(childLod, ref thisLodCell) || !AllSiblingsWereLoaded(ref thisLodCell))
                    {
                        RemoveFromScene(entry.Key, data);
                    }
                    else
                    {
                        AddToScene(entry.Key, data);
                        //data.Cell.PixelDiscardEnabled = false;
                    }
                }
            }

            private void GetNearbyLodLevels(out LodLevel parentLod, out LodLevel childLod)
            {
                var levels = m_parent.m_lodLevels;

                int parentIdx = m_lodIndex + 1;
                if (levels.IsValidIndex(parentIdx))
                    parentLod = levels[parentIdx];
                else
                    parentLod = null;

                int childIdx = m_lodIndex - 1;
                if (levels.IsValidIndex(childIdx))
                    childLod = levels[childIdx];
                else
                    childLod = null;
            }

            internal void UpdateWorldMatrices(bool sortCellsIntoCullObjects)
            {
                foreach (var data in m_storedCellData.Values)
                {
                    if (data.Cell != null)
                    {
                        data.Cell.UpdateWorldMatrix(ref m_parent.m_worldMatrix, sortCellsIntoCullObjects);
                    }
                }
            }
             
            /// <summary>
            /// Checks ancestor nodes recursively. Typically, this checks at most 9 nodes or so (depending on settings).
            /// </summary>
            private static bool WasAncestorCellLoaded(LodLevel parentLod, ref MyCellCoord thisLodCell)
            {            
                if (parentLod == null || !parentLod.m_fitsInFrustum || !parentLod.Visible)
                {
                    return true;
                }

                Debug.Assert(thisLodCell.Lod == parentLod.m_lodIndex - 1);

                var parentCell = new MyCellCoord(thisLodCell.Lod + 1, thisLodCell.CoordInLod >> 1);
                CellData data;
                if (parentLod.m_storedCellData.TryGetValue(parentCell.PackId64(), out data))
                {
                    return data.WasLoaded;
                }

                LodLevel ancestor;
                if (parentLod.m_parent.m_lodLevels.TryGetValue(parentLod.m_lodIndex+1, out ancestor))
                    return WasAncestorCellLoaded(ancestor, ref parentCell);
                else
                    return false;
            }

            /// <summary>
            /// Checks only immediate children (any deeper would take too long).
            /// </summary>
            private static bool ChildrenWereLoaded(LodLevel childLod, ref MyCellCoord thisLodCell)
            {
                if (childLod == null || !childLod.Visible)
                    return false;

                Debug.Assert(thisLodCell.Lod == childLod.m_lodIndex + 1);

                var childLodCell = new MyCellCoord();
                childLodCell.Lod = childLod.m_lodIndex;
                var start = thisLodCell.CoordInLod << 1;
                var end = start + 1;

                Vector3I.Min(ref end, ref childLod.m_lodSizeMinusOne, out end);
                for (childLodCell.CoordInLod.Z = start.Z; childLodCell.CoordInLod.Z <= end.Z; ++childLodCell.CoordInLod.Z)
                for (childLodCell.CoordInLod.Y = start.Y; childLodCell.CoordInLod.Y <= end.Y; ++childLodCell.CoordInLod.Y)
                for (childLodCell.CoordInLod.X = start.X; childLodCell.CoordInLod.X <= end.X; ++childLodCell.CoordInLod.X)
                {
                    var key = childLodCell.PackId64();
                    CellData data;
                    if (!childLod.m_storedCellData.TryGetValue(key, out data))
                    {
                        return false;
                    }

                    if (!data.WasLoaded)
                    {
                        return false;
                    }
                }

                return true;
            }

            
            private bool AllSiblingsWereLoaded(ref MyCellCoord thisLodCell)
            {
                MyCellCoord sibling;
                sibling.Lod = thisLodCell.Lod;
                var start = new Vector3I( // get rid of lowest bit to make this min child of parent
                    thisLodCell.CoordInLod.X & (-2),
                    thisLodCell.CoordInLod.Y & (-2),
                    thisLodCell.CoordInLod.Z & (-2));
                var end = start + 1;
                Vector3I.Min(ref end, ref m_lodSizeMinusOne, out end);
                for (sibling.CoordInLod.Z = start.Z; sibling.CoordInLod.Z <= end.Z; ++sibling.CoordInLod.Z)
                for (sibling.CoordInLod.Y = start.Y; sibling.CoordInLod.Y <= end.Y; ++sibling.CoordInLod.Y)
                for (sibling.CoordInLod.X = start.X; sibling.CoordInLod.X <= end.X; ++sibling.CoordInLod.X)
                {
                    var key = sibling.PackId64();
                    CellData data;
                    if (!m_storedCellData.TryGetValue(key, out data))
                    {
                        return false;
                    }

                    if (!data.WasLoaded)
                    {
                        return false;
                    }
                }

                return true;
            }

            private void Delete(UInt64 key, CellData data = null)
            {
                data = data ?? m_storedCellData[key];
                if (data.Cell != null)
                {
                    m_nonEmptyCells.Remove(key);
                    RemoveFromScene(key, data);
                    m_parent.m_cellHandler.DeleteCell(data.Cell);
                }
                m_storedCellData.Remove(key);
            }

            private void AddToScene(UInt64 key, CellData data = null)
            {
                data = data ?? m_storedCellData[key];
                if (!data.InScene)
                {
                    Debug.Assert(data.Cell != null);
                    m_parent.m_cellHandler.AddToScene(data.Cell);
                    data.InScene = true;
                }
            }

            private void RemoveFromScene(UInt64 key, CellData data = null)
            {
                data = data ?? m_storedCellData[key];
                if (data.InScene)
                {
                    Debug.Assert(data.Cell != null);
                    m_parent.m_cellHandler.RemoveFromScene(data.Cell);
                    data.InScene = false;
                }
            }
        }

        class RequestCollector
        {
            private readonly HashSet<UInt64> m_sentRequests = new HashSet<UInt64>();
            private readonly HashSet<UInt64> m_unsentRequestsHigh = new HashSet<UInt64>();
            private readonly HashSet<UInt64>[] m_unsentRequestsLow;
            private readonly HashSet<UInt64> m_cancelRequests = new HashSet<UInt64>();

            /// <summary>
            /// Sent requests + low priority requests are checked against this.
            /// High priority requests should be sent even when they are over limit.
            /// </summary>
            private int m_maxRequests = 10000;//int.MaxValue;
            private uint m_clipmapId;

            public bool SentRequestsEmpty
            {
                get { return m_sentRequests.Count == 0; }
            }

            public RequestCollector(uint clipmapId)
            {
                m_clipmapId = clipmapId;
                m_unsentRequestsLow = new HashSet<UInt64>[MyCellCoord.MAX_LOD_COUNT];
                for (int i = 0; i < m_unsentRequestsLow.Length; i++)
                {
                    m_unsentRequestsLow[i] = new HashSet<UInt64>();
                }
            }

            public void AddRequest(UInt64 cellId, bool isHighPriority)
            {
                m_cancelRequests.Remove(cellId);
                if (!m_sentRequests.Contains(cellId))
                {
                    if (isHighPriority)
                        m_unsentRequestsHigh.Add(cellId);
                    else
                    {
                        var lod = MyCellCoord.UnpackLod(cellId);
                        m_unsentRequestsLow[lod].Add(cellId);
                    }
                }
            }

            public void CancelRequest(UInt64 cellId)
            {
                var lod = MyCellCoord.UnpackLod(cellId);
                m_unsentRequestsLow[lod].Remove(cellId);
                if (m_sentRequests.Contains(cellId))
                {
                    m_cancelRequests.Add(cellId);
                }
            }

            public void Submit()
            {
                ProfilerShort.Begin("RequestCollector.Submit");

                MyCellCoord cell = default(MyCellCoord);
                foreach (var cellId in m_cancelRequests)
                {
                    cell.SetUnpack(cellId);
                    MyRenderProxy.CancelClipmapCell(m_clipmapId, cell);
                    bool removed = m_sentRequests.Remove(cellId);
                    Debug.Assert(removed);
                }

                foreach (var highPriorityRequest in m_unsentRequestsHigh)
                {
                    cell.SetUnpack(highPriorityRequest);
                    MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, highPriority: true);
                }
                m_unsentRequestsHigh.Clear();

                int addedCount = 0;
                for (int i = m_unsentRequestsLow.Length - 1; i >= 0; i--)
                {
                    var unsent = m_unsentRequestsLow[i];
                    while (0 < unsent.Count && m_sentRequests.Count < m_maxRequests)
                    {
                        var cellId = unsent.FirstElement();

                        cell.SetUnpack(cellId);
                        // Do Z-order style iteration of siblings that also need to
                        // be requested. This ensures faster processing of cells and
                        // shorter time when both lods are rendered.
                        var baseCoord = (cell.CoordInLod >> 1) << 1;
                        var offset = Vector3I.Zero;
                        for (var it = new Vector3I.RangeIterator(ref Vector3I.Zero, ref Vector3I.One);
                            it.IsValid(); it.GetNext(out offset))
                        {
                            cell.CoordInLod = baseCoord + offset;
                            cellId = cell.PackId64();
                            if (!unsent.Remove(cellId))
                            {
                                continue;
                            }

                            Debug.Assert(!m_cancelRequests.Contains(cellId));
                            MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, highPriority: false);
                            bool added = m_sentRequests.Add(cellId);
                            Debug.Assert(added);
                            addedCount++;
                        }
                    }

                    // When set reaches reasonably small size, stop freeing memory
                    if (unsent.Count > 100)
                        unsent.TrimExcess();
                }

                m_cancelRequests.Clear();

                ProfilerShort.End();
            }

            internal void RequestFulfilled(UInt64 cellId)
            {
                m_sentRequests.Remove(cellId);
            }
        }


    }
}
