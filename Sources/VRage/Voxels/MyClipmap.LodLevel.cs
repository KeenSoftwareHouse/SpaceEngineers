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
        public enum CellState
        {
            Invalid,
            Pending,
            Loaded
        }

        public class CellData
        {
            public CellState State;
            public IMyClipmapCell Cell;
            public bool InScene;
            public bool WasLoaded;
        }

        class LodLevel
        {
            FastResourceLock m_storedCellDataLock = new FastResourceLock();
            private Dictionary<UInt64, CellData> m_storedCellData = new Dictionary<UInt64, CellData>();

            private Dictionary<UInt64, CellData> m_nonEmptyCells = new Dictionary<UInt64, CellData>();

            // temporary dictionaries
            private Dictionary<UInt64, CellData> m_clippedCells = new Dictionary<UInt64, CellData>();

            private int m_lodIndex;
            private Vector3I m_lodSizeMinusOne;
            private MyClipmap m_clipmap;
            private Vector3I m_lastMin = Vector3I.MaxValue;
            private Vector3I m_lastMax = Vector3I.MinValue;
       

            internal LodLevel(MyClipmap parent, int lodIndex, Vector3I lodSize)
            {
                m_clipmap = parent;
                m_lodIndex = lodIndex;
                m_lodSizeMinusOne = lodSize - 1;
            }

            internal void UnloadContent()
            {
                foreach (var data in m_nonEmptyCells.Values)
                {
                    if (data.InScene)
                        m_clipmap.m_cellHandler.RemoveFromScene(data.Cell);
                    m_clipmap.m_cellHandler.DeleteCell(data.Cell);
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
                    using (m_storedCellDataLock.AcquireSharedUsing())
                    {
                        if (m_storedCellData.TryGetValue(id, out data))
                        {
                            data.State = CellState.Invalid;
                        }
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
                var cellId = msg.Metadata.Cell.PackId64();
                CellData data;

                using (m_storedCellDataLock.AcquireSharedUsing())
                {
                    if (m_storedCellData.TryGetValue(cellId, out data))
                    {
                        if (data.Cell == null && msg.Batches.Count != 0)
                        {
                            data.Cell = m_clipmap.m_cellHandler.CreateCell(m_clipmap.m_scaleGroup, msg.Metadata.Cell, ref m_clipmap.m_worldMatrix);
                            m_nonEmptyCells[cellId] = data;
                        }
                        else if (data.Cell != null && msg.Batches.Count == 0)
                        {
                            RemoveFromScene(cellId, data);
                            m_nonEmptyCells.Remove(cellId);
                            m_clipmap.m_cellHandler.DeleteCell(data.Cell);
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
            }

         
            private bool TryAddCellRequest(RequestCollector collector, LodLevel parentLod, MyCellCoord cell, ulong cellId, CellData data)
            {
                var shiftToParent = MyVoxelCoordSystems.RenderCellSizeShiftToLessDetailed(cell.Lod);
                var parentCell = parentLod != null ? new MyCellCoord(parentLod.m_lodIndex, cell.CoordInLod >> shiftToParent) : cell;
                BoundingBoxD worldAABB;
                MyVoxelCoordSystems.RenderCellCoordToWorldAABB(m_clipmap.m_worldMatrix.Translation, ref parentCell, out worldAABB);
                worldAABB.Inflate(-1.0f * m_lodIndex * m_lodIndex);

                var parentCellId = parentCell.PackId64();
                //if (PriorityFunc(worldAABB.Center, parentLod, parentCellId) == int.MaxValue) //this cell would just slow down sorting, it will be added again if needed
                //    return false;
                collector.AddRequest(cellId, data.WasLoaded, () => PriorityFunc(worldAABB, parentLod, parentCellId, cell), (c) => DebugDrawJob(c, worldAABB));
                data.State = CellState.Pending;
                return true;
            }

            private void DebugDrawJob(Color c, BoundingBoxD wAABB)
            {
                ////return;
                //var cam = CameraMatrixGetter();
                //wAABB = wAABB.Translate(-m_parent.m_worldMatrix.Translation);
                //wAABB = wAABB.Transform(MatrixD.CreateScale(0.001f));
                //wAABB = wAABB.Translate(-wAABB.Center / 2 + cam.Translation);
                MyRenderProxy.DebugDrawAABB(wAABB, c, 1, 1, false);
            }


            private static readonly Vector4[] LOD_COLORS = 
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
                if (m_lodIndex > 5)
                    return;


                if (m_lodIndex == 1)
                {
                    float sizeInMetres = MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex);

                    //var start = localFarCameraBox.Min;
                    //var end = localFarCameraBox.Max;
                    var start = m_localNearCameraBox.Min;
                    var end = m_localNearCameraBox.Max;
                    Vector3I coord = start;

                    Color nearColor = Color.Yellow;
                    Color farColor = Color.White;

                    var startF = m_localFarCameraBox.Min;
                    var endF = m_localFarCameraBox.Max;
                    Vector3I coordF = startF;

    //                for (var it = new Vector3I.RangeIterator(ref startF, ref endF);
    //it.IsValid(); it.GetNext(out coordF))
    //                {
    //                    Vector3D min = Vector3D.Transform((Vector3D)(sizeInMetres * coordF), m_parent.m_worldMatrix);
    //                    Vector3D max = Vector3D.Transform((Vector3D)(sizeInMetres * coordF + new Vector3(sizeInMetres)), m_parent.m_worldMatrix);

    //                    BoundingBoxD aabb = new BoundingBoxD(min, max);
    //                    MyRenderProxy.DebugDrawAABB(aabb, farColor, 1, 1, false);

    //                    if (Vector3D.Distance(CameraFrustumGetter().Matrix.Translation, aabb.Center) < 200)
    //                        MyRenderProxy.DebugDrawText3D(aabb.Center, coordF.ToString(), farColor, 0.5f, false);
    //                }


                    for (var it = new Vector3I.RangeIterator(ref start, ref end);
    it.IsValid(); it.GetNext(out coord))
                    {
                        Vector3D min = Vector3D.Transform((Vector3D)(sizeInMetres * coord), m_clipmap.m_worldMatrix);
                        Vector3D max = Vector3D.Transform((Vector3D)(sizeInMetres * coord + new Vector3(sizeInMetres)), m_clipmap.m_worldMatrix);

                        BoundingBoxD aabb = new BoundingBoxD(min, max);
                        MyRenderProxy.DebugDrawAABB(aabb, nearColor, 1, 1, false);
                    }


                    Vector3D center = Vector3D.Transform(m_localPosition, m_clipmap.m_worldMatrix);

                    MyRenderProxy.DebugDrawSphere(center, m_nearDistance, nearColor, 1, false);
                    MyRenderProxy.DebugDrawSphere(center, m_farDistance, farColor, 1, false);

                }

               
                //if (m_lodIndex == 1)
                {
                    float sizeInMetres = MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex);
                    Color color = LOD_COLORS[m_lodIndex] + new Vector4(0.2f);


                    foreach (var cell in m_storedCellData)
                    {
                        if (!cell.Value.InScene)
                            continue;

                        MyCellCoord cellStr = new MyCellCoord();
                        cellStr.SetUnpack(cell.Key);
                        var coordF = cellStr.CoordInLod;

                        

                        Vector3D min = Vector3D.Transform((Vector3D)(sizeInMetres * coordF), m_clipmap.m_worldMatrix);
                        Vector3D max = Vector3D.Transform((Vector3D)(sizeInMetres * coordF + new Vector3(sizeInMetres)), m_clipmap.m_worldMatrix);

                        BoundingBoxD aabb = new BoundingBoxD(min, max);
                        MyRenderProxy.DebugDrawAABB(aabb, color, 1, 1, false);

                        if (Vector3D.Distance(CameraFrustumGetter().Matrix.Translation, aabb.Center) < 200)
                            MyRenderProxy.DebugDrawText3D(aabb.Center, coordF.ToString(), color, 0.5f, false);
                    }

                    if (m_storedCellData.Count > 0)
                    {
                        Vector3D center = Vector3D.Transform(m_localPosition, m_clipmap.m_worldMatrix);
                        //MyRenderProxy.DebugDrawSphere(center, m_farDistance, color, 1, false);
                    }

                }
            }


            /// <summary>
            /// Priority function for sorting render cell precalc jobs
            /// </summary>
            /// <param name="cellWorldPos"></param>
            /// <param name="parent"></param>
            /// <param name="parentCellId"></param>
            /// <param name="cell"></param>
            /// <returns></returns>
            int PriorityFunc(BoundingBoxD cellWorldPos, LodLevel parent, ulong parentCellId, MyCellCoord cell)
            {
                //not using priority now, only physics prefetch is prioritized before graphics
                return int.MaxValue;
                //commented out since now we are not dependent on parent
                //if (parent != null)//topmost lod does not have parent
                //{
                //    //var coordCell = new MyCellCoord(); 
                //    //coordCell.SetUnpack(parentCellId);
                //    //if (!AllSiblingsWereLoaded(ref coordCell))//doesnt improve holes and slows progression awfully
                //    //    return int.MaxValue;

                //    CellData data;
                //    if (!parent.m_storedCellData.TryGetValue(parentCellId, out data) || !data.WasLoaded) //we need loaded parent for blending lods
                //        return int.MaxValue;
                //}

                //var cam = CameraMatrixGetter();//get current cam position

                ////float mult = m_lodIndex; 
                //var dir = (cellWorldPos.Center - cam.Translation); //direction to camera
                //var length = dir.Length(); 
                //var dot = (dir/length).Dot(cam.Forward); //dot with look direction

                //if (cellWorldPos.Contains(cam.Translation) != ContainmentType.Disjoint)
                //{//we are inside the cell so top priority
                //    length = 1;
                //}
                ////commented out since now we are not dependent on parent
                ////else if (AnySiblingChildrenLoaded(ref cell))
                ////    mult = 0.1f; //we should speed up since sibling child cannot be rendered without us
                //length *= 1.5f - dot; //prioritize by view direction

                //const double intMax = (double)int.MaxValue;
                //return length > intMax ? (int)intMax : (int)length;
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
                            if (!TryAddCellRequest(collector, parentLod, thisLodCell, entry.Key, data))
                                continue;
                        }

                        m_storedCellData.Add(entry.Key, data);
                    }
                    else
                    {
                        if (UseCache && data.State == CellState.Loaded)
                        {
                            var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, entry.Key);

                            CellsCache.Write(clipmapCellId, data);
                            Delete(entry.Key, data, false);
                        }
                        else
                        {
                            if (data.State == CellState.Pending)
                                collector.CancelRequest(entry.Key);
                            if (data.Cell != null)
                                Delete(entry.Key, data);
                        }

                        if (!UseCache)
                        {
                            CellsCache.Reset();
                        }
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

                    if (NEW_VOXEL_CLIPPING)
                    {
                        var siblingsLoaded = AllSiblingsWereLoaded(ref thisLodCell);
                        if (ChildrenWereLoaded(childLod, ref thisLodCell) && siblingsLoaded)
                        {
                            RemoveFromScene(entry.Key, data);
                        }
                        else
                        {
                            AddToScene(entry.Key, data);
                        }
                    }
                    else
                    {
                        if (ChildrenWereLoaded(childLod, ref thisLodCell)
                            ||
                     (MyVoxelCoordSystems.RenderCellSizeShiftToLessDetailed(thisLodCell.Lod) == 1 && !AllSiblingsWereLoaded(ref thisLodCell))
                            )
                        {
                            RemoveFromScene(entry.Key, data);
                        }
                        else
                        {
                            AddToScene(entry.Key, data);
                        }
                    }
                }
            }

            private void GetNearbyLodLevels(out LodLevel parentLod, out LodLevel childLod)
            {
                var levels = m_clipmap.m_lodLevels;

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
                        data.Cell.UpdateWorldMatrix(ref m_clipmap.m_worldMatrix, sortCellsIntoCullObjects);
                    }
                }
            }

            /// <summary>
            /// Checks ancestor nodes recursively.
            /// </summary>
            private static bool WasAncestorCellLoaded(LodLevel parentLod, ref MyCellCoord thisLodCell)
            {
                if (parentLod == null || !parentLod.m_fitsInFrustum)
                {
                    return true;
                }

                Debug.Assert(thisLodCell.Lod == parentLod.m_lodIndex - 1);

                var shiftToParent = MyVoxelCoordSystems.RenderCellSizeShiftToLessDetailed(thisLodCell.Lod);
                var parentCell = new MyCellCoord(thisLodCell.Lod + 1, thisLodCell.CoordInLod >> shiftToParent);
                CellData data;
                using (parentLod.m_storedCellDataLock.AcquireSharedUsing())
                {
                    if (parentLod.m_storedCellData.TryGetValue(parentCell.PackId64(), out data))
                    {
                        return data.WasLoaded;
                    }
                }

                LodLevel ancestor;
                if (parentLod.m_clipmap.m_lodLevels.TryGetValue(parentLod.m_lodIndex + 1, out ancestor))
                    return WasAncestorCellLoaded(ancestor, ref parentCell);
                else
                    return false;
            }

            /// <summary>
            /// Checks only immediate children (any deeper would take too long).
            /// </summary>
            private static bool ChildrenWereLoaded(LodLevel childLod, ref MyCellCoord thisLodCell)
            {
                if (childLod == null)
                    return false;

                Debug.Assert(thisLodCell.Lod == childLod.m_lodIndex + 1);

                var childLodCell = new MyCellCoord();
                childLodCell.Lod = childLod.m_lodIndex;
                var shiftToChild = MyVoxelCoordSystems.RenderCellSizeShiftToMoreDetailed(thisLodCell.Lod);
                var start = thisLodCell.CoordInLod << shiftToChild;
                var end = start + ((1 << shiftToChild) >> 1);

                Vector3I.Min(ref end, ref childLod.m_lodSizeMinusOne, out end);
                childLodCell.CoordInLod = start;
                for (var it = new Vector3I.RangeIterator(ref start, ref end);
                    it.IsValid(); it.GetNext(out childLodCell.CoordInLod))
                {
                    var key = childLodCell.PackId64();
                    CellData data;
                    using (childLod.m_storedCellDataLock.AcquireSharedUsing())
                    {
                        if (!childLod.m_storedCellData.TryGetValue(key, out data) || !data.WasLoaded)
                        {
                            return false;
                        }
                    }

                }

                return true;
            }

            private static BoundingBox GetChildrenCoords(LodLevel childLod, ref MyCellCoord thisLodCell)
            {
                if (childLod == null)
                    return BoundingBox.CreateInvalid();

                Debug.Assert(thisLodCell.Lod == childLod.m_lodIndex + 1);

                var childLodCell = new MyCellCoord();
                childLodCell.Lod = childLod.m_lodIndex;
                var shiftToChild = MyVoxelCoordSystems.RenderCellSizeShiftToMoreDetailed(thisLodCell.Lod);
                var start = thisLodCell.CoordInLod << shiftToChild;
                var end = start + ((1 << shiftToChild) >> 1);

                Vector3I.Min(ref end, ref childLod.m_lodSizeMinusOne, out end);

                return new BoundingBox(start, end);
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
                            using (m_storedCellDataLock.AcquireSharedUsing())
                            {
                                if (!m_storedCellData.TryGetValue(key, out data))
                                {
                                    return false;
                                }
                            }

                            if (!data.WasLoaded)
                            {
                                return false;
                            }
                        }

                return true;
            }
            private bool AnySiblingChildrenLoaded(ref MyCellCoord thisLodCell)
            {
                LodLevel plod, clod;
                GetNearbyLodLevels(out plod, out clod);
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
                            using (m_storedCellDataLock.AcquireSharedUsing())
                            {
                                if (!m_storedCellData.TryGetValue(key, out data) || !data.WasLoaded)
                                {
                                    continue;
                                }
                            }


                            if (ChildrenWereLoaded(clod, ref thisLodCell))
                                return true;
                        }

                return false;
            }

            private void Delete(UInt64 key, CellData data = null, bool delete = true)
            {
                data = data ?? m_storedCellData[key];
                if (data.Cell != null)
                {
                    m_nonEmptyCells.Remove(key);
                    RemoveFromScene(key, data);
                    
                    if (delete)
                        m_clipmap.m_cellHandler.DeleteCell(data.Cell);
                }
                m_storedCellData.Remove(key);
            }

            private void AddToScene(UInt64 key, CellData data = null)
            {
                data = data ?? m_storedCellData[key];
                if (!data.InScene)
                {
                    Debug.Assert(data.Cell != null);
                    m_clipmap.m_cellHandler.AddToScene(data.Cell);
                    data.InScene = true;
                }
            }

            private void RemoveFromScene(UInt64 key, CellData data = null)
            {
                data = data ?? m_storedCellData[key];
                if (data.InScene)
                {
                    Debug.Assert(data.Cell != null);
                    m_clipmap.m_cellHandler.RemoveFromScene(data.Cell);
                    data.InScene = false;
                }
            }

            /// <summary>
            /// New clipping routine, call on lowest desired lod with position of camera
            /// Should be later enhanced with spread if desired lod would be lower than 0
            /// Finaly it will be used as hint for structure managing cells to be calculated and rendered
            /// </summary>
            /// <param name="localPosition"></param>
            /// <param name="collector"></param>
            internal void DoClipping(Vector3D localPosition, RequestCollector collector, int spread)
            {
                Vector3I min, max;
                {
                    Vector3I center;
                    MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref localPosition, out center);
                    min = center - spread;
                    max = center + spread;
                    Vector3I.Clamp(ref min, ref Vector3I.Zero, ref m_lodSizeMinusOne, out min);
                    Vector3I.Clamp(ref max, ref Vector3I.Zero, ref m_lodSizeMinusOne, out max);
                }
                var it0 = new Vector3I.RangeIterator(ref min, ref max);
                var ignore = BoundingBox.CreateInvalid();

                DoClipping(collector, min, max, ref ignore);
            }


            /// <summary>
            /// Recursive clipping function requests cells in provided range and
            /// cells needed from parent to wrap the lod safely
            /// </summary>
            /// <param name="collector"></param>
            /// <param name="it0">requested range</param>
            /// <param name="ignore">inner range filled by children</param>
            private void DoClipping(RequestCollector collector, Vector3I min, Vector3I max, ref BoundingBox ignore)
            {
                LodLevel parentLod, clevel;
                GetNearbyLodLevels(out parentLod, out clevel);
                MyCellCoord cell = new MyCellCoord(m_lodIndex, Vector3I.Zero);

                //if (collector.SentRequestsEmpty)
                {
                    MyUtils.Swap(ref m_storedCellData, ref m_clippedCells);
                    m_storedCellData.Clear();
                }

                var it0 = new Vector3I.RangeIterator(ref min, ref max);
                cell.CoordInLod = it0.Current;

                var shiftToParent = MyVoxelCoordSystems.RenderCellSizeShiftToLessDetailed(cell.Lod);
                var parentCell = parentLod != null ? new MyCellCoord(parentLod.m_lodIndex, cell.CoordInLod >> shiftToParent) : cell;
                var parentIgnore = new BoundingBox(parentCell.CoordInLod, parentCell.CoordInLod);

                BoundingBox bb = new BoundingBox(cell.CoordInLod, cell.CoordInLod);
                for (; it0.IsValid(); it0.GetNext(out cell.CoordInLod)) //cells to be loaded
                {
                    if (ignore.Contains((Vector3)cell.CoordInLod) == ContainmentType.Contains)
                    {
                        continue; //lower lod requested
                    }

                    if (parentLod != null) //get also their lodcell mates
                    {
                        parentCell = new MyCellCoord(parentLod.m_lodIndex, cell.CoordInLod >> shiftToParent);
                        var it = GetChildrenCoords(this, ref parentCell);
                        bb.Include(it);
                        parentIgnore.Max = parentCell.CoordInLod;
                    }

                }
                if (parentLod != null)
                {
                    Vector3I parentMinI = Vector3I.Round(parentIgnore.Min - Vector3.One);
                    Vector3I parentMaxI = Vector3I.Round(parentIgnore.Max + Vector3.One);
                    //Vector3I.Clamp(ref parentMinI, ref Vector3I.Zero, ref m_lodSizeMinusOne, out parentMinI);
                    //Vector3I.Clamp(ref parentMaxI, ref Vector3I.Zero, ref m_lodSizeMinusOne, out parentMaxI);
                    var parentIterator = new Vector3I.RangeIterator(ref parentMinI, ref parentMaxI);
                    parentLod.DoClipping(collector, parentMinI, parentMaxI, ref parentIgnore);
                }

                Vector3I start, end;
                start = Vector3I.Round(bb.Min); end = Vector3I.Round(bb.Max);
                Vector3I.Clamp(ref start, ref Vector3I.Zero, ref m_lodSizeMinusOne, out start);
                Vector3I.Clamp(ref end, ref Vector3I.Zero, ref m_lodSizeMinusOne, out end);
                it0 = new Vector3I.RangeIterator(ref start, ref end);
                cell.CoordInLod = it0.Current;
                for (; it0.IsValid(); it0.GetNext(out cell.CoordInLod)) //cells to be loaded
                {
                    if (ignore.Contains((Vector3)cell.CoordInLod) == ContainmentType.Contains)
                    {
                        continue; //lower lod requested
                    }

                    var cellId = cell.PackId64();
                    
                    CellData data;
                    if (m_clippedCells.TryGetValue(cellId, out data))
                    {
                        m_clippedCells.Remove(cellId);
                    }
                    else
                    {
                        var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, cellId);

                        data = CellsCache.Read(clipmapCellId);

                        if (data == null) //cache miss
                        {
                            data = new CellData();
                            ClippingCacheMisses++;
                        }
                        else
                        {
                            //cache hit
                            ClippingCacheHits++;

                            data.InScene = false;
                            if (data.Cell != null)
                            {
                                m_nonEmptyCells[cellId] = data;
                            }
                        }
                    }

                    if (data.State == CellState.Invalid)
                    {
                        if (!TryAddCellRequest(collector, parentLod, cell, cellId, data))
                        {
                            continue;
                        }
                    }
                    if (!m_storedCellData.ContainsKey(cellId))
                        m_storedCellData.Add(cellId, data);
                }

            }


            float m_nearDistance;
            float m_farDistance;
            bool m_fitsInFrustum;

            BoundingBoxI m_localFarCameraBox;
            BoundingBoxI m_localNearCameraBox;
            Vector3D m_localPosition;

            internal void DoClipping_Old(Vector3D localPosition, float farPlaneDistance, RequestCollector collector)
            {
                m_localPosition = localPosition;
                MyClipmap.ComputeLodViewBounds(m_clipmap.m_scaleGroup, m_lodIndex, out m_nearDistance, out m_farDistance);

                m_fitsInFrustum = (farPlaneDistance * 1.25f) > m_nearDistance;

                if (!m_fitsInFrustum)
                    return;


                //var localFrustum = new BoundingFrustumD(CameraFrustumGetter().Matrix * m_parent.m_invWorldMatrix);
                var frustum = CameraFrustumGetter();

                Vector3I min, max;
                Vector3I ignoreMin, ignoreMax;

                var minD = m_localPosition - m_farDistance;
                var maxD = m_localPosition + m_farDistance;
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref minD, out min);
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref maxD, out max);

                BoundingBoxI lodBox = new BoundingBoxI(Vector3I.Zero, m_lodSizeMinusOne);
                bool intersects = false;
                bool intersectsNear = false;

                m_localFarCameraBox = new BoundingBoxI(min, max);
                m_localNearCameraBox = new BoundingBoxI(min, max);
                if (lodBox.Intersects(m_localFarCameraBox))
                {
                    intersects = true;
                    var intersection = lodBox.Intersect(m_localFarCameraBox);
                    min = intersection.Min;
                    max = intersection.Max;

                    //Optimize only LOD2 and higher by two lods, because neighbour cells shares border cells
                    if (m_lodIndex > 1)
                    {
                        float lowerFar, lowerNear;
                        MyClipmap.ComputeLodViewBounds(m_clipmap.m_scaleGroup, m_lodIndex - 2, out lowerFar, out lowerNear);

                        var minNear = m_localPosition - (lowerNear - MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex) / 2);
                        var maxNear = m_localPosition + (lowerNear - MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex) / 2);
                        MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref minNear, out ignoreMin);
                        MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref maxNear, out ignoreMax);

                        m_localNearCameraBox = new BoundingBoxI(ignoreMin, ignoreMax);
                        if (lodBox.Intersects(m_localNearCameraBox))
                            intersectsNear = false;
                    }
                }
            
                if (m_lastMin == min && m_lastMax == max && !m_clipmap.m_updateClipping)
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

                if (intersects)
                {
                    float sizeInMetres = MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex);

                    MyCellCoord cell = new MyCellCoord(m_lodIndex, ref min);
                    for (var it = new Vector3I.RangeIterator(ref min, ref max);
                        it.IsValid(); it.GetNext(out cell.CoordInLod))
                    {
                        if (intersectsNear &&
                            m_localNearCameraBox.Contains(cell.CoordInLod) == ContainmentType.Contains)
                            continue;

                        //if (!WasAncestorCellLoaded(parentLod, ref cell))
                        //    continue;

                        
                        Vector3D minAABB = Vector3D.Transform((Vector3D)(sizeInMetres * (cell.CoordInLod - 2)), m_clipmap.m_worldMatrix);
                        Vector3D maxAABB = Vector3D.Transform((Vector3D)(sizeInMetres * (cell.CoordInLod + 2) + new Vector3(sizeInMetres)), m_clipmap.m_worldMatrix);

                         if (frustum.Contains(new BoundingBoxD(minAABB, maxAABB)) == ContainmentType.Disjoint)
                            continue;

                        var cellId = cell.PackId64();
                        CellData data;
                        if (m_clippedCells.TryGetValue(cellId, out data))
                        {
                            m_clippedCells.Remove(cellId);
                        }
                        else
                        {
                            var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, cellId);
                            data = CellsCache.Read(clipmapCellId);

                            if (data == null) //cache miss
                            {
                                data = new CellData();
                                ClippingCacheMisses++;
                            }
                            else
                            {
                                //cache hit
                                ClippingCacheHits++;

                                //System.Diagnostics.Debug.Assert((!data.InScene && data.Cell != null) || data.Cell == null, "Not allowed cell state");
                                data.InScene = false;
                                if (data.Cell != null)
                                {
                                    m_nonEmptyCells[cellId] = data;
                                }
                            }
                        }

                        if (data.State == CellState.Invalid)
                        {
                            if (!TryAddCellRequest(collector, parentLod, cell, cellId, data))
                                continue;
                        }
                        m_storedCellData.Add(cellId, data);
                    }
                }
            }
        }
    }
}
