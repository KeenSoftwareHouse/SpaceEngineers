using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyClipmapHandler : IMyClipmapCellHandler
    {
        const int MergeLodSubdivideCount = 3;

        private readonly MyClipmap m_clipmapBase;
        internal MyClipmap Base { get { return m_clipmapBase; } }

        readonly Vector3D m_massiveCenter;
        readonly float m_massiveRadius;

        private MyLodMeshMergeHandler m_mergeHandler;

        readonly RenderFlags m_renderFlags;
        public RenderFlags RenderFlags { get { return m_renderFlags; } }

        internal MyClipmapHandler(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, Vector3D massiveCenter, float massiveRadius, bool spherize, RenderFlags additionalFlags, VRage.Voxels.MyClipmap.PruningFunc prunningFunc)
        {
            m_clipmapBase = new MyClipmap(id, scaleGroup, worldMatrix, sizeLod0, this, massiveCenter, massiveRadius, prunningFunc);
            m_massiveCenter = massiveCenter;
            m_renderFlags = additionalFlags;
            m_mergeHandler = null;

            if (spherize)
                m_massiveRadius = massiveRadius;

            if (MyLodMeshMergeHandler.ShouldAllocate(m_mergeHandler))
                m_mergeHandler = AllocateMergeHandler();

            MyClipmap.AddToUpdate(MyRender11.Environment.CameraPosition, Base);
        }

        private MyLodMeshMergeHandler AllocateMergeHandler()
        {
            MyLodMeshMergeHandler mergeHandler;
            MatrixD worldMatrix = Base.WorldMatrix;
            Vector3D massiveCenter = m_massiveCenter;
            mergeHandler = new MyLodMeshMergeHandler(Base, MyCellCoord.MAX_LOD_COUNT, MergeLodSubdivideCount, ref worldMatrix, ref massiveCenter, m_massiveRadius, m_renderFlags);
            return mergeHandler;
        }

        public IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref MatrixD worldMatrix)
        {
            var cell = new MyClipmapCellProxy(cellCoord, ref worldMatrix, m_massiveCenter, m_massiveRadius, m_renderFlags);
            cell.SetVisibility(false);
            cell.ScaleGroup = scaleGroup;
            return cell;
        }

        public void UpdateMesh(IMyClipmapCell cell, MyRenderMessageUpdateClipmapCell msg)
        {
            cell.UpdateMesh(msg);
        }

        public void UpdateMerging()
        {
            if(m_mergeHandler != null)
                m_mergeHandler.Update();
        }

        internal void UpdateMergedMesh(MyRenderMessageUpdateMergedVoxelMesh msg)
        {
            ProfilerShort.Begin("MyClipmapHandler.UpdateMergedMesh");

            if(m_mergeHandler != null)
                m_mergeHandler.UpdateMesh(msg);

            ProfilerShort.End();
        }

        internal void ResetMergedMeshes()
        {
            if (MyLodMeshMergeHandler.ShouldAllocate(m_mergeHandler))
                m_mergeHandler = AllocateMergeHandler();

            if(m_mergeHandler != null)
                m_mergeHandler.ResetMeshes();
        }

        internal void UpdateWorldMatrix(ref MatrixD worldMatrix)
        {
            Base.UpdateWorldMatrix(ref worldMatrix, true);
        }

        public void AddToScene(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Adding wrong type of clipmap cell to scene!");
            if (cellProxy != null)
            {
                cellProxy.SetVisibility(true);

                AddToMergeBatch(cellProxy);
            }
        }

        public void RemoveFromScene(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Removing wrong type of clipmap cell from scene!");
            cellProxy.SetVisibility(false);

            if(m_mergeHandler != null)
                m_mergeHandler.OnRemovedFromScene(cellProxy);
        }

        public void DeleteCell(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Deleting wrong type of clipmap cell!");

            if (cellProxy.Actor != null)
			{
            	if(m_mergeHandler == null || !m_mergeHandler.OnDeleteCell(cellProxy))
                	cellProxy.Unload();
			}
        }

        public void AddToMergeBatch(IMyClipmapCell cell)
        {
            var cellProxy = (MyClipmapCellProxy)cell;
            if (m_mergeHandler != null)
                m_mergeHandler.OnAddedToScene(cellProxy);
        }

        internal static void UpdateQueued()
        {
            if (!MyRender11.Settings.FreezeTerrainQueries)
            {
                MyClipmap.UpdateQueued(MyRender11.Environment.CameraPosition, MyRender11.Environment.InvView.Forward, MyRender11.Environment.FarClipping, MyRender11.Environment.LargeDistanceFarClipping);
            }
        }

        internal void RemoveFromUpdate()
        {
            Base.UnloadContent();
            MyClipmap.RemoveFromUpdate(Base);
        }

        float IMyClipmapCellHandler.GetTime()
        {
            return (float)MyRender11.CurrentDrawTime.Seconds;
        }

        void IMyClipmapCellHandler.DebugDrawMergedCells()
        {
            m_mergeHandler.DebugDrawCells();
        }
    }
}
