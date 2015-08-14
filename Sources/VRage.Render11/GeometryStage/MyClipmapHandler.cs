using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyClipmapCellProxy : IMyClipmapCell
    {
        MyActor m_actor;
        //MyVoxelMesh m_mesh;
        MeshId Mesh = MeshId.NULL;
        MatrixD m_worldMatrix;
        Vector3 m_scale;
        Vector3 m_translation;
        int m_lod;
        bool m_discardingOn;
        internal MyClipmapScaleEnum ScaleGroup;

        BoundingBox m_localAabb;

        void IMyClipmapCell.UpdateMesh(MyRenderMessageUpdateClipmapCell msg)
        {
            MyMeshes.UpdateVoxelCell(Mesh, msg.Batches);

            m_scale = msg.PositionScale;
            m_translation = msg.PositionOffset;
            m_localAabb = msg.MeshAabb;

            var matrix = (MatrixD)(MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix);

            m_actor.GetRenderable().m_voxelScale = m_scale;
            m_actor.GetRenderable().m_voxelOffset = m_translation;

            m_actor.SetMatrix(ref matrix);
            m_actor.SetAabb((BoundingBoxD)msg.MeshAabb.Transform(m_worldMatrix));
            m_actor.GetRenderable().SetVoxelLod(m_lod, ScaleGroup);

            (m_actor.GetComponent(MyActorComponentEnum.Foliage) as MyFoliageComponent).InvalidateStreams();
            m_actor.MarkRenderDirty();
        }

        bool /*IMyClipmapCell.*/PixelDiscardEnabled
        {
            get
            {
                return m_discardingOn;
            }
            set
            {
                m_discardingOn = value;
                //m_actor.MarkRenderDirty();
                m_actor.GetRenderable().SetVoxelLod(m_lod, ScaleGroup);
            }
        }

        void IMyClipmapCell.UpdateWorldMatrix(ref VRageMath.MatrixD worldMatrix, bool sortIntoCullObjects)
        {
            m_worldMatrix = worldMatrix;

            MatrixD m = MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix;
            m_actor.SetMatrix(ref m);
            m_actor.SetAabb((BoundingBoxD)m_localAabb.Transform(m_worldMatrix));
        }

        internal MyClipmapCellProxy(MyCellCoord cellCoord, ref VRageMath.MatrixD worldMatrix)
        {
            m_worldMatrix = worldMatrix;

            m_actor = MyActorFactory.CreateSceneObject();
            //m_mesh = new MyVoxelMesh(cellCoord.CoordInLod, cellCoord.Lod, "");
            //m_actor.GetRenderable().SetModel(m_mesh);
            m_actor.SetMatrix(ref worldMatrix);
            m_actor.AddComponent(MyComponentFactory<MyFoliageComponent>.Create());

            m_lod = cellCoord.Lod;

            Mesh = MyMeshes.CreateVoxelCell(cellCoord.CoordInLod, cellCoord.Lod);
            m_actor.GetRenderable().SetModel(Mesh);

            m_discardingOn = false;
        }

        internal void SetVisibility(bool value)
        {
            m_actor.SetVisibility(value);
        }

        internal void Unload()
        {
            //m_mesh.Dispose();
            m_actor.Destruct();
            MyMeshes.RemoveVoxelCell(Mesh);
        }
    }

    class MyClipmapHandler : IMyClipmapCellHandler
    {
        private readonly MyClipmap m_clipmapBase;
        internal MyClipmap Base { get { return m_clipmapBase; } }

        internal MyClipmapHandler(uint id, MyClipmapScaleEnum scaleGroup, MatrixD worldMatrix, Vector3I sizeLod0)
        {
            m_clipmapBase = new MyClipmap(id, scaleGroup, worldMatrix, sizeLod0, this);
            MyClipmap.AddToUpdate(MyEnvironment.CameraPosition, m_clipmapBase);
        }

        public IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref VRageMath.MatrixD worldMatrix)
        {
            var cell = new MyClipmapCellProxy(cellCoord, ref worldMatrix);
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
            if(!MyRender11.Settings.FreezeTerrainQueries)
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

    static class MyClipmapFactory
    {
        internal static Dictionary<uint, MyClipmapHandler> ClipmapByID = new Dictionary<uint, MyClipmapHandler>();

        internal static void RemoveAll()
        {
            foreach(var clipmap in ClipmapByID)
            {
                clipmap.Value.Base.UnloadContent();
            }

            ClipmapByID.Clear();
        }
    }
}
