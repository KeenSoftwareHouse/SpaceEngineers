using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace VRageRender
{
    class MyLodMeshMergeHandler
    {
        private class MyLodMeshMerge
        {
            private struct MergeJobInfo
            {
                public ulong CurrentWorkId;
                public List<LodMeshId> LodMeshesBeingMerged;
                public ulong NextPossibleMergeStartTime;

                public const int MergeJobDelay = 30;    // Frames
            }

            private readonly MyClipmap m_parentClipmap;
            private readonly int m_lod;

            private readonly MergeJobInfo[] m_mergeJobs;
            private readonly MyClipmapCellProxy[] m_mergedLodMeshProxies;
            private readonly HashSet<MyActor>[] m_trackedActors;   // List of actors that correspond to the cells that have been or will be merged
            private readonly HashSet<int> m_dirtyProxyIndices;

            private readonly Dictionary<MyClipmapCellProxy, int> m_cellProxyToAabbProxy;
            private readonly MyDynamicAABBTreeD m_boundingBoxes;

            private readonly int m_lodDivisions;

            private static ulong m_runningWorkId = 0;

            internal int Lod { get { return m_lod; } }

            internal MyLodMeshMerge(MyClipmap parentClipmap, int lod, int lodDivisions, ref MatrixD worldMatrix, ref Vector3D massiveCenter, float massiveRadius, RenderFlags renderFlags)
            {
                Debug.Assert(parentClipmap != null, "Parent clipmap cannot be null");
                Debug.Assert(lod >= 0, "Lod level must be non-negative");
                Debug.Assert(lodDivisions >= 0, "Invalid number of lod divisions");
                m_parentClipmap = parentClipmap;
                m_lod = lod;
                m_lodDivisions = lodDivisions;

                if (m_lodDivisions <= 0)
                    return;

                m_dirtyProxyIndices = new HashSet<int>();
                m_cellProxyToAabbProxy = new Dictionary<MyClipmapCellProxy, int>();
                m_boundingBoxes = new MyDynamicAABBTreeD(Vector3D.Zero);

                int cellCount = lodDivisions*lodDivisions*lodDivisions;
                m_mergeJobs = new MergeJobInfo[cellCount];
                m_mergedLodMeshProxies = new MyClipmapCellProxy[cellCount];
                m_trackedActors = new HashSet<MyActor>[cellCount];

                for (int divideIndex = 0; divideIndex < cellCount; ++divideIndex)
                {
                    m_mergedLodMeshProxies[divideIndex] = new MyClipmapCellProxy(new MyCellCoord(Lod, GetCellFromDivideIndex(divideIndex)), ref worldMatrix, massiveCenter, massiveRadius, renderFlags, true);
                    m_mergeJobs[divideIndex] = new MergeJobInfo { CurrentWorkId = 0, LodMeshesBeingMerged = new List<LodMeshId>(), NextPossibleMergeStartTime = MyCommon.FrameCounter };
                    m_trackedActors[divideIndex] = new HashSet<MyActor>();
                    m_dirtyProxyIndices.Add(divideIndex);
                }
            }

            internal void Update()
            {
                if (!IsUsed())
                    return;

                bool invalidateLod = false;
                foreach (var proxyIndex in m_dirtyProxyIndices)
                {
                    var cellProxy = m_mergedLodMeshProxies[proxyIndex];
                    if (MyMeshes.CanStartMerge(cellProxy.MeshId, 1))
                    {
                        Vector3I mergeCell = MyMeshes.GetVoxelInfo(cellProxy.MeshId).Coord;
                        int divideIndex = GetDivideIndexFromMergeCell(ref mergeCell);
                        MyMergedLodMeshId mergedId = MyMeshes.GetMergedLodMesh(cellProxy.MeshId, 0);
                        invalidateLod |= mergedId.Info.MergedLodMeshes.Count > 0;
                        mergedId.Info.PendingLodMeshes.UnionWith(mergedId.Info.MergedLodMeshes);
                        mergedId.Info.MergedLodMeshes.Clear();

                        TryCancelMergeJob(divideIndex, MeshId.NULL);
                        TryStartMergeJob(divideIndex, 1);
                    }
                }
                m_dirtyProxyIndices.Clear();
                if (invalidateLod)
                {
                    InvalidateAllMergedMeshesInLod();
                }
            }

            internal void UpdateMesh(MyRenderMessageUpdateMergedVoxelMesh updateMessage)
            {
                int divideIndex = GetDivideIndexFromMergeCell(ref updateMessage.Metadata.Cell.CoordInLod);

                // A job wasn't cancelled in time and made it here
                if (m_mergeJobs[divideIndex].CurrentWorkId != updateMessage.WorkId)
                    return;

                MyMergedLodMeshId mergedId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                bool mergeSuccessful = m_mergedLodMeshProxies[divideIndex].UpdateMergedMesh(updateMessage);
                if (mergeSuccessful)
                {
                    Debug.Assert(mergedId.Info.MergedLodMeshes.Count == 0, "Merging into an already merged mesh");
                    mergedId.Info.MergedLodMeshes.UnionWith(m_mergeJobs[divideIndex].LodMeshesBeingMerged);
                    SwitchMergeState(divideIndex, true);
                }
                else
                    mergedId.Info.PendingLodMeshes.UnionWith(m_mergeJobs[divideIndex].LodMeshesBeingMerged);

                ResetMerge(divideIndex);
            }

            internal bool OnAddedToScene(MyClipmapCellProxy cellProxy)
            {
                if (!IsUsed())
                    return false;

                bool lodAabbChanged = false;
                int rootProxy = m_boundingBoxes.GetRoot();
                BoundingBoxD lodAabbBefore = BoundingBoxD.CreateInvalid();
                if(rootProxy != -1)
                    lodAabbBefore = m_boundingBoxes.GetAabb(rootProxy);

                BoundingBoxD cellAabb = (BoundingBoxD)cellProxy.LocalAabb;
                m_cellProxyToAabbProxy.Add(cellProxy, m_boundingBoxes.AddProxy(ref cellAabb, null, 0));

                if(rootProxy != -1)
                {
                    BoundingBoxD lodAabbAfter = m_boundingBoxes.GetAabb(rootProxy);
                    lodAabbChanged = lodAabbBefore.Equals(lodAabbAfter);
                }

                if(lodAabbChanged)
                    InvalidateAllMergedMeshesInLod();

                Vector3D translation = cellProxy.Translation;
                int divideIndex = GetDivideIndex(ref translation);

                m_trackedActors[divideIndex].Add(cellProxy.Actor);

                MyMergedLodMeshId mergedLodMeshId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                LodMeshId lodMeshToMerge = MyMeshes.GetLodMesh(cellProxy.MeshId, 0);
                bool mergedMesh = mergedLodMeshId.MergeLodMesh(lodMeshToMerge);
                if (mergedMesh)
                {
                    InvalidateAllMergedMeshesInLod();
                }

                TryCancelMergeJob(divideIndex, MeshId.NULL);
                bool startedMerge = TryStartMergeJob(divideIndex, 1000);

                bool shouldMarkDirty = !mergedMesh && !startedMerge;
                if (shouldMarkDirty)
                    m_dirtyProxyIndices.Add(divideIndex);

                return shouldMarkDirty;
            }

            internal bool OnRemovedFromScene(MyClipmapCellProxy cellProxy)
            {
                if (!IsUsed())
                    return false;

                bool shouldMarkDirty = false;
                Vector3D translation = cellProxy.Translation;
                int divideIndex;
                if (TryGetDivideIndex(ref translation, out divideIndex))
                {
                    MyMergedLodMeshId mergedLodMeshId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                    LodMeshId lodMeshId = MyMeshes.GetLodMesh(cellProxy.MeshId, 0);

                    bool unmergedMesh = mergedLodMeshId.UnmergeLodMesh(lodMeshId);
                    if (unmergedMesh)
                    {
                        InvalidateAllMergedMeshesInLod();
                    }

                    TryCancelMergeJob(divideIndex, cellProxy.MeshId);
                    bool startedJob = TryStartMergeJob(divideIndex, 1000);

                    shouldMarkDirty = unmergedMesh && !startedJob;
                    if (shouldMarkDirty)
                        m_dirtyProxyIndices.Add(divideIndex);

                    m_trackedActors[divideIndex].Remove(cellProxy.Actor);
                }

                int proxyId;
                if (m_cellProxyToAabbProxy.TryGetValue(cellProxy, out proxyId))
                {
                    m_boundingBoxes.RemoveProxy(m_cellProxyToAabbProxy[cellProxy]);
                    m_cellProxyToAabbProxy.Remove(cellProxy);
                }

                return shouldMarkDirty;
            }

            internal bool OnDeleteCell(MyClipmapCellProxy cellProxy)
            {
                if (!IsUsed())
                    return false;

                bool unloadedCell = false;
                Vector3D translation = cellProxy.Translation;
                int divideIndex;
                if (TryGetDivideIndex(ref translation, out divideIndex))
                {
                    MyMergedLodMeshId mergedLodMeshId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                    LodMeshId lodMeshId = MyMeshes.GetLodMesh(cellProxy.MeshId, 0);

                    bool unmergedMesh = mergedLodMeshId.UnmergeLodMesh(lodMeshId);
                    if (unmergedMesh)
                    {
                        InvalidateAllMergedMeshesInLod();
                    }

                    TryCancelMergeJob(divideIndex, cellProxy.MeshId);

                    cellProxy.Unload();
                    unloadedCell = true;

                    TryStartMergeJob(divideIndex, 1000);

                    if (unmergedMesh && unloadedCell)
                        m_dirtyProxyIndices.Add(divideIndex);

                    m_trackedActors[divideIndex].Remove(cellProxy.Actor);

                }

                int proxyId;
                if (m_cellProxyToAabbProxy.TryGetValue(cellProxy, out proxyId))
                {
                    m_boundingBoxes.RemoveProxy(m_cellProxyToAabbProxy[cellProxy]);
                    m_cellProxyToAabbProxy.Remove(cellProxy);
                }

                return unloadedCell;
            }

            private bool TryStartMergeJob(int divideIndex, int pendingThreshold)
            {
                bool jobStarted = false;
                var meshId = m_mergedLodMeshProxies[divideIndex].MeshId;
                var workId = m_runningWorkId + 1;

                if (MyCommon.FrameCounter >= m_mergeJobs[divideIndex].NextPossibleMergeStartTime &&
                    MyMeshes.TryStartMerge(meshId, m_parentClipmap.Id, pendingThreshold, m_mergeJobs[divideIndex].LodMeshesBeingMerged, workId))
                {
                    // Sent message
                    m_mergeJobs[divideIndex].CurrentWorkId = ++m_runningWorkId;
                    m_mergeJobs[divideIndex].NextPossibleMergeStartTime = MyCommon.FrameCounter + MergeJobInfo.MergeJobDelay;
                    jobStarted = true;
                }
                return jobStarted;
            }

            private bool TryCancelMergeJob(int divideIndex, MeshId meshIdToRemove)
            {
                bool canceled = false;
                if (m_mergeJobs[divideIndex].LodMeshesBeingMerged.Count > 0)
                {
                    // Send the cancel message
                    MyRenderProxy.CancelVoxelMeshMerge(m_parentClipmap.Id, m_mergeJobs[divideIndex].CurrentWorkId);

                    if (meshIdToRemove != MeshId.NULL)
                        m_mergeJobs[divideIndex].LodMeshesBeingMerged.Remove(MyMeshes.GetLodMesh(meshIdToRemove, 0));

                    // Put the canceled meshes back into the correct pending queue
                    var mergedId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                    mergedId.Info.PendingLodMeshes.UnionWith(m_mergeJobs[divideIndex].LodMeshesBeingMerged);

                    ResetMerge(divideIndex);
                    canceled = true;
                }
                return canceled;
            }

            private void ResetMerge(int divideIndex)
            {
                var mergeJobInfo = m_mergeJobs[divideIndex];

                mergeJobInfo.CurrentWorkId = 0;
                mergeJobInfo.LodMeshesBeingMerged.Clear();

                m_mergeJobs[divideIndex] = mergeJobInfo;
            }

            internal void ResetMeshes()
            {
                if (!IsUsed())
                    return;

                foreach (var mergedProxy in m_mergedLodMeshProxies)
                {
                    var mergedMeshId = MyMeshes.GetMergedLodMesh(mergedProxy.MeshId, 0);
                    mergedMeshId.Info.PendingLodMeshes.Clear();
                    mergedMeshId.Info.MergedLodMeshes.Clear();
                }

                for (int divideIndex = 0; divideIndex < m_lodDivisions; ++divideIndex)
                {
                    SwitchMergeState(divideIndex, false);
                    m_trackedActors[divideIndex].Clear();
                    ResetMerge(divideIndex);
                }
                m_boundingBoxes.Clear();
                m_cellProxyToAabbProxy.Clear();
                m_dirtyProxyIndices.Clear();
            }

            private void SwitchMergeStateForAll(bool mergeState)
            {
                for(int divideIndex = 0; divideIndex < m_lodDivisions; ++divideIndex)
                    SwitchMergeState(divideIndex, mergeState);
            }

            private void SwitchMergeState(int divideIndex, bool mergeState)
            {
                foreach (var actor in m_trackedActors[divideIndex])
                    actor.MarkRenderDirty();

                m_mergedLodMeshProxies[divideIndex].Actor.MarkRenderDirty();
            }

            private void InvalidateAllMergedMeshesInLod()
            {
                // TODO: Redistribute actors
                for (int divideIndex = 0; divideIndex < m_lodDivisions; ++divideIndex)
                {
                    MyMergedLodMeshId mergedLodMeshId = MyMeshes.GetMergedLodMesh(m_mergedLodMeshProxies[divideIndex].MeshId, 0);
                    bool markDirty = mergedLodMeshId.Info.MergedLodMeshes.Count > 0;
                    mergedLodMeshId.Info.PendingLodMeshes.UnionWith(mergedLodMeshId.Info.MergedLodMeshes);
                    mergedLodMeshId.Info.MergedLodMeshes.Clear();
                    SwitchMergeState(divideIndex, false);

                    if(markDirty)
                        m_dirtyProxyIndices.Add(divideIndex);
                }
            }

            private bool TryGetDivideIndex(ref Vector3D translation, out int divideIndex)
            {
                divideIndex = 0;
                if (m_boundingBoxes.GetRoot() < 0)
                    return false;

                Vector3I renderCellCoord;
                BoundingBoxD lodAabb = m_boundingBoxes.GetAabb(m_boundingBoxes.GetRoot());
                Vector3D relativeTranslation = Vector3D.Max(translation - lodAabb.Min, Vector3D.Zero);
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lod, ref relativeTranslation, out renderCellCoord);
                divideIndex = GetDivideIndex(ref renderCellCoord);
                return true;
            }

            private int GetDivideIndex(ref Vector3D translation)
            {
                Vector3I renderCellCoord;
                BoundingBoxD lodAabb = m_boundingBoxes.GetAabb(m_boundingBoxes.GetRoot());
                Vector3D relativeTranslation = Vector3D.Max(translation - lodAabb.Min, Vector3D.Zero);
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lod, ref relativeTranslation, out renderCellCoord);
                return GetDivideIndex(ref renderCellCoord);
            }

            private int GetDivideIndex(ref Vector3I renderCellCoord)
            {
                // TODO: Optimize
                int divideIndex = 0;
                if (m_lodDivisions > 1)
                {
                    BoundingBoxD lodAabb = m_boundingBoxes.GetAabb(m_boundingBoxes.GetRoot());
                    Vector3I test = Vector3I.Round(lodAabb.Size / (double)MyVoxelCoordSystems.RenderCellSizeInMeters(m_lod));
                    //Vector3I lodSizeMinusOne = m_parentClipmap.LodSizeMinusOne(m_lod);
                    //Vector3I lodSize = lodSizeMinusOne + Vector3I.One;
                    Vector3I lodSize = test;
                    Vector3I lodSizeMinusOne = test - 1;
                    Vector3I lodDivision = Vector3I.One * (m_lodDivisions - 1);

                    var cellIterator = new Vector3I.RangeIterator(ref Vector3I.Zero, ref lodDivision);
                    for (; cellIterator.IsValid(); cellIterator.MoveNext())
                    {
                        Vector3I currentDivision = cellIterator.Current;
                        Vector3I min = currentDivision * lodSize / m_lodDivisions;
                        Vector3I max = (currentDivision + Vector3I.One) * lodSize / m_lodDivisions;
                        if (renderCellCoord.IsInsideInclusive(ref min, ref max))
                            break;
                    }
                    Debug.Assert(cellIterator.IsValid(), "Valid division index not found!");
                    Vector3I foundCell = cellIterator.Current;
                    divideIndex = GetDivideIndexFromMergeCell(ref foundCell);
                }
                return divideIndex;
            }

            private Vector3I GetCellFromDivideIndex(int divideIndex)
            {
                return new Vector3I(divideIndex % m_lodDivisions, divideIndex / m_lodDivisions % m_lodDivisions, divideIndex / (m_lodDivisions * m_lodDivisions));
            }

            private int GetDivideIndexFromMergeCell(ref Vector3I mergeCell)
            {
                return mergeCell.X + mergeCell.Y * m_lodDivisions + mergeCell.Z * (m_lodDivisions * m_lodDivisions);
            }

            private bool IsUsed()
            {
                return m_lodDivisions >= 1;
            }

            internal void DebugDrawCells()
            {
                if (!IsUsed())
                    return;

                foreach (var cellProxy in m_mergedLodMeshProxies)
                {
                    var mergedMeshId = MyMeshes.GetMergedLodMesh(cellProxy.MeshId, 0);
                    if (mergedMeshId.Info.MergedLodMeshes.Count <= 0)
                        continue;

                    BoundingBoxD worldAabb = cellProxy.LocalAabb.Transform(cellProxy.WorldMatrix);
                    MyRenderProxy.DebugDrawAABB(worldAabb, MyClipmap.LOD_COLORS[m_lod], 1.0f, 1.0f, false);
                }
            }
        }

        private MyLodMeshMerge[] m_mergesPerLod;
        private readonly HashSet<int> m_dirtyLodMergeIndices = new HashSet<int>();

        private const ulong m_updateCheckInterval = 180;
        private ulong m_mergeCounter = 0;

        private static readonly int[] NUM_DIVISIONS_PER_LOD = new int[] {
            0,  // 1
            0,  // 2
            0,  // 3
            0,  // 4

            0,  // 5
            0,  // 6
            0,  // 7
            1,  // 8

            1,  // 9
            1,  // 10
            1,  // 11
            1,  // 12

            1,  // 13
            1,  // 14
            1,  // 15
            1,  // 16
        };

        internal MyLodMeshMergeHandler(MyClipmap parentClipmap, int lodCount, int lodDivisions, ref MatrixD worldMatrix, ref Vector3D massiveCenter, float massiveRadius, RenderFlags renderFlags)
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return;

            m_mergesPerLod = new MyLodMeshMerge[lodCount];

            for(int lodIndex = 0; lodIndex < m_mergesPerLod.Length; ++lodIndex)
            {
                int divisions = NUM_DIVISIONS_PER_LOD[lodIndex];
                m_mergesPerLod[lodIndex] = new MyLodMeshMerge(parentClipmap, lodIndex, divisions, ref worldMatrix, ref massiveCenter, massiveRadius, renderFlags);
                m_dirtyLodMergeIndices.Add(lodIndex);
            }
        }

        internal bool Update()
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return false;

            if (MyCommon.FrameCounter - m_mergeCounter < m_updateCheckInterval)
                return false;

            foreach (int lodMergeIndex in m_dirtyLodMergeIndices)
                m_mergesPerLod[lodMergeIndex].Update();

            m_dirtyLodMergeIndices.Clear();
            m_mergeCounter = MyCommon.FrameCounter;
            return true;
        }

        internal void UpdateMesh(MyRenderMessageUpdateMergedVoxelMesh updateMessage)
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return;

            m_mergesPerLod[updateMessage.Lod].UpdateMesh(updateMessage);
        }

        internal void OnAddedToScene(MyClipmapCellProxy cellProxy)
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return;

            if (m_mergesPerLod[cellProxy.Lod].OnAddedToScene(cellProxy))
                m_dirtyLodMergeIndices.Add(cellProxy.Lod);
        }

        internal void OnRemovedFromScene(MyClipmapCellProxy cellProxy)
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return;

            if (m_mergesPerLod[cellProxy.Lod].OnRemovedFromScene(cellProxy))
                m_dirtyLodMergeIndices.Add(cellProxy.Lod);
        }

        internal bool OnDeleteCell(MyClipmapCellProxy cellProxy)
        {
            if (!MyRenderProxy.Settings.EnableVoxelMerging)
                return false;

            bool cellDeleted = m_mergesPerLod[cellProxy.Lod].OnDeleteCell(cellProxy);
            if(cellDeleted)
                m_dirtyLodMergeIndices.Add(cellProxy.Lod);

            return cellDeleted;
        }

        internal void ResetMeshes()
        {
            // TODO: Start merges when merging turned on

            foreach (var lodMerge in m_mergesPerLod)
                lodMerge.ResetMeshes();

            m_dirtyLodMergeIndices.Clear();
            MyMeshes.LodMeshToMerged.Clear();
        }

        internal void DebugDrawCells()
        {
            foreach (var lodMerge in m_mergesPerLod)
                lodMerge.DebugDrawCells();
        }
    }

    internal static class MyMergedLodMeshIdExtensions
    {
        internal static bool MergeLodMesh(this MyMergedLodMeshId mergedLodMesh, LodMeshId lodMesh)
        {
            var pendingLodMeshes = mergedLodMesh.Info.PendingLodMeshes;
            var mergedLodMeshes = mergedLodMesh.Info.MergedLodMeshes;

            bool alreadyMerged = mergedLodMeshes.Contains(lodMesh);
            if (alreadyMerged)
            {
                pendingLodMeshes.UnionWith(mergedLodMeshes);
                mergedLodMeshes.Clear();
            }
            else
                pendingLodMeshes.Add(lodMesh);

            MyMeshes.LinkLodMeshToMerged(lodMesh, mergedLodMesh);
            return alreadyMerged;
        }

        internal static bool UnmergeLodMesh(this MyMergedLodMeshId mergedLodMesh, LodMeshId lodMesh)
        {
            var pendingLodMeshes = mergedLodMesh.Info.PendingLodMeshes;
            var mergedLodMeshes = mergedLodMesh.Info.MergedLodMeshes;

            bool alreadyMerged = mergedLodMeshes.Contains(lodMesh);
            if (alreadyMerged)
                MyMeshes.UnlinkLodMeshFromMerged(lodMesh);

            if (pendingLodMeshes.Remove(lodMesh))
                Debug.Assert(!alreadyMerged, "Lod mesh set as pending and merged at the same time!");

            if (alreadyMerged)
            {
                mergedLodMeshes.Remove(lodMesh);
                pendingLodMeshes.UnionWith(mergedLodMeshes);
                mergedLodMeshes.Clear();
            }

            return alreadyMerged;
        }

        internal static bool CanStartMerge(this MyMergedLodMeshId mergedLodMeshId, int pendingThreshold)
        {
            var pendingLodMeshes = mergedLodMeshId.Info.PendingLodMeshes;

            return pendingLodMeshes.Count >= pendingThreshold;
        }


        private static readonly List<MyClipmapCellBatch> m_tmpBatches = new List<MyClipmapCellBatch>();
        private static readonly List<MyClipmapCellMeshMetadata> m_tmpMetadata = new List<MyClipmapCellMeshMetadata>();

        /// <summary>
        /// Sends a merge job message if mergedMeshId has more than pendingThreshold pending lod meshes.
        /// </summary>
        /// <returns>True if message was sent; false otherwise</returns>
        internal static bool TryStartMerge(this MyMergedLodMeshId mergedLodMeshId, uint clipmapId, int pendingThreshold, List<LodMeshId> outLodMeshesSent, ulong workId)
        {
            Debug.Assert(outLodMeshesSent != null && outLodMeshesSent.Count == 0, "Lod mesh list not empty!");
            var pendingLodMeshes = mergedLodMeshId.Info.PendingLodMeshes;
            var mergedLodMeshes = mergedLodMeshId.Info.MergedLodMeshes;

            bool pendingMeshesOverThreshold = pendingLodMeshes.Count >= pendingThreshold;
            if (pendingMeshesOverThreshold)
            {
                foreach (LodMeshId lodMesh in pendingLodMeshes)
                {
                    m_tmpBatches.AddArray(lodMesh.Info.DataBatches);
                    outLodMeshesSent.Add(lodMesh);
                    m_tmpMetadata.Add(lodMesh.Info.BatchMetadata);
                }
                foreach (LodMeshId lodMesh in mergedLodMeshes)
                {
                    m_tmpBatches.AddArray(lodMesh.Info.DataBatches);
                    outLodMeshesSent.Add(lodMesh);
                    m_tmpMetadata.Add(lodMesh.Info.BatchMetadata);
                }
                pendingLodMeshes.Clear();
                mergedLodMeshes.Clear();

                MyVoxelCellInfo cellInfo = MyMeshes.GetVoxelInfo(mergedLodMeshId);
                MyRenderProxy.MergeVoxelMeshes(clipmapId, workId, m_tmpMetadata, new MyCellCoord(cellInfo.Lod, cellInfo.Coord), m_tmpBatches);

                m_tmpBatches.Clear();
                m_tmpMetadata.Clear();
            }
            return pendingMeshesOverThreshold;
        }
    }
}
