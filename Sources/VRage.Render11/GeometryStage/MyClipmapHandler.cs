using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;
using VRageRender.Messages;
using VRageRender.Voxels;

namespace VRageRender
{
    class MyClipmapHandler : IMyClipmapCellHandler
    {
        const int MergeLodSubdivideCount = 3;

        private readonly MyClipmap m_clipmapBase;
        internal MyClipmap Base { get { return m_clipmapBase; } }

        readonly Vector3D m_massiveCenter;
        readonly float m_massiveRadius;

        readonly RenderFlags m_renderFlags;
        public RenderFlags RenderFlags { get { return m_renderFlags; } }

        internal MyClipmapHandler(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, Vector3D massiveCenter, float massiveRadius, bool spherize, RenderFlags additionalFlags, MyClipmap.PruningFunc prunningFunc)
        {
            m_clipmapBase = new MyClipmap(id, scaleGroup, worldMatrix, sizeLod0, this, massiveCenter, massiveRadius, prunningFunc);
            m_massiveCenter = massiveCenter;
            m_renderFlags = additionalFlags;

            if (spherize)
                m_massiveRadius = massiveRadius;

            MyClipmap.AddToUpdate(MyRender11.Environment.Matrices.CameraPosition, Base);
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
            }
        }

        public void RemoveFromScene(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Removing wrong type of clipmap cell from scene!");
            cellProxy.SetVisibility(false);
        }

        public void DeleteCell(IMyClipmapCell cell)
        {
            var cellProxy = cell as MyClipmapCellProxy;
            Debug.Assert(cellProxy != null, "Deleting wrong type of clipmap cell!");

            if (cellProxy.Actor != null)
			    cellProxy.Unload();
        }

        internal static void UpdateQueued()
        {
            if (!MyRender11.Settings.FreezeTerrainQueries)
            {
                var cameraPosition = MyRenderProxy.PointsForVoxelPrecache.Count > 0 ? MyRenderProxy.PointsForVoxelPrecache[0] : MyRender11.Environment.Matrices.CameraPosition;

                MyClipmap.UpdateQueued(cameraPosition, MyRender11.Environment.Matrices.InvView.Forward, 
                    MyRender11.Environment.Matrices.FarClipping, MyRender11.Environment.Matrices.LargeDistanceFarClipping);
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
    }
}
