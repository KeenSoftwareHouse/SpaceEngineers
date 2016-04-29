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
	public class MyClipmap_CellData
	{
		public MyClipmap.CellState State;
		public IMyClipmapCell Cell;
		public IMyClipmapCellHandler CellHandler;
		public bool InScene;
		public bool WasLoaded;
		public bool ReadyInClipmap;
		public bool DeleteAfterRemove;
		public bool ClippedOut;
		public bool HighPriority;

		public int GetPriority()
		{
			return HighPriority ? int.MaxValue : 0;
		}
	}

    public partial class MyClipmap
    {
        public enum CellState
        {
            Invalid,
            Pending,
            Loaded
        }


        class LodLevel
        {
            private Dictionary<UInt64, MyClipmap_CellData> m_storedCellData = new Dictionary<UInt64, MyClipmap_CellData>();
            private Dictionary<UInt64, MyClipmap_CellData> m_nonEmptyCells = new Dictionary<UInt64, MyClipmap_CellData>();
            private Dictionary<UInt64, MyClipmap_CellData> m_clippedCells = new Dictionary<UInt64, MyClipmap_CellData>();

            enum BlendState
            {
                Adding,
                Removing
            }

            struct CellBlendData
            {
                public MyClipmap_CellData CellData;
                public float TimeAdded; //seconds
                public BlendState State;
                public bool UndoAfterFinish;
            }

            static float CellsDitherTime = 1.5f;
            private Dictionary<UInt64, CellBlendData> m_blendedCells = new Dictionary<UInt64, CellBlendData>();
            List<UInt64> m_cellsToDelete = new List<UInt64>();
            List<UInt64> m_cellsToOpposite = new List<UInt64>();


            private int m_lodIndex;
            private Vector3I m_lodSizeMinusOne;
            private MyClipmap m_clipmap;

            private List<Vector3I> m_outsideCells = new List<Vector3I>(1024);
            float m_nearDistance;
            float m_farDistance;
            bool m_fitsInFrustum;

            BoundingBoxI m_localFarCameraBox;
            BoundingBoxI m_localNearCameraBox;
            Vector3D m_localPosition;

            static float VisibleDelay = 25; //seconds
            float m_lastVisibleCounter; //seconds

            internal Vector3I LodSizeMinusOne { get { return m_lodSizeMinusOne; } }

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
                m_blendedCells.Clear();
            }

            internal void InvalidateRange(Vector3I lodMin, Vector3I lodMax)
            {
  //              MyLog.Default.WriteLine("InvalidateRange Lod: " + m_lodIndex + " Min: " + lodMin + " Max: " + lodMax);

                var cell = new MyCellCoord(m_lodIndex, lodMin);
                for (var it = new Vector3I_RangeIterator(ref lodMin, ref lodMax);
                    it.IsValid(); it.GetNext(out cell.CoordInLod))
                {
                    MyClipmap_CellData data;
                    var id = cell.PackId64();
//                    MyLog.Default.WriteLine("Setting to: m_lodIndex " + cell.Lod + " Coord: " + cell.CoordInLod);


                    if (m_storedCellData.TryGetValue(id, out data))
                    {
                        data.State = CellState.Invalid;
                        //MyLog.Default.WriteLine("Really set to: m_lodIndex " + cell.Lod + " Coord: " + cell.CoordInLod);
                    }

                    if (MyClipmap.UseCache)
                    {
                        var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, id);
                        var cachedCell = MyClipmap.CellsCache.Read(clipmapCellId);
                        if (cachedCell != null)
                        {
                            cachedCell.State = CellState.Invalid;
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
                MyClipmap_CellData data;
                var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, cellId);

              //  MyCellCoord cellc = new MyCellCoord();
              //  cellc.SetUnpack(cellId);

                //MyLog.Default.WriteLine("SetCellMesh Lod: " + cellc.Lod + " Coord: " + cellc.CoordInLod);

                if (m_storedCellData.TryGetValue(cellId, out data))
                {
                    PendingCacheCellData.Remove(clipmapCellId);

                    if (data.State == CellState.Invalid)
                    {
//                        MyLog.Default.WriteLine("Invalid");
                        //Cell was invalidated while calculating from old data
                        return;
                    }

                    if (data.Cell == null && msg.Batches.Count != 0)
                    {
                        //MyLog.Default.WriteLine("added to nonempty");
                        data.Cell = m_clipmap.m_cellHandler.CreateCell(m_clipmap.m_scaleGroup, msg.Metadata.Cell, ref m_clipmap.m_worldMatrix);
                        System.Diagnostics.Debug.Assert(data.Cell != null, "Cell not created");
                        if (data.Cell != null)
                        {
                            if (data.Cell.IsValid())
                            {
                                data.CellHandler = m_clipmap.m_cellHandler;
                                m_nonEmptyCells[cellId] = data;
                            }
                        }
                    }
                    else if (data.Cell != null && msg.Batches.Count == 0)
                    {
                        //MyLog.Default.WriteLine("removed");
                        RemoveFromScene(cellId, data);
                        m_nonEmptyCells.Remove(cellId);
                        m_clipmap.m_cellHandler.DeleteCell(data.Cell);
                        m_blendedCells.Remove(cellId);
                        data.Cell = null;
                        data.CellHandler = null;
                        if (UseCache)
                            CellsCache.Remove(cellId);
                    }

                    if (data.Cell != null)
                    {
                        //MyLog.Default.WriteLine("mesh updated");
                        if (data.Cell.IsValid())
                        {
                            m_clipmap.m_cellHandler.UpdateMesh(data.Cell, msg);
                        }
                    }
                    data.State = CellState.Loaded;
                    data.WasLoaded = true;
                }
                else
                if (PendingCacheCellData.TryGetValue(clipmapCellId, out data))
                {
                    if (msg.Batches.Count != 0)
                    {
                        data.Cell = m_clipmap.m_cellHandler.CreateCell(m_clipmap.m_scaleGroup, msg.Metadata.Cell, ref m_clipmap.m_worldMatrix);
                        m_clipmap.m_cellHandler.UpdateMesh(data.Cell, msg);
                        data.CellHandler = m_clipmap.m_cellHandler;
                    }
                    
                    CellsCache.Write(clipmapCellId, data);
                    PendingCacheCellData.Remove(clipmapCellId);

                    data.State = CellState.Loaded;
                    data.WasLoaded = true;
                }
            }

            public void DebugDraw()
            {
                //if (m_lodIndex > 5)
                //    return;


    //            if (m_lodIndex == 1)
    //            {
    //                float sizeInMetres = MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex);

    //                //var start = localFarCameraBox.Min;
    //                //var end = localFarCameraBox.Max;
    //                var start = m_localNearCameraBox.Min;
    //                var end = m_localNearCameraBox.Max;
    //                Vector3I coord = start;

    //                Color nearColor = Color.Yellow;
    //                Color farColor = Color.White;

    //                var startF = m_localFarCameraBox.Min;
    //                var endF = m_localFarCameraBox.Max;
    //                Vector3I coordF = startF;

    ////                for (var it = new Vector3I_RangeIterator(ref startF, ref endF);
    ////it.IsValid(); it.GetNext(out coordF))
    ////                {
    ////                    Vector3D min = Vector3D.Transform((Vector3D)(sizeInMetres * coordF), m_parent.m_worldMatrix);
    ////                    Vector3D max = Vector3D.Transform((Vector3D)(sizeInMetres * coordF + new Vector3(sizeInMetres)), m_parent.m_worldMatrix);

    ////                    BoundingBoxD aabb = new BoundingBoxD(min, max);
    ////                    MyRenderProxy.DebugDrawAABB(aabb, farColor, 1, 1, false);

    ////                    if (Vector3D.Distance(CameraFrustumGetter().Matrix.Translation, aabb.Center) < 200)
    ////                        MyRenderProxy.DebugDrawText3D(aabb.Center, coordF.ToString(), farColor, 0.5f, false);
    ////                }


    //                for (var it = new Vector3I_RangeIterator(ref start, ref end);
    //it.IsValid(); it.GetNext(out coord))
    //                {
    //                    Vector3D min = Vector3D.Transform((Vector3D)(sizeInMetres * coord), m_clipmap.m_worldMatrix);
    //                    Vector3D max = Vector3D.Transform((Vector3D)(sizeInMetres * coord + new Vector3(sizeInMetres)), m_clipmap.m_worldMatrix);

    //                    BoundingBoxD aabb = new BoundingBoxD(min, max);
    //                    MyRenderProxy.DebugDrawAABB(aabb, nearColor, 1, 1, false);
    //                }


    //                Vector3D center = Vector3D.Transform(m_localPosition, m_clipmap.m_worldMatrix);

    //                MyRenderProxy.DebugDrawSphere(center, m_nearDistance, nearColor, 1, false);
    //                MyRenderProxy.DebugDrawSphere(center, m_farDistance, farColor, 1, false);

    //            }

                var camera = m_clipmap.LastCameraPosition;

                //if (m_lodIndex < 6)
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
                        double distance = Vector3D.Distance(camera, aabb.Center);
                        //if (distance < sizeInMetres * 4)
                            MyRenderProxy.DebugDrawAABB(aabb, color, 1, 1, true);
                        if (distance < sizeInMetres * 2)
                            MyRenderProxy.DebugDrawText3D(aabb.Center, String.Format("{0}:{1}", m_lodIndex, coordF.ToString()), color, 0.7f, false);
                    }

                    if (m_storedCellData.Count > 0)
                    {
                        Vector3D center = Vector3D.Transform(m_localPosition, m_clipmap.m_worldMatrix);
                        //MyRenderProxy.DebugDrawSphere(center, m_farDistance, color, 1, false);
                    }

                }
            }


           
            internal void DiscardClippedCells(RequestCollector collector)
            {
                foreach (var entry in m_clippedCells)
                {
                    var data = entry.Value;

                    data.ClippedOut = true;

                    if (UseCache)
                    {
                        var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, entry.Key);

                        CellsCache.Write(clipmapCellId, data);
                        Delete(entry.Key, data, false);
                    }
                    else
                    {
                        if (data.Cell != null)
                            Delete(entry.Key, data);

                        data.ReadyInClipmap = false;
                    }
                }

                m_clippedCells.Clear();
            }

            bool ShouldBeThisLodVisible(float camDistanceFromCenter)
            {
                if (!MyClipmap.UseLodCut)
                    return true;

                //4000m = lod 4
                //60000m = lod 8
                int lodClipmapCut = (int)Math.Ceiling(m_clipmap.m_massiveRadius / 14000.0f + 3.7f);

                float heightMultiplier = 1.1f;
                if (MyRenderProxy.Settings.VoxelQuality == MyRenderQualityEnum.NORMAL)
                {
                    lodClipmapCut--;
                    heightMultiplier = 1.05f;
                }

                if (MyRenderProxy.Settings.VoxelQuality == MyRenderQualityEnum.LOW)
                {
                    lodClipmapCut -= 1;
                    heightMultiplier = 1.05f;
                }


                //4000m = 1.05f;
                //60000m = 1.2f
                //float radiusMultiplier = m_clipmap.m_massiveRadius / 373333f + 1.04f;

                if (lodClipmapCut > 0 && camDistanceFromCenter < (m_clipmap.m_massiveRadius * heightMultiplier))
                {
                    if (m_lodIndex > lodClipmapCut)
                    {
                        if ((m_clipmap.m_cellHandler.GetTime() - m_lastVisibleCounter) > VisibleDelay)
                            return false;
                        else
                            return true;
                    }
                }

                m_lastVisibleCounter = m_clipmap.m_cellHandler.GetTime();
                return true;
            }

            internal void UpdateCellsInScene(float cameraDistance, Vector3D localPosition)
            {
                LodLevel parentLod, childLod;
                GetNearbyLodLevels(out parentLod, out childLod);

                MyCellCoord thisLodCell = new MyCellCoord();
                foreach (var entry in m_nonEmptyCells)
                {
                    var data = entry.Value;
                    Debug.Assert(data.Cell != null, "Empty cell in m_nonEmptyCells!");
                    if (data.Cell != null)
                    {
                        thisLodCell.SetUnpack(entry.Key);

                        if (ChildrenWereLoaded(childLod, ref thisLodCell)
                            ||
                        (MyVoxelCoordSystems.RenderCellSizeShiftToLessDetailed(thisLodCell.Lod) == 1 && !AllSiblingsWereLoaded(ref thisLodCell))
                            ||
                            !ShouldBeThisLodVisible(cameraDistance)
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
                MyClipmap_CellData data;
                if (parentLod.m_storedCellData.TryGetValue(parentCell.PackId64(), out data))
                {
                    return data.WasLoaded;
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

                Vector3I.Max(ref childLod.m_lodSizeMinusOne, ref Vector3I.Zero, out childLod.m_lodSizeMinusOne);
                Vector3I.Min(ref end, ref childLod.m_lodSizeMinusOne, out end);
                childLodCell.CoordInLod = start;
                for (var it = new Vector3I_RangeIterator(ref start, ref end);
                    it.IsValid(); it.GetNext(out childLodCell.CoordInLod))
                {
                    var key = childLodCell.PackId64();
                    MyClipmap_CellData data;
                    if (!childLod.m_storedCellData.TryGetValue(key, out data) || !data.WasLoaded)
                    {
                        return false;
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

                return new VRageMath.BoundingBox(start, end);
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
                            MyClipmap_CellData data;
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
                            MyClipmap_CellData data;
                            if (!m_storedCellData.TryGetValue(key, out data) || !data.WasLoaded)
                            {
                                continue;
                            }

                            if (ChildrenWereLoaded(clod, ref thisLodCell))
                                return true;
                        }

                return false;
            }

            private void Delete(UInt64 key, MyClipmap_CellData data = null, bool del = true)
            {
                data = data ?? m_storedCellData[key];
                if (data.Cell != null)
                {
                    if (UseDithering && data.InScene)
                    {
                        RemoveFromScene(key, data);
                        if (del )
                            data.DeleteAfterRemove = true;

                        return;
                    }

                    m_nonEmptyCells.Remove(key);
                    RemoveFromScene(key, data);

                    if (del )
                    {
                        m_clipmap.m_cellHandler.DeleteCell(data.Cell);
                    }
                }
                m_storedCellData.Remove(key);

                data.ReadyInClipmap = false;
            }

            private void AddToScene(UInt64 key, MyClipmap_CellData data = null)
            {
               // data = data ?? m_storedCellData[key];
                CellBlendData blendData;

                bool inScene = data.InScene;
                if (!data.InScene && !data.ClippedOut)
                {
                    Debug.Assert(data.Cell != null, "Adding null cell");
                    if (data.Cell != null)
                    {
                        //System.Diagnostics.Debug.Assert(data.Cell.IsValid(), "Invalid cell!");
                        if (data.Cell.IsValid())
                        {
                            m_clipmap.m_cellHandler.AddToScene(data.Cell);
                            data.Cell.SetDithering(0);
                            data.InScene = true;
                        }
                    }
                }

                if (MyClipmap.UseDithering)
                {
                    if (!m_blendedCells.TryGetValue(key, out blendData))
                    {
                        if (!inScene && data.InScene)
                        {
                            blendData = new CellBlendData();
                            blendData.CellData = data;
                            blendData.TimeAdded = m_clipmap.m_cellHandler.GetTime();
                            blendData.State = BlendState.Adding;
                            m_blendedCells.Add(key, blendData);
                            data.Cell.SetDithering(2);
                        }
                    }
                    else
                    {
                     //   System.Diagnostics.Debug.Assert(inScene, "We are blending something what was not in scene");

                        if (inScene)
                        {
                            if (!data.ClippedOut)
                            {
                                if (blendData.State == BlendState.Removing)
                                {
                                    blendData.State = BlendState.Adding;
                                    blendData.UndoAfterFinish = false;
                                    blendData.CellData.DeleteAfterRemove = false;

                                    float newEndTime = m_clipmap.m_cellHandler.GetTime() - blendData.TimeAdded;

                                    blendData.TimeAdded = m_clipmap.m_cellHandler.GetTime() - CellsDitherTime;

                                }
                                else
                                    blendData.UndoAfterFinish = false;
                            }

                            m_blendedCells[key] = blendData;
                        }
                    }
                }
                           
            }

            private void RemoveFromScene(UInt64 key, MyClipmap_CellData data = null)
            {
               // data = data ?? m_storedCellData[key];

                if (data.InScene)
                {
                    Debug.Assert(data.Cell != null);

                    if (MyClipmap.UseDithering)
                    {
                        CellBlendData blendData;

                        if (!m_blendedCells.TryGetValue(key, out blendData))
                        {
                            blendData = new CellBlendData();
                            blendData.CellData = data;
                            System.Diagnostics.Debug.Assert(blendData.CellData.Cell.IsValid(), "Invalid cell!");
                            blendData.TimeAdded = m_clipmap.m_cellHandler.GetTime();
                            blendData.State = BlendState.Removing;
                            m_blendedCells.Add(key, blendData);
                            data.Cell.SetDithering(0);
                        }
                        else
                        {
                            if (blendData.State == BlendState.Adding)
                                blendData.UndoAfterFinish = true;
                            else
                            {
                                blendData.UndoAfterFinish = false;
                                //blendData.CellData.DeleteAfterRemove = false;
                            }

                            System.Diagnostics.Debug.Assert(blendData.CellData.Cell.IsValid(), "Invalid cell!");
                            m_blendedCells[key] = blendData;
                        }
                    }
                    else
                    {
                        m_clipmap.m_cellHandler.RemoveFromScene(data.Cell);
                        data.InScene = false;
                    }
                }
            }

            public void UpdateDithering()
            {
                float frameTime = m_clipmap.m_cellHandler.GetTime();

                foreach (var cellBlend in m_blendedCells)
                {
                    if (cellBlend.Value.State == BlendState.Adding)
                    {
                        if ((frameTime - cellBlend.Value.TimeAdded) > CellsDitherTime)
                        {
                            cellBlend.Value.CellData.Cell.SetDithering(0);

                            if (cellBlend.Value.UndoAfterFinish)
                                m_cellsToOpposite.Add(cellBlend.Key);
                            else
                                m_cellsToDelete.Add(cellBlend.Key);                            
                        }
                        else
                        {
                            float dither = 1 - ((frameTime - cellBlend.Value.TimeAdded) / CellsDitherTime);
                            cellBlend.Value.CellData.Cell.SetDithering(2 + 2 * dither);
                            //cellBlend.Value.MyClipmap_CellData.Cell.SetDithering(0);
                        }
                    }

                    if (cellBlend.Value.State == BlendState.Removing)
                    {
                        if (frameTime - cellBlend.Value.TimeAdded > CellsDitherTime)
                        {
                            cellBlend.Value.CellData.Cell.SetDithering(2); //because of cache

                            if (cellBlend.Value.UndoAfterFinish)
                                m_cellsToOpposite.Add(cellBlend.Key);
                            else
                                m_cellsToDelete.Add(cellBlend.Key);
                        }
                        else
                        {
                            float dither = (frameTime - cellBlend.Value.TimeAdded) / CellsDitherTime;
                            //System.Diagnostics.Debug.Assert(dither <= 1 && dither >= 0, "Invalid dither");
                            cellBlend.Value.CellData.Cell.SetDithering(2 * dither);

                            //cellBlend.Value.MyClipmap_CellData.Cell.SetDithering(0);
                        }
                    }
                }

                foreach (var cellId in m_cellsToOpposite)
                {
                    System.Diagnostics.Debug.Assert(!m_cellsToDelete.Contains(cellId), "Deleting wrong cell");

                    var cell = m_blendedCells[cellId];
                    cell.State = cell.State == BlendState.Adding ? BlendState.Removing : BlendState.Adding;
                    cell.UndoAfterFinish = false;
                    cell.TimeAdded = frameTime;
                    m_blendedCells[cellId] = cell;
                }
                m_cellsToOpposite.Clear();

                foreach (var cellId in m_cellsToDelete)
                {
                    var cell = m_blendedCells[cellId];
                    m_blendedCells.Remove(cellId);

                    if (cell.State == BlendState.Removing)
                    {
                        m_clipmap.m_cellHandler.RemoveFromScene(cell.CellData.Cell);
                        cell.CellData.InScene = false;
                        cell.CellData.ReadyInClipmap = false;

                        if (cell.CellData.DeleteAfterRemove)
                        {
                            m_clipmap.m_cellHandler.DeleteCell(cell.CellData.Cell);
                            m_storedCellData.Remove(cellId);
                            m_nonEmptyCells.Remove(cellId);
                        }
                    }
                }
                m_cellsToDelete.Clear();
            }

            public bool IsDitheringInProgress()
            {
                return m_blendedCells.Count > 0;
            }


            internal void DoClipping(float camDistanceFromCenter, Vector3D localPosition, float farPlaneDistance, RequestCollector collector, bool frustumCulling, float rangeScale)
            {
                int lodIndex = m_lodIndex;

                if (!ShouldBeThisLodVisible(camDistanceFromCenter))
                {
                    MyUtils.Swap(ref m_storedCellData, ref m_clippedCells);
                    m_storedCellData.Clear();
                    return;
                }


                m_localPosition = localPosition;
                MyClipmap.ComputeLodViewBounds(m_clipmap.m_scaleGroup, lodIndex, out m_nearDistance, out m_farDistance);


                farPlaneDistance *= rangeScale;
                m_farDistance *= rangeScale;
                m_nearDistance *= rangeScale;

                m_fitsInFrustum = (farPlaneDistance * 1.25f) > m_nearDistance;

                if (!m_fitsInFrustum && m_lodIndex == lodIndex)
                    return;


                //var localFrustum = new BoundingFrustumD(CameraFrustumGetter().Matrix * m_parent.m_invWorldMatrix);
                var frustum = CameraFrustumGetter();

                Vector3I min, max;
                // Vector3I ignoreMin, ignoreMax;

                var minD = m_localPosition - (double)m_farDistance;
                var maxD = m_localPosition + (double)m_farDistance;
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(lodIndex, ref minD, out min);
                MyVoxelCoordSystems.LocalPositionToRenderCellCoord(lodIndex, ref maxD, out max);

                BoundingBoxI lodBox = new BoundingBoxI(Vector3I.Zero, Vector3I.Max(m_lodSizeMinusOne, Vector3I.Zero));
                bool intersects = false;
                //bool intersectsNear = false;

                m_localFarCameraBox = new BoundingBoxI(min, max);
                m_localNearCameraBox = new BoundingBoxI(min, max);
                if (lodBox.Intersects(m_localFarCameraBox))
                {
                    intersects = true;
                    var intersection = lodBox;
                    intersection.IntersectWith(ref m_localFarCameraBox);
                    min = intersection.Min;
                    max = intersection.Max;

                    //Optimize only LOD2 and higher by two lods, because neighbour cells shares border cells
                    //if (m_lodIndex > 1)
                    //{
                    //    float lowerFar, lowerNear;
                    //    MyClipmap.ComputeLodViewBounds(m_clipmap.m_scaleGroup, m_lodIndex - 2, out lowerFar, out lowerNear);

                    //    var minNear = m_localPosition - (lowerNear - MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex) / 2);
                    //    var maxNear = m_localPosition + (lowerNear - MyVoxelCoordSystems.RenderCellSizeInMeters(m_lodIndex) / 2);
                    //    MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref minNear, out ignoreMin);
                    //    MyVoxelCoordSystems.LocalPositionToRenderCellCoord(m_lodIndex, ref maxNear, out ignoreMax);

                    //    m_localNearCameraBox = new BoundingBoxI(ignoreMin, ignoreMax);
                    //    if (lodBox.Intersects(m_localNearCameraBox))
                    //        intersectsNear = false;
                    //}
                }
            
                //if (m_lastMin == min && m_lastMax == max && !m_clipmap.m_updateClipping)
                //    return;

                //m_lastMin = min;
                //m_lastMax = max;

                //LodLevel parentLod, childLod;
                //GetNearbyLodLevels(out parentLod, out childLod);

                // Moves cells which are still needed from one collection to another.
                // All that is left behind is unloaded as no longer needed.

                // Move everything in range to collection of next stored cells.

                if (frustumCulling)
                {
                    MyUtils.Swap(ref m_storedCellData, ref m_clippedCells);
                    m_storedCellData.Clear();
                }

                if (intersects)
                {
                    float sizeInMetres = MyVoxelCoordSystems.RenderCellSizeInMeters(lodIndex);


                    MyCellCoord cell = new MyCellCoord(lodIndex, ref min);

                    for (var it = new Vector3I_RangeIterator(ref min, ref max);
                        it.IsValid(); it.GetNext(out cell.CoordInLod))
                    {
                        //if (intersectsNear &&
                        //    m_localNearCameraBox.Contains(cell.CoordInLod) == ContainmentType.Contains)
                        //    continue;

                        //if (frustumCulling)
                        //{
                        //    Vector3D minAABB = Vector3D.Transform((Vector3D)(sizeInMetres * (cell.CoordInLod - 2)), m_clipmap.m_worldMatrix);
                        //    Vector3D maxAABB = Vector3D.Transform((Vector3D)(sizeInMetres * (cell.CoordInLod + 2) + new Vector3(sizeInMetres)), m_clipmap.m_worldMatrix);

                        //    if (frustum.Contains(new BoundingBoxD(minAABB, maxAABB)) == ContainmentType.Disjoint)
                        //    {
                        //        m_outsideCells.Add(cell.CoordInLod);
                        //        continue;
                        //    }
                        //}

                        UnclipCell(collector, cell, true);
                    }

                    //cache cells around frustum
                    if (collector.SentRequestsEmpty)
                    {
                          foreach (var outsideCell in m_outsideCells)
                          {
                              cell.CoordInLod = outsideCell;
                              UnclipCell(collector, cell, frustumCulling);
                          }
                    }

                    m_outsideCells.Clear();
                }
            }

            private void UnclipCell(RequestCollector collector, MyCellCoord cell, bool isVisible)
            {
                var cellId = cell.PackId64();
                var clipmapCellId = MyCellCoord.GetClipmapCellHash(m_clipmap.Id, cellId);
                MyClipmap_CellData data;

                if (isVisible)
                {
                    bool highPriority = true;

                    if (m_clippedCells.TryGetValue(cellId, out data))
                    {
                        m_clippedCells.Remove(cellId);
                    }
                    else
                    {
                        highPriority = false;

                        CellBlendData blendData;
                        if (!m_blendedCells.TryGetValue(cellId, out blendData))
                        {
                            data = CellsCache.Read(clipmapCellId);

                            if (data == null) //cache miss
                            {
                                data = new MyClipmap_CellData();
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
                        else
                        {
                            data = blendData.CellData;
                            if (blendData.State == BlendState.Removing)
                                blendData.UndoAfterFinish = true;
                            if (data.Cell != null)
                            {
                                m_nonEmptyCells[cellId] = data;
                            }
                        }
                    }

                    if (data.State == CellState.Invalid)
                    {
                        if (MyClipmap.UseQueries)
                        {
                            BoundingBoxD bbd;
                            MyVoxelCoordSystems.RenderCellCoordToLocalAABB(ref cell, out bbd);
                            BoundingBox bb = new BoundingBox(bbd);
                            if (m_clipmap.m_prunningFunc == null || m_clipmap.m_prunningFunc(ref bb, false) == ContainmentType.Intersects)
                            {
                                collector.AddRequest(cellId, data, highPriority);
                            }
                            else
                            {
                                data.State = CellState.Loaded;
                                data.WasLoaded = true;
                            }
                        }
                        else
                          collector.AddRequest(cellId, data, highPriority);
                    }
             
                    m_storedCellData.Add(cellId, data);
                    data.ReadyInClipmap = true;
                    data.ClippedOut = false;
                }
                else
                {
                    if (!m_storedCellData.ContainsKey(cellId) && (!PendingCacheCellData.ContainsKey(clipmapCellId) || PendingCacheCellData[clipmapCellId].State == CellState.Invalid) && CellsCache.Read(clipmapCellId) == null)
                    {
                        if (!PendingCacheCellData.TryGetValue(clipmapCellId, out data))
                        {
                            data = new MyClipmap_CellData();
                            PendingCacheCellData.Add(clipmapCellId, data);
                        }

                        if (MyClipmap.UseQueries)
                        {
                            BoundingBoxD bbd;
                            MyVoxelCoordSystems.RenderCellCoordToLocalAABB(ref cell, out bbd);
                            BoundingBox bb = new BoundingBox(bbd);
                            if (m_clipmap.m_prunningFunc == null || m_clipmap.m_prunningFunc(ref bb, false) == ContainmentType.Intersects)
                            {
                                data.State = CellState.Invalid;

                                collector.AddRequest(cellId, data, false);
                            }
                            else
                            {
                                data.State = CellState.Loaded;
                                data.WasLoaded = true;
                            }
                        }
                        else
                        {
                            data.State = CellState.Invalid;
                            collector.AddRequest(cellId, data, false);
                        }
                    }
                }
            }

            internal void RequestMergeAll()
            {
                foreach (var cellData in m_nonEmptyCells.Values)
                {
                    if(cellData.InScene)
                        cellData.CellHandler.AddToMergeBatch(cellData.Cell);
                }
            }
        }
    }
}
