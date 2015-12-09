using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyClipmapHandler : IMyClipmapCellHandler
    {
        private readonly MyClipmap m_clipmapBase;
        internal MyClipmap Base { get { return m_clipmapBase; } }

        RenderFlags m_renderFlags;
        public RenderFlags RenderFlags { get { return m_renderFlags; } }

        internal MyClipmapHandler(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0, RenderFlags additionalFlags)
        {
            m_clipmapBase = new MyClipmap(id, scaleGroup, worldMatrix, sizeLod0, this);
            m_renderFlags = additionalFlags;

            MyClipmap.AddToUpdate(MyEnvironment.CameraPosition, m_clipmapBase);
        }

        public IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref VRageMath.MatrixD worldMatrix)
        {
            var cell = new MyClipmapCellProxy(cellCoord, ref worldMatrix, m_renderFlags);
            cell.SetVisibility(false);
            cell.ScaleGroup = scaleGroup;
            return cell;
        }

        internal void UpdateWorldMatrix(ref MatrixD worldMatrix)
        {
            m_clipmapBase.UpdateWorldMatrix(ref worldMatrix, true);
        }

        public void DeleteCell(IMyClipmapCell cell)
        {
            (cell as MyClipmapCellProxy).Unload();
        }

        public void AddToScene(IMyClipmapCell cell)
        {
            (cell as MyClipmapCellProxy).SetVisibility(true);
        }

        public void RemoveFromScene(IMyClipmapCell cell)
        {
            (cell as MyClipmapCellProxy).SetVisibility(false);
        }

        internal static void UpdateQueued()
        {
            if (!MyRender11.Settings.FreezeTerrainQueries)
            {
                MyClipmap.UpdateQueued(MyEnvironment.CameraPosition, MyEnvironment.FarClipping, MyEnvironment.LargeDistanceFarClipping);
            }
        }

        internal void RemoveFromUpdate()
        {
            Base.UnloadContent();
            MyClipmap.RemoveFromUpdate(Base);
        }
    }
}
