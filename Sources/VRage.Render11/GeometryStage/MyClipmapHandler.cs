using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyClipmapHandler : IMyClipmapCellHandler
    {
        private readonly MyClipmap m_clipmapBase;
        internal MyClipmap Base { get { return m_clipmapBase; } }

        readonly Vector3D m_massiveCenter;
        readonly float m_massiveRadius;

        private readonly MyLodMeshMergeHandler m_mergeHandler;

        readonly RenderFlags m_renderFlags;
        public RenderFlags RenderFlags { get { return m_renderFlags; } }

        internal MyClipmapHandler(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, Vector3D massiveCenter, float massiveRadius, bool spherize, RenderFlags additionalFlags, VRage.Voxels.MyClipmap.PruningFunc prunningFunc)
        {
            m_clipmapBase = new MyClipmap(id, scaleGroup, worldMatrix, sizeLod0, this, massiveCenter, massiveRadius, prunningFunc);
            m_massiveCenter = massiveCenter;
            m_renderFlags = additionalFlags;

            if (spherize)
                m_massiveRadius = massiveRadius;

            const int mergeLodSubdivideCount = 3;
            m_mergeHandler = new MyLodMeshMergeHandler(Base, MyCellCoord.MAX_LOD_COUNT, mergeLodSubdivideCount, ref worldMatrix, ref massiveCenter, massiveRadius, m_renderFlags);

            MyClipmap.AddToUpdate(MyEnvironment.CameraPosition, Base);
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
            m_mergeHandler.Update();
        }

        internal void UpdateMergedMesh(MyRenderMessageUpdateMergedVoxelMesh msg)
        {
            m_mergeHandler.UpdateMesh(msg);
        }

        internal void ResetMergedMeshes()
        {
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

                m_mergeHandler.OnAddedToScene(cellProxy);
            }
        }

        public void RemoveFromScene(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Removing wrong type of clipmap cell from scene!");
            cellProxy.SetVisibility(false);

            m_mergeHandler.OnRemovedFromScene(cellProxy);
        }

        public void DeleteCell(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Deleting wrong type of clipmap cell!");

            if (cellProxy.Actor != null)
			{
            	if(!m_mergeHandler.OnDeleteCell(cellProxy))
                	cellProxy.Unload();
			}
        }

        internal static void UpdateQueued()
        {
            if (!MyRender11.Settings.FreezeTerrainQueries)
            {
                MyClipmap.UpdateQueued(MyEnvironment.CameraPosition, MyEnvironment.InvView.Forward, MyEnvironment.FarClipping, MyEnvironment.LargeDistanceFarClipping);
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
