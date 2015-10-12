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
        MeshId Mesh = MeshId.NULL;
        MatrixD m_worldMatrix;
        Vector3 m_scale;
        Vector3 m_translation;
        int m_lod;
        internal MyClipmapScaleEnum ScaleGroup;

        BoundingBox m_localAabb;

        void IMyClipmapCell.UpdateMesh(MyRenderMessageUpdateClipmapCell msg)
        {
            MyMeshes.UpdateVoxelCell(Mesh, msg.Batches);

            m_scale = msg.Metadata.PositionScale;
            m_translation = msg.Metadata.PositionOffset;
            m_localAabb = msg.Metadata.LocalAabb;

            var matrix = MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix;

            m_actor.GetRenderable().m_voxelScale = m_scale;
            m_actor.GetRenderable().m_voxelOffset = m_translation;

            m_actor.SetMatrix(ref matrix);
            m_actor.SetAabb(msg.Metadata.LocalAabb.Transform(m_worldMatrix));
            m_actor.GetRenderable().SetVoxelLod(m_lod, ScaleGroup);

            (m_actor.GetComponent(MyActorComponentEnum.Foliage) as MyFoliageComponent).InvalidateStreams();
            m_actor.MarkRenderDirty();
        }

        void IMyClipmapCell.UpdateWorldMatrix(ref VRageMath.MatrixD worldMatrix, bool sortIntoCullObjects)
        {
            m_worldMatrix = worldMatrix;

            MatrixD m = MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix;
            m_actor.SetMatrix(ref m);
            m_actor.SetAabb((BoundingBoxD)m_localAabb.Transform(m_worldMatrix));
        }

		internal MyClipmapCellProxy(MyCellCoord cellCoord, ref VRageMath.MatrixD worldMatrix, RenderFlags additionalFlags = 0)
        {
            m_worldMatrix = worldMatrix;

            m_actor = MyActorFactory.CreateSceneObject();
            m_actor.SetMatrix(ref worldMatrix);
            m_actor.AddComponent(MyComponentFactory<MyFoliageComponent>.Create());

            m_lod = cellCoord.Lod;

            Mesh = MyMeshes.CreateVoxelCell(cellCoord.CoordInLod, cellCoord.Lod);
            m_actor.GetRenderable().SetModel(Mesh);
			m_actor.GetRenderable().m_additionalFlags = MyProxiesFactory.GetRenderableProxyFlags(additionalFlags);
        }

        internal void SetVisibility(bool value)
        {
            m_actor.SetVisibility(value);
        }

        internal void Unload()
        {
            m_actor.Destruct();
            MyMeshes.RemoveVoxelCell(Mesh);
        }
    }
}
