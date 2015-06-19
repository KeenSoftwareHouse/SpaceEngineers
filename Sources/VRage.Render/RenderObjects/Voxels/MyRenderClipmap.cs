using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Voxels;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;

namespace VRageRender
{
    partial class MyRenderClipmap : MyRenderObject, IMyClipmapCellHandler
    {
        private readonly MyInterpolationQueue<MatrixD> m_interpolation = new MyInterpolationQueue<MatrixD>(3, MatrixD.Slerp);
        private readonly MyClipmap m_clipmapBase;
        Vector3D m_position;
        float m_atmosphereRadius = 0.0f;
        float m_planetRadius = 0.0f;
        bool m_hasAtmosphere = false;
        Vector3? m_atmosphereWaveLengths = null;

        public MyRenderClipmap(MyRenderMessageCreateClipmap msg)
            : base(msg.ClipmapId, "Clipmap")
        {
            m_clipmapBase = new MyClipmap(msg.ClipmapId, msg.ScaleGroup, msg.WorldMatrix, msg.SizeLod0, this);
            SetDirty();
            m_position = msg.Position;
            m_atmosphereRadius = msg.AtmosphereRadius;
            m_planetRadius = msg.PlanetRadius;
            m_hasAtmosphere = msg.HasAtmosphere;
            m_atmosphereWaveLengths = msg.AtmosphereWaveLenghts;
        }

        public override void UpdateWorldAABB()
        {
            m_clipmapBase.UpdateWorldAABB(out m_aabb);
            base.UpdateWorldAABB();
        }

        internal void UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortCellsIntoCullObjects)
        {
            MyRender.AddAndInterpolateObjectMatrix(m_interpolation, ref worldMatrix);
            m_clipmapBase.UpdateWorldMatrix(ref worldMatrix, sortCellsIntoCullObjects);
            SetDirty();
        }

        public override void DebugDraw()
        {
            //MyDebugDraw.DrawAABBLine(ref m_aabb, ref Vector4.One, 1f, true);

            base.DebugDraw();
        }

        public override void LoadContent()
        {
            m_clipmapBase.LoadContent();
            MyClipmap.AddToUpdate(MyRenderCamera.Position, m_clipmapBase);
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            m_clipmapBase.UnloadContent();
            MyClipmap.RemoveFromUpdate(m_clipmapBase);
            base.UnloadContent();
        }

        /// <param name="minCellLod0">Inclusive.</param>
        /// <param name="maxCellLod0">Inclusive.</param>
        internal void InvalidateRange(Vector3I minCellLod0, Vector3I maxCellLod0)
        {
            m_clipmapBase.InvalidateRange(minCellLod0, maxCellLod0);
        }

        internal void UpdateCell(MyRenderMessageUpdateClipmapCell msg)
        {
            m_clipmapBase.UpdateCell(msg);
        }

        internal static void UpdateQueued()
        {
            if (!MyRender.Settings.FreezeTerrainQueries)
            {
                MyClipmap.UpdateQueued(MyRenderCamera.Position, MyRenderCamera.FAR_PLANE_DISTANCE, MyRenderCamera.FAR_PLANE_FOR_BACKGROUND);
            }
        }

        IMyClipmapCell IMyClipmapCellHandler.CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref MatrixD worldMatrix)
        {
            switch (scaleGroup)
            {
                case MyClipmapScaleEnum.Normal:
                    return new MyRenderVoxelCell(scaleGroup, cellCoord, ref worldMatrix);

                case MyClipmapScaleEnum.Massive:
                    return new MyRenderVoxelCellBackground(cellCoord, ref worldMatrix, m_position, m_atmosphereRadius, m_planetRadius, m_hasAtmosphere,m_atmosphereWaveLengths.Value);

                default:
                    throw new InvalidBranchException();
            }
        }

        void IMyClipmapCellHandler.DeleteCell(IMyClipmapCell cell)
        {
            ((MyRenderVoxelCell)cell).UnloadContent();
        }

        void IMyClipmapCellHandler.AddToScene(IMyClipmapCell cell)
        {
            MyRender.AddRenderObject((MyRenderVoxelCell)cell);
        }

        void IMyClipmapCellHandler.RemoveFromScene(IMyClipmapCell cell)
        {
            MyRender.RemoveRenderObject((MyRenderVoxelCell)cell);
        }
    }
}
